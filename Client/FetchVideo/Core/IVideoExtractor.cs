namespace FetchVideo.Core;

public interface IVideoExtractor
{
    Task<VideoInfo> GetVideoInfoAsync(string url);
    Task DownloadAsync(VideoInfo info, string outputPath, IProgress<double> progress = null, CancellationToken ct = default);
}
