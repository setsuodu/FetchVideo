using Microsoft.Extensions.Options;
using FetchVideo.Models;

namespace FetchVideo.Services;

public class DailyTriggerService : BackgroundService
{
    private readonly ILogger<DailyTriggerService> _logger;
    private ClockConfig _config;

    public DailyTriggerService(
        ILogger<DailyTriggerService> logger,
        IOptionsMonitor<ClockConfig> optionsMonitor)
    {
        _logger = logger;
        _config = optionsMonitor.CurrentValue;

        // 热更新监听
        optionsMonitor.OnChange(newConfig =>
        {
            _logger.LogWarning("clock.json 已热更新！！新时间：{0}", string.Join(", ", newConfig.TriggerTimes));
            _config = newConfig;
        });

        // 启动时也打印一次
        _logger.LogWarning("启动时读取的时间：{0}", string.Join(", ", _config.TriggerTimes));
        _logger.LogWarning("实时修改这个文件 → {0}", Path.Combine(AppContext.BaseDirectory, "clock.json"));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogWarning("定时服务启动成功！实时修改 → {0}",
            Path.Combine(AppContext.BaseDirectory, "clock.json"));

        // 启动时也触发一次热更新日志（让你知道活着）
        _logger.LogInformation("当前触发时间：{0}", string.Join(", ", _config.TriggerTimes));

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var today = now.Date;
            DateTime? nearest = null;

            // 每次循环都重新读取最新配置 → 热更新实时生效！
            foreach (var t in _config.TriggerTimes ?? Enumerable.Empty<string>())
            {
                if (TimeSpan.TryParse(t, out var time))
                {
                    var candidate = today.Add(time);
                    if (candidate <= now)
                        candidate = candidate.AddDays(1);

                    if (nearest == null || candidate < nearest)
                        nearest = candidate;
                }
            }

            if (nearest.HasValue)
            {
                var delay = nearest.Value - DateTime.Now;
                _logger.LogInformation("下一次触发 → {0}（等待 {1:c}）",
                    nearest.Value.ToString("yyyy-MM-dd HH:mm:ss"), delay);

                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay, stoppingToken);

                if (stoppingToken.IsCancellationRequested) break;

                _logger.LogWarning("时间到！执行定时任务 → {0}", DateTime.Now.ToString("HH:mm:ss"));
                await TriggerApiAsync();
            }
            else
            {
                await Task.Delay(5000, stoppingToken); // 配置全错，缓一缓
            }
        }
    }

    private async Task TriggerApiAsync()
    {
        Console.WriteLine("【Action】触发了事件！！！");
        await Task.CompletedTask;
    }
}