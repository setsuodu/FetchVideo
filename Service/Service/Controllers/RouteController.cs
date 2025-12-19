using Microsoft.AspNetCore.Mvc;
using FetchVideo.Utils;
using FetchVideo.Models;

namespace FetchVideo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RouteController : ControllerBase //路由器
{
    private readonly BilibiliController bili;
    private readonly YoutubeController tube;
    private readonly FFmpegProcessManager _manager;

    // 从构造函数注入配置，变成本地只读（推荐写法！）
    public RouteController(IConfiguration configuration, FFmpegProcessManager manager)
    {
        // 如果配置中没找到，就用 "/app/downloads";
        bili = new BilibiliController(configuration, manager);
        tube = new YoutubeController(configuration, manager);
        _manager = manager;
    }

    [HttpGet("check")]
    public async Task<IActionResult> Check([FromQuery] string url, [FromQuery] int? length)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(new
            {
                error = "URL 参数不能为空",
                message = "请提供有效的视频 URL"
            });
        }
        FFmpegProcessInfo result = null;
        Console.WriteLine($"检查是什么平台的视频: {url}");

        // 短链👉长链
        if (url.Contains("b23.tv"))
        {
            url = await Shared.Curl_I(url);
            Console.WriteLine($"转长链👉{url}");
        }

        // ①B站视频
        // ②B站直播
        // ③YouTube视频
        if (url.Contains("bilibili.com/video/BV"))
        {
            string bvId = Shared.GetBvId(url); // 获取视频标题
            Console.WriteLine($"是 Bilibili视频: bvId={bvId}");
            result = await bili.GetBilibiliVideoAsync(bvId); // 获取视频
        }
        else if (url.Contains("live.bilibili"))
        {
            int minute = length ?? 10; //默认十分钟
            result = await bili.BiliLiveRecord(url, minute);
        }
        else if (url.Contains("youtu"))
        {
            Console.WriteLine($"是 Youtube视频: ");
            result = await tube.GetYoutubeVideoAsync(url);
        }
        else if (url.Contains(".m3u8"))
        {
            Console.WriteLine($"是 m3u8视频: ");
            result = await tube.GetM3U8(url);
        }
        else
        {
            //Console.WriteLine($"不支持的网站: {url}");
            // 统一错误返回
            return StatusCode(500, new
            {
                error = "不支持的网站",
                message = "ex.Message",
                details = "ex.InnerException?.Message"
            });
        }

        var response = new
        {
            file = result.TaskId,
            filePath = "result.FilePath",
            size = "result.FileSize",
            status = "success",
            downloadUrl = result.Command,   // 可选：提供前端直接下载
            logPath = "result.LogPath",           // 可选：下载日志
            fileName = Path.GetFileName("result.FilePath"),
        };
        return Ok(response);
    }

    // 停止 API：接收任务 ID
    [HttpGet("stop")]
    public async Task<IActionResult> Stop(string taskId)
    {
        var success = await _manager.StopFFmpeg(taskId);
        if (success)
        {
            return Ok(new { message = "FFmpeg 已停止", taskId });
        }
        return NotFound(new { message = "任务不存在或已结束", taskId });
    }

    // 可选：获取运行中的任务
    [HttpGet("running")]
    public IActionResult GetRunning()
    {
        var tasks = _manager.GetRunningTasks();
        return Ok(tasks);
    }
}