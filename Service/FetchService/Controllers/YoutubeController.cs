using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace FetchService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class YoutubeController : ControllerBase
{
    private readonly string _downloadPath;
    private readonly FFmpegProcessManager _manager;

    // 从构造函数注入配置，变成本地只读（推荐写法！）
    public YoutubeController(IConfiguration configuration, FFmpegProcessManager manager)
    {
        // 如果配置中没找到，就用 "/app/downloads";
        _downloadPath = configuration["DownloadPath"] ?? "/app/downloads";
        _manager = manager;
    }

    // 创建进度回调
    Progress<double> progress = new Progress<double>(p =>
    {
        Console.Write($"\r下载进度: {p:P1}"); // P1 = 百分比(一位小数)
    });

    public async Task<FFmpegProcessInfo> GetYoutubeVideoAsync(string url)
    {
        string part = await GetVideoInfoAsync(url);
        //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string desktopPath = _downloadPath;
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
            var processInfo = new FFmpegProcessInfo
            {
                Command = "Normal",
                StartTime = DateTime.Now,
                Status = "Completed"
            };
            return processInfo;
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

            // FFmpeg 合并
            string mergeCMD = $"-i \"{videoFile}\" -i \"{audioFile}\" -c copy \"{outputFile}\" -y";
            var processInfo = _manager.StartFFmpeg(mergeCMD);
            Console.WriteLine($"下载完成: {DateTime.Now}");
            await processInfo.process.WaitForExitAsync();
            System.IO.File.Delete(videoFile);
            System.IO.File.Delete(audioFile);
            processInfo.Command = "Merge";
            return processInfo;
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
        return Shared.MakeFileNameSafe(video.Title);
    }

    // missav
    public async Task<FFmpegProcessInfo> GetM3U8(string m3u8)
    {
        string mergeCMD = $"-i \"{m3u8}\" -c copy \"{_downloadPath}.mp4\"";
        var processInfo = _manager.StartFFmpeg(mergeCMD);
        Console.WriteLine($"下载完成: {DateTime.Now}");
        await processInfo.process.WaitForExitAsync();
        processInfo.Command = "Convert";
        return processInfo;
    }
}
