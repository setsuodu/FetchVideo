// GlobalConfig.cs   （放在项目根目录或任意位置）
using FetchVideo.Services;

namespace FetchVideo;

public static class GlobalConfig
{
    private static WebApplication? _app;

    internal static void Initialize(WebApplication app) => _app = app;

    public static List<string> TriggerTimes
    {
        get
        {
            if (_app?.Services == null)
                return ScheduleConfigService.GetHostTimes(); // 启动前用默认值

            using var scope = _app.Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ScheduleConfigService>();
            return svc.GetAsync().GetAwaiter().GetResult();
        }
        set
        {
            if (_app?.Services == null) return;

            using var scope = _app.Services.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<ScheduleConfigService>();
            svc.SaveAsync(value).GetAwaiter().GetResult();
        }
    }
}