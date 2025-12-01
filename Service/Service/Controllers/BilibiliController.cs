using Microsoft.AspNetCore.Mvc;
using FetchVideo.Models;
using FetchVideo.Utils;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace FetchVideo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BilibiliController : ControllerBase
{
    private readonly string _downloadPath;
    private readonly FFmpegProcessManager _manager;

    // 从构造函数注入配置，变成本地只读（推荐写法！）
    public BilibiliController(IConfiguration configuration, FFmpegProcessManager manager)
    {
        // 如果配置中没找到，就用 "/app/downloads";
        _downloadPath = configuration["DownloadPath"] ?? "/app/downloads";
        _manager = manager;
    }

    // 视频下载 bvId 👉 cid/title 👉 url
    [HttpGet("get_bili_video")]
    public async Task<FFmpegProcessInfo> GetBilibiliVideoAsync(string bvId)
    {
        // 获取视频信息
        var videoView = await GetUpInfo(bvId);

        // 1. 获取 cid
        string cid = videoView.cid;


        // 2. 获取视频 URL
        var httpClient = new HttpClient();
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

        // Windows Docker Desktop 调试路径
        string desktopPath = _downloadPath;
        string videoFile = Path.Combine(desktopPath, "video.m4s");
        string audioFile = Path.Combine(desktopPath, "audio.m4s");
        string outputFile = Path.Combine(desktopPath, $"【{videoView.owner.name}】{videoView.title}.mp4");

        string referer = $"{Shared.BILI_VIDEO}{bvId}";
        //await DownloadFileAsync(videoUrl, videoFile); //403 Forbidden
        await DownloadBilibiliM4sAsync(videoUrl, referer, videoFile);
        Console.WriteLine($"视频下载: {videoFile}");
        //await DownloadFileAsync(audioUrl, audioFile); //403 Forbidden
        await DownloadBilibiliM4sAsync(audioUrl, referer, audioFile);
        Console.WriteLine($"音频下载: {audioFile}");

        // FFmpeg 合并
        string mergeCMD = $"-i \"{videoFile}\" -i \"{audioFile}\" -c copy \"{outputFile}\" -y";
        var processInfo = _manager.StartFFmpeg(mergeCMD, videoView.owner.name);
        Console.WriteLine($"开始等待: {DateTime.Now}");
        await processInfo.process.WaitForExitAsync();
        Console.WriteLine($"下载完成: {DateTime.Now}");
        System.IO.File.Delete(videoFile);
        System.IO.File.Delete(audioFile);

        //return Ok(processInfo); // 返回封装对象
        processInfo.Command = "Merge";
        return processInfo;
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


    // bvId 查询 Up 主信息
    [HttpGet("upinfo")]
    public async Task<VideoView> GetUpInfo(string bvId)
    {
        string finalUrl = $"{Shared.BILI_INTERFACE}view?bvid={bvId}";
        var httpClient = new HttpClient();
        string json = await httpClient.GetStringAsync(finalUrl);
        //Console.WriteLine($"返回值: {json}");
        var jsonObject = JObject.Parse(json);
        VideoView view = new VideoView
        {
            owner = new Owner
            {
                mid = jsonObject["data"]["owner"]["mid"].ToString(), //B站Uid
                name = jsonObject["data"]["owner"]["name"].ToString(), //B站用户名
                face = jsonObject["data"]["owner"]["face"].ToString(), //头像
            },
            title = jsonObject["data"]["title"].ToString(),
            cid = jsonObject["data"]["cid"].ToString()
        };
        Console.WriteLine($"up-name={view.owner.name}, title={view.cid}, cid={view.cid}");
        return view;
    }

    // 直播流
    [HttpGet("get_bili_live")]
    public async Task<FFmpegProcessInfo> GetM3U8(string room_id, string title, int minute)
    {
        int second = minute * 60;
        Console.WriteLine($"直播流录制: {second}s 停止");
        string finalUrl = $"{Shared.BILI_ROOM}playUrl?cid={room_id}&platform=web";
        //Console.WriteLine($"URL是: {finalUrl}");
        var httpClient = new HttpClient();
        string roomJson = await httpClient.GetStringAsync(finalUrl);
        //Console.WriteLine($"返回值: {roomJson}");
        var jsonData = JObject.Parse(roomJson);
        string m3u8Url = jsonData["data"]?["durl"]?[0]?["url"]?.ToString();
        Console.WriteLine($"u3u8是: {m3u8Url}");

        // FFmpeg 转码
        string desktopPath = _downloadPath;
        string outputFile = Path.Combine(desktopPath, $"{title}.mp4");
        string convertCMD = $"-headers \"Referer: {Shared.BILI_LIVE}{room_id}\r\nUser-Agent: Mozilla/5.0\" -i \"{m3u8Url}\" -t {second} -c copy \"{outputFile}\" -y"; // -y 直接覆盖同名文件，不用交互式选择
        Console.WriteLine($"FFmpeg命令是: {convertCMD}");
        Console.WriteLine($"标题是: {title}");
        var processInfo = _manager.StartFFmpeg(convertCMD, title);
        processInfo.Command = "Convert";
        return processInfo;
    }
    // 获取直播房间信息
    [HttpGet("get_bili_roominfo")]
    public async Task<RoomInfo> GetRoomInfo(string room_id)
    {
        string finalUrl = $"{Shared.BILI_ROOM}get_info?room_id={room_id}";
        var httpClient = new HttpClient();
        string roomJson = await httpClient.GetStringAsync(finalUrl);
        Console.WriteLine(roomJson);
        var jsonObject = JObject.Parse(roomJson);
        var info = new RoomInfo
        {
            uid = jsonObject["data"]["uid"].ToObject<double>(), //直播间Up主
            live_status = jsonObject["data"]["live_status"].ToObject<byte>(), //是否开播
            title = jsonObject["data"]["title"].ToString(), //直播间标题
            user_cover = jsonObject["data"]["user_cover"].ToString(),
            parent_area_name = jsonObject["data"]["parent_area_name"].ToString(),
            area_name = jsonObject["data"]["area_name"].ToString(),
        };
        return info;
    }

    // 获取B站直播标题
    [HttpGet("title")]
    public async Task<string> GetTitleAsync(string url)
    {
        string title = "找不到 <title> 标签";
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        //string logFile = $"log_{timestamp}.txt";

        using (var http = new HttpClient())
        {
            // 一些 headers 模拟浏览器访问
            http.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/122.0.0.0 Safari/537.36");

            string html = await http.GetStringAsync(url);

            // 用 HtmlAgilityPack 解析 HTML
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null)
                title = titleNode.InnerText.Trim();

            string title_result = Shared.GetMiddleText(title);

            //Console.WriteLine("标题：" + title);
            return $"{title_result}_{timestamp}";
        }
    }

    
    // 打印容器当前运行的下载任务
    [HttpGet("running_tasks")]
    public ActionResult<List<FFmpegTaskDto>> GetRunningTasks()
    {
        var running = _manager.GetRunningTasks();
        return Ok(running);
    }
}
