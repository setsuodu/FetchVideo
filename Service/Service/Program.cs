using System.Globalization;
using Microsoft.EntityFrameworkCore;
using FetchVideo;
using FetchVideo.Controllers;
using FetchVideo.Data;
using FetchVideo.Models;
using FetchVideo.Services;
using FetchVideo.Utils;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // 关键：禁用 camelCase，保持 C# 命名（PascalCase）
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // 添加服务必须在 app build 之前
builder.Services.AddSingleton<FFmpegManager>();

// 在 builder.Services 区域加入这行
builder.Services.AddSingleton<DailyTriggerService>();  // 单例！关键！
builder.Services.AddHostedService(provider => provider.GetRequiredService<DailyTriggerService>());

// 注册服务(内部调用)
//builder.Services.AddScoped<LinkItemController>();
builder.Services.AddScoped<BilibiliController>();
builder.Services.AddScoped<RouteController>();
builder.Services.AddScoped<ScheduleConfigService>();
builder.Services.AddScoped<ISharedService, SharedService>();

// 动态路径
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
if (!Directory.Exists(dataDir))
{
    Directory.CreateDirectory(dataDir);
}
var dbPath = Path.Combine(dataDir, "app.db");
Console.WriteLine($"SQLite 数据库路径: {dbPath}");
// Docker 👉 SQLite 数据库路径: /app/data/app.db
// VS     👉 SQLite 数据库路径: D:\GitHub\[Workspace]\FetchVideo\Service\Service\data\app.db

// SQLite 配置（数据库文件会生成在容器里的 /app/data 目录）
//builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=app.db")); // vs√docker×
//builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=/app/data/app.db")); // vs×docker√
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
// 自动创建目录和数据库（开发/生产都好用）
builder.Services.BuildServiceProvider().GetService<AppDbContext>()?.Database.Migrate();

var app = builder.Build();

