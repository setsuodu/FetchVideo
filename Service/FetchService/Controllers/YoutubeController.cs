using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FetchService.Controllers;

public class YoutubeController
{
    // 创建进度回调
    Progress<double> progress = new Progress<double>(p =>
    {
        Console.Write($"\r下载进度: {p:P1}"); // P1 = 百分比(一位小数)
    });

    public async Task GetYoutubeVideoAsync(string url)
    {
        string part = await GetVideoInfoAsync(url);
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string videoFile = Path.Combine(desktopPath, $"video.mp4");
        string audioFile = Path.Combine(desktopPath, $"audio.m4a");
        string outputFile = Path.Combine(desktopPath, $"{(string.IsNullOrEmpty(part) ? "output" : part)}.mp4");
        Console.WriteLine($"outputFile是: {outputFile}");

        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

        // 视频流列表
        var videoStreams = streamManifest.GetVideoOnlyStreams();
        // 列出所有分辨率
        foreach (var stream in videoStreams)
        {
            Console.WriteLine($"{stream.VideoQuality.Label} | {stream.Container.Name} | {(stream.Bitrate.BitsPerSecond / 1000000.0):F1} Mbps");
        }

        // 优先使用已合成的流
        var muxed = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
        if (muxed != null)
        {
            await youtube.Videos.Streams.DownloadAsync(muxed, outputFile, progress); // 不分轨的
            Console.WriteLine($"不分轨的: {outputFile}");
        }
        else
        {
            // 否则分开下载
            var videoStream = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality(); // 下载最高质量
            var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

            await youtube.Videos.Streams.DownloadAsync(videoStream, videoFile, progress);
            Console.WriteLine($"视频下载: {videoFile}");
            await youtube.Videos.Streams.DownloadAsync(audioStream, audioFile, progress);
            Console.WriteLine($"音频下载: {audioFile}");

            // 用 FFmpeg 合并
            Shared.MergeAudioVideo(videoFile, audioFile, outputFile);
            Console.WriteLine($"合并完成: {outputFile}");
        }
    }

    private async Task<string> GetVideoInfoAsync(string url)
    {
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);
        Console.WriteLine($"标题: {video.Title}");
        Console.WriteLine($"作者: {video.Author.ChannelTitle}");
        Console.WriteLine($"频道ID: {video.Author.ChannelId}");
        Console.WriteLine($"发布时间: {video.UploadDate}");
        Console.WriteLine($"时长: {video.Duration}");
        Console.WriteLine($"封面: {video.Thumbnails[0].Url}");
        Console.WriteLine($"描述: {video.Description}");

        //part = video.Title;
        //Console.WriteLine($"保存文件名: {part}");
        return Shared.MakeFileNameSafe(video.Title);
    }
}
