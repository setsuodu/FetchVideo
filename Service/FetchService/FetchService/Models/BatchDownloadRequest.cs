namespace FetchService.Models;

public class BatchDownloadRequest
{
    public string BaseUrl { get; set; } = string.Empty;  // e.g., "https://example.com/image_"
    public int Start { get; set; } = 1;
    public int End { get; set; } = 10;
    public string Extension { get; set; } = ".jpg";
}
