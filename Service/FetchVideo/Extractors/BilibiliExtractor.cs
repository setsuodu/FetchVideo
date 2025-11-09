using FetchVideo.Core;

namespace FetchVideo.Extractors;

public class BilibiliExtractor : IVideoExtractor
{
    public async Task<VideoInfo> GetVideoInfoAsync(string url)
    {
        // 使用 Bilibili API 获取 aid/cid
        // 调用 https://api.bilibili.com/x/web-interface/view?bvid=...
        // 解析 dash / mp4 流
        return null;
    }

    public async Task DownloadAsync(VideoInfo info, string outputPath, IProgress<double> progress, CancellationToken ct)
    {
        // 使用 HttpClient + aria2c 或分段下载
    }
}
