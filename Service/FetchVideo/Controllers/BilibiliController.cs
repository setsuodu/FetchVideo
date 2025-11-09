using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace FetchVideo.Controllers;

public class BilibiliController
{
    // BvId 👉 cid 👉 url
    //var webUrl = "https://www.bilibili.com/video/BV1ysySBsExt/"; // B站视频
    string baseUrl = "https://api.bilibili.com/x/player/";
    string jsonp = "jsonp"; // 假设 jsonp 也是一个参数
    string bvId = "BV1Xe1LB5ENJ";
    string referer(string bvId) => $"https://www.bilibili.com/video/{bvId}";
    string part = ""; //视频名称

    public async Task GetBilibiliVideoAsync(string bvId)
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
        await DownloadBilibiliM4sAsync(videoUrl, referer(bvId), videoFile);
        Console.WriteLine($"视频下载: {videoFile}");
        //await DownloadFileAsync(audioUrl, audioFile); //403 Forbidden
        await DownloadBilibiliM4sAsync(audioUrl, referer(bvId), audioFile);
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

    // 获取该视频 Up 主信息
    public async Task GetBilibiliUpInfoAsync(string bvId)
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

    //await GetBilibiliVideoAsync(bvId);

    //await GetBilibiliUpInfoAsync(bvId);



    public static void MergeAudioVideo(string videoPath, string audioPath, string outputPath)
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
    public static string MakeFileNameSafe(string name)
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

}