// 自动迁移（容器启动时自动建库建表）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();   // 没有表就自动创建

    // ===== 添加种子数据逻辑 =====
    // 检查表是否为空
    if (!db.LinkItems.Any())
    {
        var defaultLinks = LinkItemSQL.DefaultList();

        db.LinkItems.AddRange(defaultLinks);
        db.SaveChanges();
    }
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// 关键：启用 wwwroot 静态文件
app.UseStaticFiles();// <-- 新增
app.UseRouting();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 让 /downloads 能直接访问宿主机映射目录（用于下载 404 日志）
app.MapGet("/downloads/{*path}", async (string path, HttpContext ctx) =>
{
    var filePath = Path.Combine("/app/downloads", path);
    if (!System.IO.File.Exists(filePath)) return Results.NotFound();
    return Results.File(filePath, "application/octet-stream");
});
// 在 app.MapControllers(); 之前加一个 API
app.MapPost("/api/schedule/update", async (List<string> times_utc8, DailyTriggerService service) =>
{
    // 这里提交的时间，一定是UTC+8
    // 转成服务器的时区。
    Console.WriteLine($"客户端提交: {string.Join(", ", times_utc8)}");
    var times = Shared.ConvertUtc8ConfigToLocal(times_utc8);
    Console.WriteLine($"转服务器时区: {string.Join(", ", times)}");

    using (var scope = app.Services.CreateScope())
    {
        var svc = scope.ServiceProvider.GetRequiredService<ScheduleConfigService>();
        // 保存
        await svc.SaveAsync(times);
        Console.WriteLine($"SQLite保存: {times.Count}个");
    }

    service.UpdateTriggerTimes(times);
    return Results.Ok(new
    {
        message = "定时时间已更新",
        //current = service.GetCurrentTriggerTimes() //👈这要返回 UTC+8 的配置
        current = Shared.ScheduleConfigSort(times_utc8)
    });
});
// GET：查看当前定时时间
app.MapGet("/api/schedule/current", async (DailyTriggerService service) =>
{
    List<string> times = new List<string>();
    using (var scope = app.Services.CreateScope())
    {
        var svc = scope.ServiceProvider.GetRequiredService<ScheduleConfigService>();
        // 读取
        var timesNew = await svc.GetAsync();   // 直接得到 List<string>
        Console.WriteLine($"SQLite读取: length=[{timesNew.Count}]:`{string.Join(", ", timesNew)}");

        times.AddRange(timesNew);
    }

    // ===== 获取服务器时区信息（始终返回标准 IANA ID）=====
    TimeZoneInfo localTimeZone = TimeZoneInfo.Local;

    // 常见 Windows 时区名 → 标准 IANA ID 映射（只列最常用，够用即可）
    string serverTimeZoneId = localTimeZone.Id switch
    {
        "China Standard Time" => "Asia/Shanghai",
        "Pacific Standard Time" => "America/Los_Angeles",
        "Eastern Standard Time" => "America/New_York",
        "Central Standard Time" => "America/Chicago",
        "Mountain Standard Time" => "America/Denver",
        "GMT Standard Time" => "Europe/London",
        "Central Europe Standard Time" => "Europe/Prague",    // 或 Europe/Warsaw 等
        "Romance Standard Time" => "Europe/Paris",
        "Tokyo Standard Time" => "Asia/Tokyo",
        "Korean Standard Time" => "Asia/Seoul",
        "Singapore Standard Time" => "Asia/Singapore",
        "India Standard Time" => "Asia/Kolkata",
        // 如需更多可继续补充
        _ => localTimeZone.Id  // .NET 6+ 在 Linux 上已经是 IANA，直接返回；Windows 上若未匹配则返回原 Id（兜底）
    };

    int serverOffsetMinutes = (int)localTimeZone.GetUtcOffset(DateTime.UtcNow).TotalMinutes;

    // ===== 计算下一个可能的触发时间（基于服务器本地时区）=====
    var nextPossibleTriggers = times.Select(t =>
    {
        if (TimeSpan.TryParseExact(t, new[] { "H:m:s", "HH:mm:ss", "H:m", "HH:mm", "HH:mm:ss.fff", "H:m:s.fff" },
            CultureInfo.InvariantCulture, TimeSpanStyles.None, out var ts))
        {
            var next = DateTime.Today.Add(ts);
            if (next <= DateTime.Now) next = next.AddDays(1);
            return new
            {
                time = t,
                nextRun = next.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
        return null;
    }).Where(x => x != null)!;

    return Results.Ok(new
    {
        currentTimes = times,                    // 服务器本地时区的时间点列表
        count = times.Count,
        nextPossibleTriggers,

        // 下发给客户端的时区信息（ianaId 现在一定是浏览器支持的格式）
        serverTimeZone = new
        {
            ianaId = serverTimeZoneId,           // 标准 IANA ID，前端可直接用于 Intl.DateTimeFormat
            offsetMinutes = serverOffsetMinutes  // 备用（手动计算时使用）
        }
    });
});
// [GET] /api/debug/gc-collect
app.MapGet("/api/debug/gc-collect", () =>
{
    // 获取当前正在运行的任务数，判断 ConcurrentDictionary 是否真的清空了
    var ffmpegMgr = app.Services.GetRequiredService<FFmpegManager>();
    int taskCount = ffmpegMgr.GetRunningTasks().Count;

    long before = GC.GetTotalMemory(false) / 1024 / 1024;

    // 强制触发 Full GC (Generation 2) 并压缩堆空间
    // 这会把所有没有被引用的对象全部清理掉
    GC.Collect(2, GCCollectionMode.Forced, true, true);
    GC.WaitForPendingFinalizers();

    long after = GC.GetTotalMemory(true) / 1024 / 1024;

    return Results.Ok(new
    {
        Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        RunningFFmpegTasks = taskCount,
        BeforeMB = before,
        AfterMB = after,
        ReleasedMB = before - after,
        Analysis = after > 250 ? "警告：手动回收后内存依然较高，可能存在真泄露" : "提示：内存已回落，属于 .NET GC 正常延迟回收"
    });
});
// 可选：默认跳转到 WebView
app.MapGet("/", () => Results.Redirect("/index.html")); //重定向

// ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
// 关键：把 app 传给 GlobalConfig
GlobalConfig.Initialize(app);
// ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←

app.Run();