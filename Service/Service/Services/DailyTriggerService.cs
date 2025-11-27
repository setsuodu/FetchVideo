using FetchVideo.Controllers;
using FetchVideo.Utils;

namespace FetchVideo.Services;

public class DailyTriggerService : BackgroundService
{
    private readonly ILogger<DailyTriggerService> _logger;
    // 🌟 核心修改 1: 注入 IServiceScopeFactory
    private readonly IServiceScopeFactory _scopeFactory;
    private volatile List<string> _triggerTimes = new() { "08:00", "12:00", "18:00", "22:00" };
    private CancellationTokenSource _cts = new();   // 每次都要重新 new

    public DailyTriggerService(ILogger<DailyTriggerService> logger,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory; // 保存 Factory
    }

    public IReadOnlyList<string> GetCurrentTriggerTimes() => _triggerTimes.AsReadOnly();

    public void UpdateTriggerTimes(List<string> newTimes)
    {
        if (newTimes == null || !newTimes.Any()) return;

        lock (this)
        {
            var validTimes = new List<string>();
            foreach (var t in newTimes)
            {
                if (TimeSpan.TryParse(t.Trim(), out _))   // ← 改成 TryParse，万能！
                {
                    validTimes.Add(t.Trim());
                }
                else
                {
                    //_logger.LogWarning("忽略无效时间格式：{0}", t);
                }
            }

            _triggerTimes = validTimes;
            _logger.LogWarning("定时时间已更新 → {0}", string.Join(", ", _triggerTimes));

            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }

    private (DateTime nextRun, TimeSpan delay) CalculateNextRun()
    {
        var now = DateTime.Now;
        var today = now.Date;
        var times = _triggerTimes.ToList();

        DateTime? nearest = null;
        TimeSpan? shortest = null;

        foreach (var t in times)
        {
            if (TimeSpan.TryParse(t.Trim(), out var ts))   // ← 同样改成 TryParse
            {
                var candidate = today.Add(ts);
                if (candidate <= now) candidate = candidate.AddDays(1);
                var d = candidate - now;

                if (shortest == null || d < shortest.Value)
                {
                    shortest = d;
                    nearest = candidate;
                }
            }
        }

        return (nearest ?? now.AddMinutes(1), shortest ?? TimeSpan.FromMinutes(1));
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 宿主关闭时，循环会停止
        while (!stoppingToken.IsCancellationRequested)
        {
            var (nextRun, delay) = CalculateNextRun();

            if (delay > TimeSpan.Zero)
            {
                _logger.LogInformation("下次触发：{0}（等待 {1:h\\:mm\\:ss}）",
                    nextRun.ToString("yyyy-MM-dd HH:mm:ss"), delay);

                // 🌟 核心修复：关联两个 CancellationToken
                // 1. _cts.Token：用于时间更新时取消等待
                // 2. stoppingToken：用于宿主关闭时取消等待（解决卡住问题）
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, stoppingToken);

                try
                {
                    // 使用关联后的 linkedCts.Token
                    await Task.Delay(delay, linkedCts.Token);
                }
                catch (TaskCanceledException)
                {
                    // 检查是哪种取消
                    if (stoppingToken.IsCancellationRequested)
                    {
                        // 宿主关闭导致的取消，正常退出
                        _logger.LogInformation("宿主正在关闭，DailyTriggerService 退出。");
                        return; // 退出 ExecuteAsync，完成服务关闭
                    }

                    // 否则是 UpdateTriggerTimes 调用 _cts.Cancel() 导致的取消
                    _logger.LogInformation("定时时间被修改，重新计算下次运行时间……");
                    continue;   // 直接进入下一轮 while 循环，重新计算和等待
                }

                //_logger.LogWarning("【Action】定时任务触发了！！！");
                //Console.WriteLine("【Action】触发了事件！！！");
                await TriggerApiAsync();
            }
            else
            {
                // 如果计算不出时间（或 delay <= 0），也应该监听 stoppingToken
                await Task.Delay(1000, stoppingToken);
            }
        }
        _logger.LogInformation("DailyTriggerService 退出循环。");
    }

    private async Task TriggerApiAsync()
    {
        //TODO: 这里写死了，改成配置
        int length = 2;
        List<string> urlList = new List<string>
        {
            "https://live.bilibili.com/1239314", //setsuodu（未开播）
            "https://live.bilibili.com/31734361", //小利萝（未开播）
            "https://live.bilibili.com/936822", //瑟宝瑟宝（轮播？）
            "https://live.bilibili.com/1728867738", //至尊强者小知恩
            "https://live.bilibili.com/1792597682", //羊莓杨莓
            "https://live.bilibili.com/1868871042", //开心螺蛳粉宝宝
            "https://live.bilibili.com/1948312359", //aeri酱咩
            "https://live.bilibili.com/1712256876", //yy的小歪
            "https://live.bilibili.com/1772923624", //你别芭乐我_
        };


        for (int i = 0; i < urlList.Count; i++)
        {
            string url = urlList[i];

            // 🌟 核心修改 3: 在作用域内执行服务操作
            using (var scope = _scopeFactory.CreateScope())
            {
                // 先检查是否开播
                var bili = scope.ServiceProvider.GetRequiredService<BilibiliController>();
                string room_id = Shared.GetRoomId(url);
                var room_info = await bili.GetRoomInfo(room_id);
                if (room_info.live_status == 0)
                {
                    _logger.LogInformation($"[{i}] - 未开播 - {url}");
                }
                if (room_info.live_status == 1)
                {
                    // 确定开播开始录制
                    var route = scope.ServiceProvider.GetRequiredService<RouteController>();
                    try
                    {
                        await route.Check(url, length);
                        _logger.LogInformation("计划录制...");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "调用 route.CheckAsync 失败。");
                    }
                }
                else
                {
                    _logger.LogInformation($"[{i}] - 在轮播 - {url}");
                }
            }
        }
    }
}