using FetchVideo.Core;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FetchVideo.Extractors;

public class YoutubeExtractor : IVideoExtractor
{
    private readonly YoutubeClient _client = new();

    public async Task<VideoInfo> GetVideoInfoAsync(string url)
    {
        var video = await _client.Videos.GetAsync(url);
        var manifest = await _client.Videos.Streams.GetManifestAsync(video.Id);

        var info = new VideoInfo
        {
            Title = video.Title,
            Author = video.Author.ChannelTitle,
            Duration = video.Duration ?? TimeSpan.Zero,
        };

        info.Streams.AddRange(manifest.GetMuxedStreams()
            .OrderByDescending(s => s.VideoQuality)
            .Select(s => new StreamInfo
            {
                Quality = $"{s.VideoQuality.Label} ({s.Container})",
                Format = s.Container.ToString(),
                Size = s.Size.Bytes,
                Url = s.Url
            }));

        return info;
    }

    public async Task DownloadAsync(VideoInfo info, string outputPath, IProgress<double> progress, CancellationToken ct)
    {
        var streamInfo = info.Streams.First();
        //var stream = await _client.Videos.Streams.GetAsync(new StreamInfoAdapter(streamInfo));
        //await _client.Videos.Streams.DownloadAsync(streamInfo.Url, outputPath, progress, ct);
    }
}
