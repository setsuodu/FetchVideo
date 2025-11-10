using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FetchVideo.Controllers;

public class YoutubeController
{
    string part = ""; //视频名称

    // 创建进度回调
    Progress<double> progress = new Progress<double>(p =>
    {
        Console.Write($"\r下载进度: {p:P1}"); // P1 = 百分比(一位小数)
    });

    public async Task GetYoutubeVideoAsync(string url)
    {
        if (!Directory.Exists("Download"))
            Directory.CreateDirectory("Download");
        string videoFile = "Download\\video.mp4";
        string audioFile = "Download\\audio.m4a";
        string outputFile = $"Download\\{(string.IsNullOrEmpty(part) ? "output" : part)}.mp4";
        Console.WriteLine($"outputFile是: {Shared.MakeFileNameSafe(outputFile)}");

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

    public async Task GetVideoInfoAsync(string url)
    {
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);

        //var info = new YouTubeVideoInfo
        //{
        //    Title = video.Title,
        //    Author = video.Author.ChannelTitle,
        //    ChannelId = video.Author.ChannelId,
        //    Description = video.Description,
        //    ThumbnailUrl = video.Thumbnails.LastOrDefault()?.Url,
        //    UploadDate = video.UploadDate,
        //    Duration = video.Duration
        //};

        Console.WriteLine($"标题: {video.Title}");
        Console.WriteLine($"作者: {video.Author.ChannelTitle}");
        Console.WriteLine($"频道ID: {video.Author.ChannelId}");
        Console.WriteLine($"发布时间: {video.UploadDate}");
        Console.WriteLine($"时长: {video.Duration}");
        Console.WriteLine($"封面: {video.Thumbnails[0].Url}");
        Console.WriteLine($"描述: {video.Description}");

        part = video.Title;
        Console.WriteLine($"保存文件名: {part}");
    }
}