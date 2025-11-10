using System.Net.Http;
using System.Diagnostics;
using Newtonsoft.Json.Linq;

namespace FetchVideo.Controllers;

public class BilibiliController
{
    // 视频下载 bvId 👉 cid/part 👉 url
    public async Task GetBilibiliVideoAsync(string bvId)
    {
        var httpClient = new HttpClient();

        // 1. 获取 cid
        string finalUrl = $"{Shared.BILI_PLAYER}pagelist?bvid={bvId}&jsonp=jsonp";
        //Console.WriteLine($"URL是: {finalUrl}");
        string pagelistJson = await httpClient.GetStringAsync(finalUrl);
        //Console.WriteLine($"返回值: {pagelistJson}");
        var jsonPage = JObject.Parse(pagelistJson);
        string cid = jsonPage["data"]?[0]?["cid"]?.ToString();
        Console.WriteLine($"cid是: {cid}");
        string part = Shared.MakeFileNameSafe(jsonPage["data"]?[0]?["part"]?.ToString());
        Console.WriteLine($"视频标题是: {part}");


        // 2. 获取视频 URL
        var apiUrl = $"{Shared.BILI_PLAYER}playurl?bvid={bvId}&cid={cid}&qn=80&fnval=16";
        var playUrlJson = await httpClient.GetStringAsync(apiUrl);
        //Console.WriteLine($"返回值: {playUrlJson}");
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

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string videoFile = Path.Combine(desktopPath, "video.m4s");
        string audioFile = Path.Combine(desktopPath, "audio.m4s");
        string outputFile = Path.Combine(desktopPath, $"{(string.IsNullOrEmpty(part) ? "output" : part)}.mp4");

        string referer = $"{Shared.BILI_VIDEO}{bvId}";
        //await DownloadFileAsync(videoUrl, videoFile); //403 Forbidden
        await DownloadBilibiliM4sAsync(videoUrl, referer, videoFile);
        Console.WriteLine($"视频下载: {videoFile}");
        //await DownloadFileAsync(audioUrl, audioFile); //403 Forbidden
        await DownloadBilibiliM4sAsync(audioUrl, referer, audioFile);
        Console.WriteLine($"音频下载: {audioFile}");

        // 调用
        Shared.MergeAudioVideo(videoFile, audioFile, outputFile);
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
        string finalUrl = $"{Shared.BILI_INTERFACE}view?bvid={bvId}";
        var httpClient = new HttpClient();
        string json = await httpClient.GetStringAsync(finalUrl);
        //Console.WriteLine($"返回值: {json}");
        var jsonObject = JObject.Parse(json);
        var mid = jsonObject["data"]["owner"]["mid"]; //B站Uid
        var name = jsonObject["data"]["owner"]["name"]; //B站用户名
        var face = jsonObject["data"]["owner"]["face"]; //头像
        Console.WriteLine($"Up主: {name} : {mid}");
    }


    // 直播流
    string roomUrl = "https://api.live.bilibili.com/room/v1/Room/";
    //https://live.bilibili.com/1792597682
    public async Task GetM3U8(string room_id)
    {
        string finalUrl = $"{roomUrl}playUrl?cid={room_id}&platform=web";
        //Console.WriteLine($"URL是: {finalUrl}");
        var httpClient = new HttpClient();
        string roomJson = await httpClient.GetStringAsync(finalUrl);
        Console.WriteLine($"返回值: {roomJson}");
        var jsonData = JObject.Parse(roomJson);
        string u3u8 = jsonData["data"]?["durl"]?[0]?["url"]?.ToString();
        Console.WriteLine($"u3u8是: {u3u8}");

        Shared.M3U8toMP4(room_id, u3u8, "Download\\live_record.mp4");
        /*
        var psi = new ProcessStartInfo
        {
            FileName = "D:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe", // ffmpeg.exe 路径
            Arguments = $"-headers \"Referer: https://live.bilibili.com/{room_id}\r\nUser-Agent: Mozilla/5.0\" -i \"{u3u8}\" -c copy \"live_record.mp4\" -y",
            UseShellExecute = false,
            CreateNoWindow = false, //关键①，true不执行
        };
        var ffmpeg = Process.Start(psi);
        ffmpeg.WaitForExit();
        */

        /*
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        http.DefaultRequestHeaders.Add("Referer", "https://live.bilibili.com/房间号");

        using var response = await http.GetAsync(u3u8, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync();
        await using var file = File.Create("live_record.flv");

        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer)) > 0)
        {
            await file.WriteAsync(buffer.AsMemory(0, read));
            totalRead += read;
            Console.Write($"\r已下载: {totalRead / 1024 / 1024.0:F2} MB");
        }
        Console.WriteLine("\n✅ 录制完成");
        */
    }
}