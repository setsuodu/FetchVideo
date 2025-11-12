using Microsoft.AspNetCore.Mvc;

namespace DownloadService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DownloadController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private const string HostDownloadPath = "/app/download"; // 容器内路径

    public DownloadController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }
    [HttpGet("start")]
    public async Task<IActionResult> StartDownload([FromQuery] string url, [FromQuery] string? filename = null)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            return BadRequest("Invalid URL.");

        filename ??= Path.GetFileName(new Uri(url).LocalPath);
        if (string.IsNullOrEmpty(filename)) filename = "downloaded_file";

        var containerPath = Path.Combine(HostDownloadPath, filename);
        var hostPath = Path.Combine(AppContext.BaseDirectory, "download", filename); // 宿主机路径（相对项目）

        // 确保容器内目录存在
        Directory.CreateDirectory(HostDownloadPath);

        var client = _httpClientFactory.CreateClient();
        try
        {
            using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            await using var fileStream = new FileStream(containerPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fileStream);

            // 关键：获取并打印 Windows 绝对路径
            var absoluteHostPath = Path.GetFullPath(hostPath);

            // 打印到 VS 输出窗口（Debug）
            System.Diagnostics.Debug.WriteLine($"[Download Success] Host File Path: {absoluteHostPath}");

            // 可选：打印到控制台（docker logs 也能看到）
            Console.WriteLine($"[Download Success] Host File Path: {absoluteHostPath}");

            return Ok(new
            {
                Message = "Download completed",
                FileName = filename,
                ContainerPath = containerPath,
                HostPath = absoluteHostPath  // 返回给客户端
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = ex.Message });
        }
    }
}