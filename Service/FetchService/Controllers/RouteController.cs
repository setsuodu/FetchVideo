using Microsoft.AspNetCore.Mvc;

namespace FetchService.Controllers;

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
    public async Task<IActionResult> Check([FromQuery] string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return BadRequest(new
            {
                error = "URL 参数不能为空",
                message = "请提供有效的视频 URL"
            });
        }

        Console.WriteLine($"检查是什么平台的视频: {url}");

        // ①B站视频
        // ②B站直播
        // ③YouTube视频
        // missav访问时用油猴自动提交SQL，没有则记入本地文件
        if (url.Contains("bilibili.com/video/BV"))
        {
            // https://www.bilibili.com/video/BV1ysySBsExt/
            // 获取视频标题
            string bvId = Shared.GetBvId(url);
            Console.WriteLine($"是 Bilibili视频: bvId={bvId}");

            //await bili.GetUpInfo(bvId); // 获取Up信息，不需要
            await bili.GetBilibiliVideoAsync(bvId); // 获取视频
        }
        else if (url.Contains("live.bilibili"))
        {
            // https://live.bilibili.com/1792597682
            // Up主信息 + 当前时间戳
            string roomId = Shared.GetRoomId(url);
            Console.WriteLine($"是 Bilibili直播: 房间: {roomId}");
            string title = await bili.GetTitleAsync(url);
            Console.WriteLine($"直播标题: {title}");
            await bili.GetM3U8(roomId, title);
        }
        else if (url.Contains("youtu"))
        {
            //string fullUrl = "https://www.youtube.com/watch?v=ij89E9qABho"; // 标准地址
            //string sUrl = "https://youtu.be/ij89E9qABho"; // 短地址
            //string shortUrl = "https://www.youtube.com/shorts/fOlW2f38PFE"; // Short地址（含标题日文）
            //string longUrl = "https://www.youtube.com/watch?v=CvDpSRuGsjY"; // 标准地址2（测试）
            // 获取视频标题
            Console.WriteLine($"是 Youtube视频: ");

            await tube.GetYoutubeVideoAsync(url);
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
            file = Path.GetFileName("result.FilePath"),
            filePath = "result.FilePath",
            size = "result.FileSize",
            status = "success",
            downloadUrl = "result.DownloadUrl",   // 可选：提供前端直接下载
            logPath = "result.LogPath",           // 可选：下载日志
            fileName = Path.GetFileName("result.FilePath")
        };
        return Ok(response);
    }

    // 停止 API：接收任务 ID
    //[HttpPost("stop/{taskId}")]
    [HttpGet("stop")]
    public async Task<IActionResult> Stop(string taskId)
    {
        var success = await _manager.StopFFmpeg(taskId);
        if (success)
        {
            return Ok("FFmpeg 进程已停止");
        }
        return NotFound("进程不存在或已停止");
    }
}
