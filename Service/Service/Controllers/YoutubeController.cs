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

    public YoutubeController(ISharedService sharedService)
    {
        _sharedService = sharedService;
        _downloadPath = sharedService._downloadPath;
        ffManager = sharedService._ffManager;
    }

    Progress<double> progress = new Progress<double>(p =>
    {
        Console.Write($"\r下载进度: {p:P1}");
    });

    public async Task<FFmpegTaskDto> GetYoutubeVideoAsync(string url)
    {
        string title = await GetVideoInfoAsync(url);
        string desktopPath = _downloadPath;
        string safeTitle = string.IsNullOrEmpty(title) ? "output" : title.Trim();
        string tempId = Guid.NewGuid().ToString("N")[..8];

        string outputFile = Path.Combine(desktopPath, $"{safeTitle}.mp4");
        Console.WriteLine($"outputFile: {outputFile}");

        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);
        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

        foreach (var stream in streamManifest.GetVideoOnlyStreams())
        {
            Console.WriteLine($"{stream.VideoQuality.Label} | {stream.Container.Name} | {(stream.Bitrate.BitsPerSecond / 1000000.0):F1} Mbps");
        }

        var videoStream = streamManifest.GetVideoOnlyStreams().GetWithHighestVideoQuality();

        var audioStream = streamManifest.GetAudioOnlyStreams()
            .Where(s => s.Container == Container.Mp4)
            .OrderByDescending(s => s.Bitrate)
            .FirstOrDefault()
            ?? streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();

        if (videoStream == null || audioStream == null)
        {
            throw new InvalidOperationException("无法获取可用的视频或音频流（YouTube 已取消 Muxed 流，且自适应流不可用）");
        }

        string videoExt = videoStream.Container.Name;
        string audioExt = audioStream.Container.Name;
        string videoFile = Path.Combine(desktopPath, $"_yt_{tempId}_video.{videoExt}");
        string audioFile = Path.Combine(desktopPath, $"_yt_{tempId}_audio.{audioExt}");

        Console.WriteLine($"选择视频: {videoStream.VideoQuality.Label} | {videoStream.Container.Name}");
        Console.WriteLine($"选择音频: {audioStream.Bitrate.BitsPerSecond / 1000.0:F0} kbps | {audioStream.Container.Name}");

        await youtube.Videos.Streams.DownloadAsync(videoStream, videoFile, progress);
        Console.WriteLine($"视频下载: {videoFile}");
        await youtube.Videos.Streams.DownloadAsync(audioStream, audioFile, progress);
        Console.WriteLine($"音频下载: {audioFile}");

        string command;
        if (audioStream.Container == Container.Mp4 && videoStream.Container == Container.Mp4)
        {
            command = $"-i \"{videoFile}\" -i \"{audioFile}\" -c copy \"{outputFile}\" -y";
        }
        else
        {
            command = $"-i \"{videoFile}\" -i \"{audioFile}\" -c:v copy -c:a aac -b:a 192k \"{outputFile}\" -y";
        }

        var task = ffManager.StartFFmpeg(command);
        Console.WriteLine($"合并中: {outputFile}");
        await task.Process.WaitForExitAsync();
        try { System.IO.File.Delete(videoFile); } catch { }
        try { System.IO.File.Delete(audioFile); } catch { }
        Console.WriteLine($"完成: {outputFile}");
        return FFmpegManager.ConvertDto(task);
    }

    private async Task<string> GetVideoInfoAsync(string url)
    {
        var youtube = new YoutubeClient();
        var video = await youtube.Videos.GetAsync(url);
        string title = Regex.Replace(video.Title, @"[^\u4e00-\u9fa5a-zA-Z0-9\s]", "");
        Console.WriteLine($"标题: {title}");
        Console.WriteLine($"作者: {video.Author.ChannelTitle}");
        Console.WriteLine($"频道ID: {video.Author.ChannelId}");
        Console.WriteLine($"发布时间: {video.UploadDate}");
        Console.WriteLine($"时长: {video.Duration}");
        Console.WriteLine($"封面: {video.Thumbnails[0].Url}");
        Console.WriteLine($"描述: {video.Description}");
        return title;
    }

    public async Task<FFmpegTaskDto> TestM3U8(string m3u8)
    {
        string command = $"-i \"{m3u8}\" -c copy \"{_downloadPath}.mp4\"";
        var task = ffManager.StartFFmpeg(command, "missav");
        Console.WriteLine($"下载完成: {DateTime.Now}");
        await task.Process.WaitForExitAsync();
        return FFmpegManager.ConvertDto(task);
    }
}
