namespace FetchService.Models;

// Models/VideoDownloadResult.cs
public class VideoDownloadResult
{
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? DownloadUrl { get; set; }
    public string? LogPath { get; set; }
}