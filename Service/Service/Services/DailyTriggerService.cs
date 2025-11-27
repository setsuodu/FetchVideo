namespace FetchVideo.Services;

public class DailyTriggerService : BackgroundService
{
    private readonly ILogger<DailyTriggerService> _logger;
    private volatile List<string> _triggerTimes = new() { "00:00", "12:00", "16:24" };
    private CancellationTokenSource _cts = new();   // 每次都要重新 new

    public DailyTriggerService(ILogger<DailyTriggerService> logger)
    {
        _logger = logger;
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
                    _logger.LogWarning("忽略无效时间格式：{0}", t);
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
        while (!stoppingToken.IsCancellationRequested)
        {
            var (nextRun, delay) = CalculateNextRun();

            if (delay > TimeSpan.Zero)
            {
                _logger.LogInformation("下次触发：{0}（等待 {1:h\\:mm\\:ss}）",
                    nextRun.ToString("yyyy-MM-dd HH:mm:ss"), delay);

                try
                {
                    await Task.Delay(delay, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    _logger.LogInformation("时间被修改，重新计算……");
                    continue;   // 直接进入下一轮循环
                }

                _logger.LogWarning("【Action】定时任务触发了！！！");
                Console.WriteLine("【Action】触发了事件！！！");
            }
            else
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}