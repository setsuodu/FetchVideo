namespace FetchService.Models;

public class BatchDownloadRequest
{
    public string FirstUrl { get; set; } = string.Empty;   // 第一张图片完整 URL
    public string LastUrl { get; set; } = string.Empty;   // 最后一张图片完整 URL
    public int Concurrency { get; set; } = 5;             // 可选并发数，默认 5
}