using FetchVideo.Models;
using FetchVideo.Utils;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using System.Text.Json;

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
        Console.WriteLine($"m3u8是: {m3u8Url}");

        // FFmpeg 转码
        string dateFolder = DateTime.Now.ToString("yyyy-MM-dd"); //"2025-12-09";
        string desktopPath = Path.Combine(_downloadPath, dateFolder);
        if (Directory.Exists(desktopPath) == false)
        {
            Directory.CreateDirectory(desktopPath); // 当天的子文件夹
            Console.WriteLine($"创建文件夹: {desktopPath}");
        }
        string outputFile = Path.Combine(desktopPath, $"{title}.mp4");
        Console.WriteLine($"outputFile: {outputFile}");
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
    // ❌仅适用直播，视频，个人主页无法获取完整html❌
    // 无头浏览器 MicroSoft.Playwright
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

    // 分析B站个人主页视频列表（严查，好几分钟才敢用一次）
    public async Task<List<string>> GetUploadVideosAsync(long mid, int page = 1, int pageSize = 20)
    {
        using var client = new HttpClient();
        // 模拟浏览器，避免部分风控
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        string referer = $"https://space.bilibili.com/{mid}";
        client.DefaultRequestHeaders.Add("Referer", referer);


        var url = $"{Shared.BILI_SPACE}arc/search?mid={mid}&pn={page}&ps={pageSize}&order=pubdate&jsonp=jsonp";
        var json = await client.GetStringAsync(url);
        Console.WriteLine("👇json👇");
        Console.WriteLine(json);

        // 用 System.Text.Json 解析（或 Json.NET）
        var doc = JsonDocument.Parse(json);
        var vlist = doc.RootElement
            .GetProperty("data")
            .GetProperty("list")
            .GetProperty("vlist");

        List<string> videoList = new List<string> ();
        foreach (var video in vlist.EnumerateArray())
        {
            string title = video.GetProperty("title").GetString();
            string bvId = video.GetProperty("bvid").GetString();
            videoList.Add(bvId);

            Console.WriteLine($"标题: {title}");
            Console.WriteLine($"BV: {bvId}");
            //Console.WriteLine($"播放: {video.GetProperty("play").GetInt32()}");
            Console.WriteLine("---");
        }
        return videoList;
    }
    // 使用 Playwright 模仿浏览器行为获取 html
    // ❌Playwright放服务器太重了，直接 +200MB，编译5分钟❌
    // ❌B站对Linux反爬更严格，建议功能移到客户端❌
    public async Task<string> GetHTML(string url)
    {
        // 自动安装浏览器（第一次运行会下载 Chromium，后面就不会了）
        using var playwright = await Playwright.CreateAsync();

        // 无头模式（不显示浏览器窗口）
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            //Headless = true,  // 改成 false 可以看到浏览器窗口，便于调试
            Headless = false,  // 先改成 false！有窗口才能通过大部分检测
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-infobars",
                "--window-position=0,0",
                "--disable-extensions",
                "--disable-blink-features=AutomationControlled"
            }
        });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1920, Height = 1080 },
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36"
        });

        // 关键反检测代码（这一段必须加！）
        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => false });
            window.chrome = { runtime: {}, app: {}, loadTimes: () => {} };
            Object.defineProperty(navigator, 'languages', { get: () => ['zh-CN', 'zh'] });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
        ");
        var page = await browser.NewPageAsync();

        await page.GotoAsync(url);

        // 等待页面主要内容加载完成（推荐用 NetworkIdle，比固定延时更可靠）
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 可选：再等一下确保动态内容渲染完（B 站有时稍慢）
        await Task.Delay(3000);

        // 获取完整的渲染后 HTML
        string html = await page.ContentAsync();

        // 输出到控制台（实际项目可以保存到文件）
        Console.WriteLine("=== 页面 HTML 长度 ===");
        Console.WriteLine(html.Length);
        Console.WriteLine("\n=== 前 1000 个字符预览 ===");
        Console.WriteLine(html.Substring(0, Math.Min(1000, html.Length)));

        // 保存到文件（可选）
        await System.IO.File.WriteAllTextAsync("C:\\Users\\33913\\Desktop\\up主页面.html", html);
        Console.WriteLine("完整 HTML 已保存到 up主页面.html");

        // 按任意键退出
        //Console.WriteLine("按任意键退出...");
        //Console.ReadKey();

        return html;
    }
    [HttpGet("upload_video")]
    public async Task<List<string>> GetUploadVideo(string uid)
    {
        // 替换成你要抓的 UP 主 UID
        //string uid = "502793565";  // 示例：某个 UP 主
        string url = $"https://space.bilibili.com/{uid}/upload/video";

        // 获取完整的渲染后 HTML
        string html = await GetHTML(url);

        // 用 HtmlAgilityPack 解析 HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var videoNodes = doc.DocumentNode.SelectNodes("//a[@class='bili-cover-card']");
        Console.WriteLine($"UP有{videoNodes.Count}个视频");

        List<string> videoList = new List<string>();
        foreach (var video in videoNodes)
        {
            string href = video.GetAttributeValue("href", "");
            string bvId = Shared.GetBvId(href);
            Console.WriteLine($"{href}👉{bvId}");
            videoList.Add(bvId);
        }
        return videoList;
    }

    [HttpGet("upload_test")]
    public async Task<string> TestAsync(string uid)
    {
        // 获取收藏夹列表
        string url = $"https://api.bilibili.com/x/v3/fav/folder/created/list-all?up_mid={uid}";
        var resp = await RequestAsync(url);
        Console.WriteLine(resp);
        return resp;
    }
    private static readonly HttpClient client = new HttpClient();
    // ←←← 这里填你的 SESSDATA（必须登录有效）
    private const string SessData = "f0ce3d2c%2C1780465626%2C7172e%2Ac2CjBGS5AJwPcfnaaVc8XogrSvBMwv_ARSaHY0GVUqDuByTCC9RpyOO_86Ks4WuQE1whASVl9Zb2JMVDlPMVBNTEkxdnhhdUJlajFMTkpCeWU2aVV0Z21PVnR2TDVkWlMyY2c2V20yaE1sSTQ4d3o1MzlhZzJaOElMMmVpejN1OVpKbTBmU1B5RHpnIIEC";
    // 你的 mid（用户ID），可通过 https://api.bilibili.com/x/space/myinfo 获取
    private static long MyMid = 3546649320229192;   // ←←← 改成自己的 mid
    // 统一请求方法（自动加 Cookie 和常见 Header）
    static DateTime UnixTimestampToDateTime(long timestamp)
        => DateTimeOffset.FromUnixTimeSeconds(timestamp).ToLocalTime().DateTime;
    static async Task<string> RequestAsync(string url)
    {
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={SessData}");
        client.DefaultRequestHeaders.Remove("User-Agent");
        client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStringAsync();
    }
    // 获取指定收藏夹的所有视频
    static async Task<List<string>> GetVideosInFolderAsync(long mediaId)
    {
        int pn = 1;
        int ps = 20; // ←←← 这里改成 20（最大值）
        bool hasMore = true;
        //string json = null; //调试打印用
        List<string> videoList = new List<string>();

        int index = 0;
        while (hasMore)
        {
            string url = $"https://api.bilibili.com/x/v3/fav/resource/list" +
                         $"?media_id={mediaId}&pn={pn}&ps={ps}&platform=web";

            var resp = await RequestAsync(url);
            var result = JsonSerializer.Deserialize<BiliFavResourceResponse>(resp, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            //json = resp;
            //Console.WriteLine("打印json");
            //Console.WriteLine(json);
            //Console.WriteLine("打印是否翻页");
            //Console.WriteLine(result?.Data.has_more);


            if (result?.Code != 0)
            {
                Console.WriteLine($"获取视频失败: {result?.Message}");
                break;
            }

            if (result.Data?.Medias == null || result.Data.Medias.Count == 0)
            {
                Console.WriteLine("该收藏夹暂无视频");
                break;
            }

            foreach (var v in result.Data.Medias)
            {
                index++;
                videoList.Add(v.Bvid);

                Console.WriteLine($"No.{index}");
                Console.WriteLine($"标题: {v.Title}");
                Console.WriteLine($"BV号: BV{v.Bvid}");
                Console.WriteLine($"链接: https://www.bilibili.com/video/BV{v.Bvid}");
                //Console.WriteLine($"封面: {v.Cover}");
                //Console.WriteLine($"播放量: {v.CntInfo.Play}   收藏时间: {UnixTimestampToDateTime(v.FavTime):yyyy-MM-dd HH:mm}");
                Console.WriteLine(new string('-', 20));
            }

            // 判断是否有下一页
            hasMore = result.Data.has_more;  // ←←← 这个字段就是翻页标志
            Console.WriteLine($"判断翻页.{hasMore}");
            if (hasMore)
            {
                Console.WriteLine($"已加载第 {pn} 页，还有更多...（共约 {result.Data.Info.MediaCount} 个视频）\n");
            }

            pn++;                     // 翻到下一页
            await Task.Delay(600);    // 防风控，建议 500~1000ms
        }

        //return json;
        return videoList;
    }

    [HttpGet("favlist")]
    public async Task<List<string>> GetFavList(string uid)
    {
        // 一般就用一个，写死即可
        //默认：https://space.bilibili.com/3546649320229192/favlist?fid=3108098292&ftype=create
        //下载：https://space.bilibili.com/3546649320229192/favlist?fid=3573957792&ftype=create

        // 找 <a class="bili-cover-card" href="...BV...">

        // 替换成你要抓的 UP 主 UID
        //string uid = "502793565";  // 示例：某个 UP 主
        //string url = $"https://space.bilibili.com/{uid}/favlist?fid=3573957792&ftype=create";

        /*
        // 获取完整的渲染后 HTML
        string html = await GetHTML(url);

        // 用 HtmlAgilityPack 解析 HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var videoNodes = doc.DocumentNode.SelectNodes("//a[@class='bili-cover-card']");
        Console.WriteLine($"收藏了有{videoNodes.Count}个视频");

        List<string> videoList = new List<string>();
        foreach (var video in videoNodes)
        {
            string href = video.GetAttributeValue("href", "");
            string bvId = Shared.GetBvId(href);
            Console.WriteLine($"{href}👉{bvId}");
            videoList.Add(bvId);
        }
        return videoList;
        */

        // 获取收藏家内视频列表
        return await GetVideosInFolderAsync(3573957792);
    }

    // 打印容器当前运行的下载任务
    [HttpGet("running_tasks")]
    public ActionResult<List<FFmpegTaskDto>> GetRunningTasks()
    {
        var running = _manager.GetRunningTasks();
        return Ok(running);
    }

    // 提交JSON {"user":"admin"}
    [HttpPost("stop_tasks")]
    public async Task<ActionResult<List<FFmpegTaskDto>>> StopRunningTasks([FromBody] StopRequest stopUser)
    {
        var running = _manager.GetRunningTasks();
        Console.WriteLine($"{stopUser.User} 要求停止, 当前任务{running.Count}个");

        await _manager.StopTasks();
        return Ok(running); //[] 👉 0个任务，成功
    }
}
