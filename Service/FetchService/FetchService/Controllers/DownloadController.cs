using Microsoft.AspNetCore.Mvc;
using FetchService.Models;

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
    /// 扩展点：批量下载连号图片（e.g., image_1.jpg ~ image_10.jpg）
    /// </summary>
    [HttpPost("download-batch")]
    public async Task<IActionResult> DownloadBatch([FromBody] BatchDownloadRequest request)
    {
        // TODO: 实现逻辑 - 循环生成 URL（如 baseUrl + i + ".jpg"），调用 DownloadFile
        // 示例：for (int i = request.Start; i <= request.End; i++) { var url = $"{request.BaseUrl}{i}{request.Extension}"; ... }
        // 返回批量结果列表
        return Ok(new { Success = true, Message = "批量下载接口待实现" });
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
}