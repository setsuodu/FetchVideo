using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
//using HtmlAgilityPack; // 需要安装的库
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

/*
var httpClient = new HttpClient();
//string url = "https://av-wiki.net/";
//string url = "https://seesaawiki.jp/w/sougouwiki/d/%BB%B0%BE%E5%CD%AA%B0%A1"; //三上悠亚
string url = "https://";
var html = await httpClient.GetStringAsync(url); // 这样获取html是压缩的，用HtmlAgilityPack格式化
Console.WriteLine(html);

string filePath = @"C:\Users\33913\Desktop\index.html";
File.WriteAllText(filePath, html);
Console.WriteLine($"saved in {filePath}");
*/

#region Bilibili

// BvId 👉 cid 👉 url
//var webUrl = "https://www.bilibili.com/video/BV1ysySBsExt/"; // B站视频
string baseUrl = "https://api.bilibili.com/x/player/";
string jsonp = "jsonp"; // 假设 jsonp 也是一个参数
string bvId = "BV1ysySBsExt";
string referer = $"https://www.bilibili.com/video/{bvId}";
string part = ""; //视频名称

async Task GetBilibiliVideoAsync(string bvId)
{
    var httpClient = new HttpClient();


    // 1. 获取 cid
    string finalUrl = $"{baseUrl}pagelist?bvid={bvId}&jsonp={jsonp}";
    //Console.WriteLine($"URL是: {finalUrl}");
    string pagelistJson = await httpClient.GetStringAsync(finalUrl);
    //Console.WriteLine($"返回值: {pagelistJson}");
    var jsonPage = JObject.Parse(pagelistJson);
    string cid = jsonPage["data"]?[0]?["cid"]?.ToString();
    Console.WriteLine($"Cid是: {cid}");

    part = MakeFileNameSafe(jsonPage["data"]?[0]?["part"]?.ToString());
    Console.WriteLine($"标题是: {part}");


    // 2. 获取视频 URL
    var apiUrl = $"{baseUrl}playurl?bvid={bvId}&cid={cid}&qn=80&fnval=16";
    var playUrlJson = await httpClient.GetStringAsync(apiUrl);
    Console.WriteLine($"返回值: {playUrlJson}");
    var jsonPlayer = JObject.Parse(playUrlJson);

    var videoArray = jsonPlayer["data"]?["dash"]?["video"] as JArray;
    var bestVideo = videoArray.OrderByDescending(v => (int)v["width"]).First();
    var videoUrl = bestVideo["baseUrl"].ToString();
    Console.WriteLine($"视频地址: {videoUrl}");

    var audioArray = jsonPlayer["data"]?["dash"]?["audio"] as JArray;
    var bestAudio = audioArray.OrderByDescending(a => (int)a["bandwidth"]).First();
    var audioUrl = bestAudio["baseUrl"].ToString();
    Console.WriteLine($"音频地址: {audioUrl}");


    // 3. 下载到本地
    // videoArray, audioArray 已从 JSON 获取
    var video = videoArray.OrderByDescending(v => (int)v["width"]).First();
    var audio = audioArray.OrderByDescending(a => (int)a["bandwidth"]).First();

    if (!Directory.Exists("temp"))
        Directory.CreateDirectory("temp");
    string videoFile = "temp\\video.m4s";
    string audioFile = "temp\\audio.m4s";
    string outputFile = $"temp\\{(string.IsNullOrEmpty(part) ? "output" : part)}.mp4";

    //await DownloadFileAsync(videoUrl, videoFile); //403 Forbidden
    await DownloadBilibiliM4sAsync(videoUrl, referer, videoFile);
    Console.WriteLine($"视频下载: {videoFile}");
    //await DownloadFileAsync(audioUrl, audioFile); //403 Forbidden
    await DownloadBilibiliM4sAsync(audioUrl, referer, audioFile);
    Console.WriteLine($"音频下载: {audioFile}");

    // 调用
    MergeAudioVideo(videoFile, audioFile, outputFile);
    Console.WriteLine($"合并完成: {outputFile}");
}

// 普通下载 (403 Forbidden：缺少 Referer 或 User-Agent)
async Task DownloadFileAsync(string url, string filePath)
{
    using var http = new HttpClient();
    using var response = await http.GetAsync(url);
    response.EnsureSuccessStatusCode();
    await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
    await response.Content.CopyToAsync(fs);
}

