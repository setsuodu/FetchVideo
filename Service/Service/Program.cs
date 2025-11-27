using FetchVideo.Controllers;
using FetchVideo.Services;

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

builder.Services.AddScoped<BilibiliController>();
builder.Services.AddScoped<RouteController>();

var app = builder.Build();

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

// 确保下载目录存在，容器内 download 映射👉 宿主机
// Windows      | C:/Users/YourName/Downloads/
// Linux/Docker | ~/download
// Synolog      | /volume1/downloads
// fnOS         | /vol1/download
//var downloadPath = Path.Combine(AppContext.BaseDirectory, "download"); // ~\bin\Debug\net9.0\download
//Directory.CreateDirectory(downloadPath);
//Console.WriteLine($"创建文件夹: {downloadPath}");

// 让 /downloads 能直接访问宿主机映射目录（用于下载 404 日志）
app.MapGet("/downloads/{*path}", async (string path, HttpContext ctx) =>
{
    var filePath = Path.Combine("/app/downloads", path);
    if (!System.IO.File.Exists(filePath)) return Results.NotFound();
    return Results.File(filePath, "application/octet-stream");
});
// 在 app.MapControllers(); 之前加一个 API
app.MapPost("/api/schedule/update", (List<string> times, DailyTriggerService service) =>
{
    service.UpdateTriggerTimes(times);
    return Results.Ok(new
    {
        message = "定时时间已更新",
        current = service.GetCurrentTriggerTimes()
    });
});
// GET：查看当前定时时间
app.MapGet("/api/schedule/current", (DailyTriggerService service) =>
{
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

app.Run();