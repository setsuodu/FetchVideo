using FetchVideo.Models;
using FetchVideo.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FetchVideo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class YoutubeController : ControllerBase
{
    private readonly ISharedService _sharedService;
    private readonly string _downloadPath;
    private readonly FFmpegManager ffManager;

    // 从构造函数注入配置，变成本地只读（推荐写法！）
    public YoutubeController(ISharedService sharedService)
    {
        _sharedService = sharedService;
        _downloadPath = sharedService._downloadPath;
        ffManager = sharedService._ffManager;
    }

    // 创建进度回调
    Progress<double> progress = new Progress<double>(p =>
    {
        Console.Write($"\r下载进度: {p:P1}"); // P1 = 百分比(一位小数)
    });

    public async Task<FFmpegTaskDto> GetYoutubeVideoAsync(string url)
    {
        string title = await GetVideoInfoAsync(url);
        string desktopPath = _downloadPath;
        string videoFile = Path.Combine(desktopPath, $"video.mp4");
        string audioFile = Path.Combine(desktopPath, $"audio.m4a");
        string outputFile = Path.Combine(desktopPath, $"{(string.IsNullOrEmpty(title) ? "output" : title)}.mp4");
        Console.WriteLine($"outputFile: {outputFile}");

        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

        // 视频流列表（调试用）
        var videoStreams = streamManifest.GetVideoOnlyStreams();
        foreach (var stream in videoStreams)
        {
            Console.WriteLine($"{stream.VideoQuality.Label} | {stream.Container.Name} | {(stream.Bitrate.BitsPerSecond / 1000000.0):F1} Mbps");
        }

        // YouTube 已基本取消传统 Muxed（音视频合一）流
        // GetMuxedStreams().GetWithHighestVideoQuality() 在空集合上会抛 "Input stream collection is empty"
        // 因此始终使用 Video-Only + Audio-Only 分离下载，再用 FFmpeg 合并
        var videoStream = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();
        var audioStream = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        if (videoStream == null || audioStream == null)
        {
            throw new InvalidOperationException("无法获取可用的视频或音频流（YouTube 已取消 Muxed 流，且自适应流不可用）");
        }

        Console.WriteLine($"选择视频: {videoStream.VideoQuality.Label} | {videoStream.Container.Name}");
        Console.WriteLine($"选择音频: {audioStream.Bitrate.BitsPerSecond / 1000.0:F0} kbps | {audioStream.Container.Name}");

        await youtube.Videos.Streams.DownloadAsync(videoStream, videoFile, progress);
        Console.WriteLine($"视频下载: {videoFile}");
        await youtube.Videos.Streams.DownloadAsync(audioStream, audioFile, progress);
        Console.WriteLine($"音频下载: {audioFile}");

        // FFmpeg 合并（-c copy 无需重新编码，速度极快）
        string command = $"-i \"{videoFile}\" -i \"{audioFile}\" -c copy \"{outputFile}\" -y";
        var task = ffManager.StartFFmpeg(command); // YT视频
        Console.WriteLine($"下载完成: {DateTime.Now}");
        await task.Process.WaitForExitAsync();
        System.IO.File.Delete(videoFile);
        System.IO.File.Delete(audioFile);
        return FFmpegManager.ConvertDto(task);
    }

    private async Task<string> GetVideoInfoAsync(string url)
    {
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);
        // 取反：中文、字母、数字、空格，以外移除
        string title = Regex.Replace(video.Title, @"[^\u4e00-\u9fa5a-zA-Z0-9\s]", "");
        Console.WriteLine($"标题: {title}");
        Console.WriteLine($"作者: {video.Author.ChannelTitle}");
        Console.WriteLine($"频道ID: {video.Author.ChannelId}");
        Console.WriteLine($"发布时间: {video.UploadDate}");
        Console.WriteLine($"时长: {video.Duration}");
        Console.WriteLine($"封面: {video.Thumbnails[0].Url}");
        Console.WriteLine($"描述: {video.Description}");
        //return Shared.MakeFileNameSafe(title);
        return title;
    }

    // missav
    public async Task<FFmpegTaskDto> TestM3U8(string m3u8)
    {
        string command = $"-i \"{m3u8}\" -c copy \"{_downloadPath}.mp4\"";
        var task = ffManager.StartFFmpeg(command, "missav");
        Console.WriteLine($"下载完成: {DateTime.Now}");
        await task.Process.WaitForExitAsync();
        return FFmpegManager.ConvertDto(task);
    }
}