// B站验证下载
async Task DownloadBilibiliM4sAsync(string url, string referer, string outputPath)
{
    using var http = new HttpClient();
    http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
    http.DefaultRequestHeaders.Add("Referer", referer);

    using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
    response.EnsureSuccessStatusCode();

    var totalBytes = response.Content.Headers.ContentLength ?? -1L;
    var canReportProgress = totalBytes != -1;

    await using var stream = await response.Content.ReadAsStreamAsync();
    await using var file = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);

    var buffer = new byte[81920];
    long totalRead = 0L;
    int read;
    while ((read = await stream.ReadAsync(buffer)) > 0)
    {
        await file.WriteAsync(buffer.AsMemory(0, read));
        totalRead += read;

        if (canReportProgress)
        {
            double progress = totalRead * 100.0 / totalBytes;
            Console.Write($"\r下载中: {progress:F1}%");
        }
        else
        {
            Console.Write($"\r已下载: {totalRead / 1024.0 / 1024.0:F2} MB");
        }
    }

    Console.WriteLine("\n✅ 下载完成：" + outputPath);
}

void MergeAudioVideo(string videoPath, string audioPath, string outputPath)
{
    var ffmpeg = new Process();
    ffmpeg.StartInfo.FileName = "D:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"; // ffmpeg.exe 路径
    ffmpeg.StartInfo.Arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c copy \"{outputPath}\" -y";
    ffmpeg.StartInfo.UseShellExecute = false;
    ffmpeg.StartInfo.CreateNoWindow = true;
    ffmpeg.Start();
    ffmpeg.WaitForExit();
}

// Windows文件名不允许文件名含（\ / : * ? " < > |）
// 替换为 下划线 _
static string MakeFileNameSafe(string name)
{
    // 常见所有系统的不合法字符
    //char[] invalidChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }; //跨平台写法
    char[] invalidChars = Path.GetInvalidFileNameChars(); //Windows写法
    
    // 过滤
    foreach (char c in invalidChars)
    {
        name = name.Replace(c, '_');
    }
    return name;
}

// 获取该视频 Up 主信息
async Task GetBilibiliUpInfoAsync(string bvId)
{
    string url = "https://api.bilibili.com/x/web-interface/view?bvid=BV1ysySBsExt";
    var httpClient = new HttpClient();
    string json = await httpClient.GetStringAsync(url);
    Console.WriteLine($"返回值: {json}");
    var jsonObject = JObject.Parse(json);
    var mid = jsonObject["data"]["owner"]["mid"]; //B站Uid
    var name = jsonObject["data"]["owner"]["name"]; //B站用户名
    var face = jsonObject["data"]["owner"]["face"]; //头像
    Console.WriteLine($"Up主: {name} : {mid}");
}

await GetBilibiliVideoAsync(bvId);

//await GetBilibiliUpInfoAsync(bvId);

#endregion

#region Youtube
// 创建进度回调
var progress = new Progress<double>(p =>
{
    Console.Write($"\r下载进度: {p:P1}"); // P1 = 百分比(一位小数)
});

async Task GetYoutubeVideoAsync(string url)
{
    if (!Directory.Exists("temp"))
        Directory.CreateDirectory("temp");
    string videoFile = "temp\\video.mp4";
    string audioFile = "temp\\audio.m4a";
    string outputFile = $"temp\\{(string.IsNullOrEmpty(part) ? "output" : part)}.mp4";
    Console.WriteLine($"outputFile是: {MakeFileNameSafe(outputFile)}");

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
        MergeAudioVideo(videoFile, audioFile, outputFile);
        Console.WriteLine($"合并完成: {outputFile}");
    }
}

async Task GetVideoInfoAsync(string url)
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

string fullUrl = "https://www.youtube.com/watch?v=ij89E9qABho";
string longUrl = "https://youtu.be/ij89E9qABho";
string shortUrl = "https://www.youtube.com/shorts/fOlW2f38PFE"; //含标题日文
//string shortUrl = "https://www.youtube.com/shorts/P2EtaBiEDGg";
//string url = "https://www.youtube.com/watch?v=CvDpSRuGsjY";

//await GetVideoInfoAsync(shortUrl);
//await GetYoutubeVideoAsync(shortUrl);
#endregion