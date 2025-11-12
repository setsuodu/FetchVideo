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

        // 1. 解析两端 URL，生成规则
        var urlInfos = GenerateSequentialUrls(request.FirstUrl, request.LastUrl);
        if (urlInfos == null)
            return BadRequest("无法解析连号规则：请确保两端 URL 只在数字部分不同，且数字长度一致");

        var (basePrefix, baseSuffix, startNum, endNum, pad) = urlInfos.Value;

        // ========== 新增：提取“数字前的那一段”作为文件夹名 ==========
        var uriSample = new Uri(request.FirstUrl);
        var pathSegment = uriSample.Segments; // e.g., ["/images/", "pic_", "001.jpg"]
        string folderName = "unknown";

        // 找到数字所在 segment 的前一个 segment（去掉 /）
        var numberMatch = Regex.Match(uriSample.AbsolutePath, @"\d+");
        if (numberMatch.Success)
        {
            int digitStart = numberMatch.Index;
            // 往前找最后一个 / 的位置
            int lastSlash = uriSample.AbsolutePath.LastIndexOf('/', digitStart - 1);
            if (lastSlash >= 0)
            {
                int prevSlash = uriSample.AbsolutePath.LastIndexOf('/', lastSlash - 1);
                folderName = uriSample.AbsolutePath.Substring(prevSlash + 1, lastSlash - prevSlash);
                folderName = folderName.TrimEnd('/'); // 防止多余 /
            }
        }

        // 安全文件名（去掉非法字符）
        folderName = string.Join("_", folderName.Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrEmpty(folderName)) folderName = "batch";

        // 创建子目录：/app/downloads/{folderName}
        var groupDir = Path.Combine(_downloadPath, folderName);
        Directory.CreateDirectory(groupDir);
        // ==========================================================

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
                    var success = await TryDownloadSingle(url, groupDir); // 传入 groupDir
                    if (!success) failedUrls.Add(url);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);

        // 3. 写 404 日志（放在组内）
        var logPath = Path.Combine(groupDir, "download_404.txt");
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
            GroupFolder = $"/downloads/{folderName}",  // 前端可访问路径
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
    /// 尝试下载单个文件，成功返回 true，失败（404/异常）返回 false
    /// </summary>
    private async Task<bool> TryDownloadSingle(string url, string targetDir)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromMinutes(5);

            var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
                return false;

            // 从 URL 提取原始文件名（如 001.jpg）
            var fileName = Path.GetFileName(new Uri(url).AbsolutePath);
            if (string.IsNullOrEmpty(Path.GetExtension(fileName)))
                fileName += ".dat";

            var fullPath = Path.Combine(targetDir, fileName);
            using var stream = await response.Content.ReadAsStreamAsync();
            using var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await stream.CopyToAsync(fs);

            _logger.LogInformation($"下载成功: {url} -> {fullPath}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"下载失败（已跳过）: {url}");
            return false;
        }
    }

    /// <summary>
    /// 解析两端 URL，生成连号规则
    /// 返回 (prefix, suffix, start, end, padLength) 或 null
    /// </summary>
    private (string prefix, string suffix, int start, int end, int pad)? GenerateSequentialUrls(string first, string last)
    {
        var u1 = new Uri(first);
        var u2 = new Uri(last);

        if (u1.Scheme != u2.Scheme || u1.Host != u2.Host || u1.Port != u2.Port)
            return null;

        var path1 = u1.AbsolutePath;
        var path2 = u2.AbsolutePath;

        // 找到第一个数字段与最后一个数字段
        var match1 = Regex.Matches(path1, @"\d+").Cast<Match>().ToList();
        var match2 = Regex.Matches(path2, @"\d+").Cast<Match>().ToList();

        if (match1.Count == 0 || match2.Count == 0) return null;

        // 简单规则：两端只有 **一个** 数字段不同
        // 更复杂情况（多段）可自行扩展
        int diffCount = 0, diffIdx = -1;
        for (int i = 0; i < Math.Min(match1.Count, match2.Count); i++)
        {
            if (match1[i].Value != match2[i].Value)
            {
                diffCount++;
                diffIdx = i;
            }
        }
        if (diffCount != 1) return null;   // 只能有一个数字段不同

        var startNum = int.Parse(match1[diffIdx].Value);
        var endNum = int.Parse(match2[diffIdx].Value);
        if (startNum >= endNum) return null;   // 必须递增

        var pad = match1[diffIdx].Value.Length;

        // 前缀 = path1 从开头到数字前
        var prefixStart = match1[diffIdx].Index;
        var prefix = path1.Substring(0, prefixStart);

        // 后缀 = path1 从数字后到结尾
        var suffixStart = match1[diffIdx].Index + match1[diffIdx].Length;
        var suffix = path1.Substring(suffixStart);

        // 完整 URL 前缀（含查询参数）
        var baseUrl = $"{u1.Scheme}://{u1.Host}:{u1.Port}";
        var fullPrefix = baseUrl + prefix;
        var fullSuffix = suffix + (string.IsNullOrEmpty(u1.Query) ? "" : u1.Query);

        return (fullPrefix, fullSuffix, startNum, endNum, pad);
    }
}