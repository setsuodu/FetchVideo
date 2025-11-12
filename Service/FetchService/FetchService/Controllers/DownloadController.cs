using FetchService.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace FetchService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _downloadPath;
    private readonly ILogger<DownloadController> _logger;

    public DownloadController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<DownloadController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _downloadPath = configuration["DownloadPath"] ?? "/app/downloads";  // 默认容器路径
        _logger = logger;
        Directory.CreateDirectory(_downloadPath);  // 确保目录存在
    }

    /// <summary>
    /// 下载单个文件（图片/视频等），保留 URL 文件名
    /// </summary>
    /// <param name="request">包含 URL 的请求体</param>
    /// <returns>下载结果</returns>
    [HttpPost("download")]
    public async Task<IActionResult> DownloadFile([FromBody] DownloadRequest request)
    {
        Console.WriteLine("下载单张图片");

        if (string.IsNullOrEmpty(request.Url))
            return BadRequest("URL 不能为空");

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(request.Url);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, $"下载失败: {response.StatusCode}");

            // 从 URL 提取文件名（保留扩展名）
            var fileName = Path.GetFileName(new Uri(request.Url).AbsolutePath) ?? "unknown_file";
            if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                fileName += ".dat";  // 默认扩展名，避免无扩展

            var fullPath = Path.Combine(_downloadPath, fileName);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fileStream);

            _logger.LogInformation($"文件下载成功: {fileName} -> {fullPath}");

            Console.WriteLine("下载成功");

            return Ok(new { Success = true, FilePath = fullPath, FileName = fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "下载异常");
            return StatusCode(500, $"下载异常: {ex.Message}");
        }
    }

    /// <summary>
    /// 连号图片批量下载
    /// </summary>
    /// <param name="request">包含 FirstUrl、LastUrl、Concurrency</param>
    /// <returns>成功/失败统计 + 404 日志路径</returns>
    [HttpPost("download-batch")]
    public async Task<IActionResult> DownloadBatch([FromBody] BatchDownloadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FirstUrl) || string.IsNullOrWhiteSpace(request.LastUrl))
            return BadRequest("FirstUrl 和 LastUrl 必须提供");

        // 1. 解析 URL 规则 + 提取基础文件名
        var parseResult = ParseSequentialUrls(request.FirstUrl, request.LastUrl);
        if (parseResult == null)
            return BadRequest("无法解析连号规则：请确保两端 URL 只在数字部分不同，且数字长度一致");

        var (basePrefix, baseSuffix, startNum, endNum, pad, folderName) = parseResult.Value;

        // 2. 创建专用文件夹（宿主机映射目录下）
        var folderPath = Path.Combine(_downloadPath, folderName);
        Directory.CreateDirectory(folderPath);

        // 3. 并发下载
        var failedUrls = new ConcurrentBag<string>();
        var semaphore = new SemaphoreSlim(request.Concurrency);
        var tasks = new List<Task>();

        for (int i = startNum; i <= endNum; i++)
        {
            var currentNumStr = i.ToString().PadLeft(pad, '0');
            var url = basePrefix + currentNumStr + baseSuffix;

            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var success = await TryDownloadSingleToFolder(url, folderPath);
                    if (!success) failedUrls.Add(url);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // 4. 写 404 日志（只在有失败时）
        var logPath = Path.Combine(folderPath, "download_404.txt");
        if (failedUrls.Count > 0)
        {
            await System.IO.File.WriteAllLinesAsync(logPath, failedUrls);
        }
        else if (System.IO.File.Exists(logPath))
        {
            System.IO.File.Delete(logPath);
        }

        return Ok(new
        {
            Success = true,
            Total = endNum - startNum + 1,
            Downloaded = endNum - startNum + 1 - failedUrls.Count,
            Failed = failedUrls.Count,
            Folder = folderName,
            LogPath = failedUrls.Count > 0 ? $"/downloads/{folderName}/download_404.txt" : null
        });
    }

    /// <summary>
    /// 扩展点：视频下载（支持大文件，添加进度回调）
    /// </summary>
    [HttpPost("download-video")]
    public async Task<IActionResult> DownloadVideo([FromBody] DownloadRequest request)
    {
        // TODO: 与 DownloadFile 类似，但用 Range 请求支持断点续传
        // 示例：client.DefaultRequestHeaders.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);
        return Ok(new { Success = true, Message = "视频下载接口待实现" });
    }

    /// <summary>
    /// 解析连号 URL 并生成：前缀、后缀、数字范围、填充位数、文件夹名
    /// </summary>
    private (string prefix, string suffix, int start, int end, int pad, string folderName)?
        ParseSequentialUrls(string first, string last)
    {
        var u1 = new Uri(first);
        var u2 = new Uri(last);

        if (u1.Scheme != u2.Scheme || u1.Host != u2.Host || u1.Port != u2.Port)
            return null;

        var path1 = u1.AbsolutePath;
        var path2 = u2.AbsolutePath;

        var matches1 = Regex.Matches(path1, @"\d+").Cast<Match>().ToList();
        var matches2 = Regex.Matches(path2, @"\d+").Cast<Match>().ToList();

        if (matches1.Count == 0 || matches2.Count == 0) return null;

        // 找出唯一不同的数字段
        int diffIdx = -1;
        for (int i = 0; i < Math.Min(matches1.Count, matches2.Count); i++)
        {
            if (matches1[i].Value != matches2[i].Value)
            {
                if (diffIdx >= 0) return null; // 多于一个差异
                diffIdx = i;
            }
        }
        if (diffIdx < 0) return null;

        var startNum = int.Parse(matches1[diffIdx].Value);
        var endNum = int.Parse(matches2[diffIdx].Value);
        if (startNum >= endNum) return null;

        var pad = matches1[diffIdx].Value.Length;
        var digitStart = matches1[diffIdx].Index;
        var digitEnd = digitStart + matches1[diffIdx].Length;

        // 提取基础文件名（去掉数字 + 数字前的特殊字符）
        var fileNamePart = path1.Substring(0, digitEnd);
        var cleanName = Regex.Replace(fileNamePart, @"[._\-]*\d+$", ""); // 去掉 _001 前的特殊字符
        cleanName = Path.GetFileNameWithoutExtension(cleanName);

        // 如果仅剩数字 → 从路径左侧取一段
        if (string.IsNullOrWhiteSpace(cleanName) || Regex.IsMatch(cleanName, @"^\d+$"))
        {
            var segments = u1.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            cleanName = segments.Length >= 2 ? segments[^2] : "batch"; // 倒数第二段
        }

        // 清理非法文件夹字符
        cleanName = string.Join("_", cleanName.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(cleanName)) cleanName = "download";

        var prefix = $"{u1.Scheme}://{u1.Host}:{u1.Port}" + path1.Substring(0, digitStart);
        var suffix = path1.Substring(digitEnd) + u1.Query;

        return (prefix, suffix, startNum, endNum, pad, cleanName);
    }

    private async Task<bool> TryDownloadSingleToFolder(string url, string folderPath)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return false;

            var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
            if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                fileName += ".dat";

            var fullPath = Path.Combine(folderPath, fileName);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
            await stream.CopyToAsync(fs);

            _logger.LogInformation($"下载成功: {url} -> {fullPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"下载失败（跳过）: {url}");
            return false;
        }
    }
}