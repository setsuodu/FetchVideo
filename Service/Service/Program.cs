using FetchVideo;
using FetchVideo.Controllers;
using FetchVideo.Data;
using FetchVideo.Services;
using Microsoft.EntityFrameworkCore;

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
builder.Services.AddSingleton<FFmpegProcessManager>();

// 在 builder.Services 区域加入这行
builder.Services.AddSingleton<DailyTriggerService>();  // 单例！关键！
builder.Services.AddHostedService(provider => provider.GetRequiredService<DailyTriggerService>());

// 注册服务
builder.Services.AddScoped<BilibiliController>();
builder.Services.AddScoped<RouteController>();
builder.Services.AddScoped<ScheduleConfigService>();

// SQLite 配置（数据库文件会生成在容器里的 /app/data 目录）
builder.Services.AddDbContext<AppDbContext>(options =>
    //options.UseSqlite("Data Source=/app/data/app.db"));
    options.UseSqlite("Data Source=app.db"));
// 自动创建目录和数据库（开发/生产都好用）
builder.Services.BuildServiceProvider().GetService<AppDbContext>()?.Database.Migrate();

var app = builder.Build();

// 自动迁移（容器启动时自动建库建表）
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();   // 没有表就自动创建
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
//var svc = app.Services.GetRequiredService<ScheduleConfigService>();
app.MapPost("/api/schedule/update", async (List<string> times, DailyTriggerService service) =>
{
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
        current = service.GetCurrentTriggerTimes()
    });
});
// GET：查看当前定时时间
app.MapGet("/api/schedule/current", async (DailyTriggerService service) =>
{
    using (var scope = app.Services.CreateScope())
    {
        var svc = scope.ServiceProvider.GetRequiredService<ScheduleConfigService>();
        // 读取
        var timesNew = await svc.GetAsync();   // 直接得到 List<string>
        Console.WriteLine($"SQLite读取: length=[{timesNew.Count}]:`{string.Join(", ", timesNew)}");
    }

    var times = service.GetCurrentTriggerTimes();
    return Results.Ok(new
    {
        currentTimes = times,
        count = times.Count,
        nextPossibleTriggers = times.Select(t =>
        {
            if (TimeSpan.TryParseExact(t, new[] { "H:m:s", "HH:mm:ss", "H:m", "HH:mm", "HH:mm:ss.fff", "H:m:s.fff" },
                System.Globalization.CultureInfo.InvariantCulture, out var ts))
            {
                var next = DateTime.Today.Add(ts);
                if (next <= DateTime.Now) next = next.AddDays(1);
                return new { time = t, nextRun = next.ToString("yyyy-MM-dd HH:mm:ss") };
            }
            return null;
        }).Where(x => x != null)
    });
});
// 可选：默认跳转到 WebView
app.MapGet("/", () => Results.Redirect("/index.html")); //重定向

// ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
// 关键：把 app 传给 GlobalConfig
GlobalConfig.Initialize(app);
// ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←

app.Run();