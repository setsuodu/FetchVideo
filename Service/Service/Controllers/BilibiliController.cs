using FetchVideo.Models;
using FetchVideo.Services;
using FetchVideo.Utils;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace FetchVideo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BilibiliController : ControllerBase
{
    private readonly ISharedService _sharedService;
    private readonly string _downloadPath;
    private readonly FFmpegManager ffManager;

    public BilibiliController(ISharedService sharedService)
    {
        _sharedService = sharedService;
        _downloadPath = sharedService._downloadPath;
        ffManager = sharedService._ffManager;
    }

    // 视频下载 bvId 👉 cid/title 👉 url
    [HttpGet("get_bili_video")]
    public async Task<FFmpegTaskDto> GetBVAsync(string bvId)
    {
        // 1. 获取 cid
        var videoView = await GetUpInfo(bvId);
        string cid = videoView.cid;

        // 2. 获取视频 URL
        var httpClient = new HttpClient();
        var apiUrl = $"{Shared.BILI_PLAYER}playurl?bvid={bvId}&cid={cid}&qn=80&fnval=16";
        var playUrlJson = await httpClient.GetStringAsync(apiUrl);
        //Console.WriteLine($"返回值: {playUrlJson}");
        var jsonPlayer = JsonNode.Parse(playUrlJson).AsObject();

        var videoArray = jsonPlayer["data"]?["dash"]?["video"]?.AsArray();
        var bestVideo = videoArray.OrderByDescending(v => (int)v["width"]).First();
        var videoUrl = bestVideo["baseUrl"].ToString();
        //Console.WriteLine($"视频地址: {videoUrl}");

        var audioArray = jsonPlayer["data"]?["dash"]?["audio"]?.AsArray();
        var bestAudio = audioArray.OrderByDescending(a => (int)a["bandwidth"]).First();
        var audioUrl = bestAudio["baseUrl"].ToString();
        //Console.WriteLine($"音频地址: {audioUrl}");

        // videoArray, audioArray 已从 JSON 获取
        var video = videoArray.OrderByDescending(v => (int)v["width"]).First();
        var audio = audioArray.OrderByDescending(a => (int)a["bandwidth"]).First();

        // 3. 输出路径
        string desktopPath = _downloadPath;
        string videoFile = Path.Combine(desktopPath, "video.m4s");
        string audioFile = Path.Combine(desktopPath, "audio.m4s");
        string outputFile = Path.Combine(desktopPath, $"【{videoView.owner.name}】{Shared.MakeFileNameSafe(videoView.title)}.mp4");

        // 4. 下载到本地
        string referer = $"{Shared.BILI_VIDEO}{bvId}";
        await DownloadBilibiliM4sAsync(videoUrl, referer, videoFile);
        Console.WriteLine($"视频下载: {videoFile}");
        await DownloadBilibiliM4sAsync(audioUrl, referer, audioFile);
        Console.WriteLine($"音频下载: {audioFile}");

        // 5. FFmpeg 合并
        string command = $"-i \"{videoFile}\" -i \"{audioFile}\" -c copy \"{outputFile}\" -y";
        var task = ffManager.StartFFmpeg(command); //BV视频
        Console.WriteLine($"开始时间: {DateTime.Now}");
        await task.Process.WaitForExitAsync();
        Console.WriteLine($"完成时间: {DateTime.Now}");
        System.IO.File.Delete(videoFile);
        System.IO.File.Delete(audioFile);

        return FFmpegManager.ConvertDto(task);
    }
    // B站视频验证下载
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
        var jsonObject = JsonNode.Parse(json).AsObject();
        VideoView view = new VideoView
        {
            owner = new Owner
            {
                mid = jsonObject["data"]?["owner"]?["mid"]?.ToString(), //B站Uid
                name = jsonObject["data"]?["owner"]?["name"]?.ToString(), //B站用户名
                face = jsonObject["data"]?["owner"]?["face"]?.ToString(), //头像
            },
            title = jsonObject["data"]?["title"]?.ToString(),
            cid = jsonObject["data"]?["cid"]?.ToString()
        };
        Console.WriteLine($"up-name={view.owner.name}, title={view.cid}, cid={view.cid}");
        return view;
    }

    // B站直播流录制
    [HttpGet("get_bili_live")]
    public async Task<FFmpegTaskDto> LiveRecord(string url, int minute, bool subscribe = false)
    {
        string room_id = Shared.GetRoomId(url);
        //Console.WriteLine($"是 Bilibili直播: 房间: {room_id}");
        string up_name = await GetTitleAsync(url);
        //Console.WriteLine($"直播标题: {title}");

        int second = minute * 60;
        string finalUrl = $"{Shared.BILI_ROOM}playUrl?cid={room_id}&platform=web";
        //Console.WriteLine($"URL是: {finalUrl}");

        // 请求 B站 API，获取地址
        var httpClient = new HttpClient();
        string roomJson = await httpClient.GetStringAsync(finalUrl);
        //Console.WriteLine($"roomJson: {roomJson}");
        var jsonData = JsonNode.Parse(roomJson).AsObject();
        //string m3u8Url = jsonData["data"]?["durl"]?[0]?["url"]?.ToString();
        var durlArray = jsonData["data"]?["durl"]?.AsArray();
        //Console.WriteLine($"durlArray: {durlArray.Count()}"); //3
        string[] urls = new string[durlArray.Count()];
        for (int i = 0; i < durlArray.Count(); i++)
        {
            urls[i] = jsonData["data"]?["durl"]?[i]?["url"]?.ToString();
            //Console.WriteLine($"[{i}]: {urls[i]}");
        }
        //string m3u8Url = urls[0];
        string m3u8Url = await Shared.GetFirstValidUrlAsync(urls);
        //Console.WriteLine($"m3u8Url: {m3u8Url}");

        //https://api.live.bilibili.com/xlive/web-room/v1/index/getInfoByRoom?room_id=1975553478
        string pkUrl = $"{Shared.BILI_PK}?room_id={room_id}";
        //Console.WriteLine($"pkUrl: {pkUrl}");
        string pkJson = await RequestAsync(pkUrl);
        //Console.WriteLine($"pkJson: {pkJson}");
        var jsonData2 = JsonNode.Parse(pkJson).AsObject();
        // 非PK时，"universal_interact_info_v2": null,
        var is_pk = jsonData2["data"]?["universal_interact_info_v2"] != null;
        if (is_pk)
        {
            var members = jsonData2["data"]?["universal_interact_info_v2"]?["members"]?.AsArray();
            Console.WriteLine($"→→PK人数: {members.Count}");
            foreach (var up in members)
            {
                var _uname = up["uname"];
                var _room_id = up["room_id"];
                Console.WriteLine($"→→{_uname}的房间是{_room_id}");

                up_name += $"([vs]{_uname})";
            }
        }
        else
        {
            Console.WriteLine("→→没PK");
        }

        // 输出目录
        string dateFolder = DateTime.Now.ToString("yyyy-MM-dd"); //"2025-12-09";
        string desktopPath = Path.Combine(_downloadPath, dateFolder);
        if (Directory.Exists(desktopPath) == false)
        {
            Directory.CreateDirectory(desktopPath); // 当天的子文件夹
            Console.WriteLine($"创建文件夹: {desktopPath}");
        }
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string outputFile = Path.Combine(desktopPath, $"{up_name}_{timestamp}.mp4");
        Console.WriteLine($"outputFile: {outputFile}");

        // FFmpeg 转码
        string command = $"-headers \"Referer: {Shared.BILI_LIVE}{room_id}\r\nUser-Agent: Mozilla/5.0\" -i \"{m3u8Url}\" -t {second} -c copy \"{outputFile}\" -y"; // -y 直接覆盖同名文件，不用交互式选择
        Console.WriteLine($"FFmpeg命令是: {command}");
        var task = ffManager.StartFFmpeg(command, up_name, room_id, minute); //B站直播


        var roomInfo = await GetRoomInfo(room_id);
        if (!string.IsNullOrEmpty(roomInfo.user_cover))
        {
            try
            {
                string savedPath = await HttpRemote.DownloadImageAsync(
                    roomInfo.user_cover,
                    desktopPath,
                    $"{up_name}_{timestamp}.jpg"  // 可选，自定义文件名，不带扩展名也可以
                );
                Console.WriteLine($"封面已保存到: {savedPath}");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"封面下载失败: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("没有封面");
        }

        // 1. 初始化 LinkItem 记录（默认设为 false 入库）
        LinkItem link = new LinkItem
        {
            Name = up_name,
            RoomId = Shared.GetRoomId(Shared.CleanUrl(url)),
            IsSubscribed = false,
        };

        // 2. 尝试添加新人入库（如果已存在则不重复添加）
        bool added = await _sharedService.AddNewLiveRoom(link);

        // 3. 处理 subscribe 参数
        // 如果 subscribe 是 false：直接跳过，什么都不做
        // 如果 subscribe 是 true：直接在数据库中强制设为 true（只设为 true，绝不取反关掉）
        if (subscribe)
        {
            var dbItem = await _sharedService._context.LinkItems
                .FirstOrDefaultAsync(l => l.RoomId == link.RoomId);

            if (dbItem != null)
            {
                dbItem.IsSubscribed = true; // 强制设为 true，安全、防抖、符合逻辑

                await _sharedService._context.SaveChangesAsync();
                Console.WriteLine($"[{up_name}] 录制时传入了 subscribe=true，已确保该直播间处于【已订阅】状态。");
            }
        }

        // === 更新最后录制时间 ===
        await _sharedService._context.UpdateLastRecordedAsync(room_id);   // 注意 _sharedService 有 _context
        Console.WriteLine($"[{up_name}] 录制启动成功，已更新 LastRecordedAt");

        return FFmpegManager.ConvertDto(task);
    }

    // 获取直播房间信息
    [HttpGet("get_bili_roominfo")]
    public async Task<RoomInfo> GetRoomInfo(string room_id)
    {
        string finalUrl = $"{Shared.BILI_ROOM}get_info?room_id={room_id}";
        string roomJson = await RequestAsync(finalUrl);
        Console.WriteLine(roomJson);
        var jsonObject = JsonNode.Parse(roomJson).AsObject();
        var info = new RoomInfo
        {
            uid = jsonObject["data"]["uid"].GetValue<double>(), //直播间Up主
            live_status = jsonObject["data"]["live_status"].GetValue<byte>(), //是否开播
            title = jsonObject["data"]?["title"]?.ToString(), //直播间标题
            user_cover = jsonObject["data"]?["user_cover"]?.ToString(),
            parent_area_name = jsonObject["data"]?["parent_area_name"]?.ToString(),
            area_name = jsonObject["data"]?["area_name"]?.ToString(),
        };
        return info;
    }

    // 获取B站直播标题
    // ❌仅适用直播，视频，个人主页无法获取完整html❌
    [HttpGet("title")]
    public async Task<string> GetTitleAsync(string url)
    {
        string title = "找不到 <title> 标签";

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
            return title_result;
        }
    }

    private static readonly HttpClient client = new HttpClient();
    // ←←← 这里填你的 SESSDATA（必须登录有效）
    // 【获取方法】直播页F12→Application→Storage→Cookies→bilibili（有效期半年左右）
    private const string SessData = "f0ce3d2c%2C1780465626%2C7172e%2Ac2CjBGS5AJwPcfnaaVc8XogrSvBMwv_ARSaHY0GVUqDuByTCC9RpyOO_86Ks4WuQE1whASVl9Zb2JMVDlPMVBNTEkxdnhhdUJlajFMTkpCeWU2aVV0Z21PVnR2TDVkWlMyY2c2V20yaE1sSTQ4d3o1MzlhZzJaOElMMmVpejN1OVpKbTBmU1B5RHpnIIEC";
    private const string WebCookie = "buvid3=5CC72C42-1C15-B97D-A8BE-7E259C059E2846197infoc; b_nut=1763480746; _uuid=F64634EC-41D1-68C8-19F7-47EC4994DB9C47694infoc; buvid_fp=ac30564e89319fbb34558e05cf7787cd; buvid4=D22D5F87-3975-7BFF-63CB-D8CFC09209F747329-025111823-LfQJGmB1N2u9vWgqZ5LdlA%3D%3D; SESSDATA=6fcecffe%2C1779032799%2C1640c%2Ab1CjCdBZacmoJHtNvDoxNSPR_nyMXcHOysWkMtlhDLl1CDG0znFFCCMvATAXTucJ9P2tQSVlJCV2h3S1J0TlFFZ2Zsc1dzZU9aNVRKN3NXRGZlZWZhMzF5NTZ3VTcyak5paG4xNHdwZzlna1VoMVFsVm1wVDZqMnZua2V3b1Q5Rm1UbU9sZFFyUXBnIIEC; bili_jct=809e7003559471e244a1cb8fa9386bc5; DedeUserID=20573602; DedeUserID__ckMd5=7af13897284e133a; sid=6xa8fd46; theme-tip-show=SHOWED; rpdid=|(m~mYllmm)0J'u~YJR)|)|~; LIVE_BUVID=AUTO7617634841892045; theme-avatar-tip-show=SHOWED; theme-switch-show=SHOWED; hit-dyn-v2=1; CURRENT_QUALITY=32; ogv_device_support_hdr=0; ogv_device_support_dolby=0; home_feed_column=5; browser_resolution=1707-791; bili_ticket=eyJhbGciOiJIUzI1NiIsImtpZCI6InMwMyIsInR5cCI6IkpXVCJ9.eyJleHAiOjE3NzUxMjQ2NTEsImlhdCI6MTc3NDg2NTM5MSwicGx0IjotMX0.Tjwa7TA0B0kPE3-rkYq7FDV17fdLU_Ry1jRJYjSICP4; bili_ticket_expires=1775124591; bp_t_offset_20573602=1186452563394822144; CURRENT_FNVAL=4048; PVID=26; b_lsid=5979A6C1_19D4B886E42";
    // 统一请求方法（自动加 Cookie 和常见 Header）
    static async Task<string> RequestAsync(string url)
    {
        // 建议在初始化 client 时只设置一次，而不是每次 Request 都在那 Remove/Add
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Referer", "https://live.bilibili.com/");
        client.DefaultRequestHeaders.Add("Origin", "https://live.bilibili.com");
        client.DefaultRequestHeaders.Add("Cookie", WebCookie);
        //client.DefaultRequestHeaders.Add("Cookie", $"SESSDATA={SessData}");

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
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = System.Text.Json.JsonSerializer.Deserialize<BiliFavResourceResponse>(resp, options);
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

        return videoList;
    }

    [HttpGet("favlist")]
    public async Task<List<string>> GetFavList()
    {
        // 一般就用一个，写死即可
        //默认：https://space.bilibili.com/3546649320229192/favlist?fid=3108098292&ftype=create
        //下载：https://space.bilibili.com/3546649320229192/favlist?fid=3573957792&ftype=create

        // 专门用来下载的收藏夹（3573957792）中视频列表
        return await GetVideosInFolderAsync(3573957792);
    }

    // 打印容器当前运行的下载任务
    [HttpGet("running_tasks")]
    public ActionResult<List<FFmpegTaskDto>> GetRunningTasks()
    {
        var running = ffManager.GetRunningTasks();
        return Ok(running);
    }

    // 提交JSON {"user":"admin"}
    [HttpPost("stop_tasks")]
    public async Task<ActionResult<List<FFmpegTaskDto>>> StopRunningTasks([FromBody] StopRequest stopUser)
    {
        var running = ffManager.GetRunningTasks();
        Console.WriteLine($"{stopUser.User} 要求停止, 当前任务{running.Count}个");

        await ffManager.StopAll();
        return Ok(running); //[] 👉 0个任务，成功
    }
}