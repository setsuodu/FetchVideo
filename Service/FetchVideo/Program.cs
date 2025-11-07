using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http;
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
async Task GetBilibiliVideoAsync(string bvId, string cid)
{
    // BvId 👉 cid 👉 url
    //var webUrl = "https://www.bilibili.com/video/BV1ysySBsExt/"; // 目前B站地址

    // 👇解析成API地址
    var apiUrl = $"https://api.bilibili.com/x/player/playurl?bvid={bvId}&cid={cid}&qn=80&fnval=16";
    // cid 需要从网页解析或 API 获取

    var httpClient = new HttpClient();
    var jsonStr = await httpClient.GetStringAsync(apiUrl);
    Console.WriteLine($"返回值: {jsonStr}");

    var json = JObject.Parse(jsonStr);
    //var videoUrl = json["data"]?["dash"]?["video"]?[0]?["baseUrl"]?.ToString();


    var videoArray = json["data"]?["dash"]?["video"] as JArray;
    var bestVideo = videoArray
        .OrderByDescending(v => (int)v["width"])
        .First();
    var videoUrl = bestVideo["baseUrl"].ToString();
    Console.WriteLine($"视频地址: {videoUrl}");


    var audioArray = json["data"]?["dash"]?["audio"] as JArray;
    var bestAudio = audioArray
        .OrderByDescending(a => (int)a["bandwidth"])
        .First();
    var audioUrl = bestAudio["baseUrl"].ToString();
    Console.WriteLine($"音频地址: {audioUrl}");


    // videoArray, audioArray 已从 JSON 获取
    var video = videoArray.OrderByDescending(v => (int)v["width"]).First();
    var audio = audioArray.OrderByDescending(a => (int)a["bandwidth"]).First();

    if (!Directory.Exists("temp"))
        Directory.CreateDirectory("temp");
    string videoFile = "temp\\video.m4s";
    string audioFile = "temp\\audio.m4s";
    string outputFile = "temp\\output.mp4";

    string bv = "BV1ysySBsExt";
    string referer = $"https://www.bilibili.com/video/{bv}";

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
async Task DownloadBilibiliM4sAsync(string url, string referer, string filePath)
{
    using var http = new HttpClient();

    // 模拟浏览器请求头
    http.DefaultRequestHeaders.Add("User-Agent",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
        "(KHTML, like Gecko) Chrome/142.0.0.0 Safari/537.36");
    http.DefaultRequestHeaders.Add("Referer", referer);

    using var response = await http.GetAsync(url);
    response.EnsureSuccessStatusCode();

    await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
    await response.Content.CopyToAsync(fs);
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

await GetBilibiliVideoAsync("BV1ysySBsExt", "33495319307");







#endregion

#region Youtube
async Task GetYoutubeVideoAsync(string url)
{
    var youtube = new YoutubeClient();
    var video = await youtube.Videos.GetAsync(url);
    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(video.Id);

    // 获取最佳 MP4 视频流（6.5.6不可用）
    //var streamInfo = streamManifest.GetMuxed().WithHighestVideoQuality(); //
    //Console.WriteLine($"视频标题: {video.Title}");
    //Console.WriteLine($"下载链接: {streamInfo.Url}");

    // 视频 only
    IVideoStreamInfo videoStreamInfo = streamManifest
        .GetVideoOnlyStreams()
        .Where(s => s.Container == Container.Mp4)   // 可选：选择 MP4 容器
        .GetWithHighestVideoQuality();

    // 音频 only
    IStreamInfo audioStreamInfo = streamManifest
        .GetAudioOnlyStreams()
        .GetWithHighestBitrate();

    // 下载示例
    await youtube.Videos.Streams.DownloadAsync(videoStreamInfo, "video.mp4");
    await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, "audio.mp4");
}
#endregion