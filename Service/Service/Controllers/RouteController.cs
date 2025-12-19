using FetchVideo.Models;
using FetchVideo.Services;
using FetchVideo.Utils;
using Microsoft.AspNetCore.Mvc;

namespace FetchVideo.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RouteController : ControllerBase //路由器
{
    private readonly BilibiliController bili;
    private readonly YoutubeController tube;
    private readonly FFmpegManager _manager;

    // 从构造函数注入配置，变成本地只读（推荐写法！）
    public RouteController(ISharedService service, FFmpegManager manager)
    {
        // 如果配置中没找到，就用 "/app/downloads";
        bili = new BilibiliController(service, manager);
        tube = new YoutubeController(service, manager);
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
        FFmpegTaskDto dto = null;
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
            string bvId = Shared.GetBvId(url);
            Console.WriteLine($"是 Bilibili视频: bvId={bvId}");
            dto = await bili.GetBVAsync(bvId);
        }
        else if (url.Contains("live.bilibili"))
        {
            int minute = length ?? 10; //默认十分钟
            dto = await bili.LiveRecord(url, minute);
        }
        else if (url.Contains("youtu"))
        {
            Console.WriteLine($"是 Youtube视频: ");
            dto = await tube.GetYoutubeVideoAsync(url);
        }
        else if (url.Contains(".m3u8"))
        {
            Console.WriteLine($"是 m3u8视频: ");
            dto = await tube.TestM3U8(url);
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

        return Ok(new { 
            output = FFmpegManager.ExtractOutput(dto.Command),
            duration = dto.Duration,
        });
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