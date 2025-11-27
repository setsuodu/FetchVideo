using FetchVideo.Controllers;
using FetchVideo.Models;
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

// 直接把 clock.json 内容绑定到 ClockConfig，强制读运行目录下的文件
builder.Services.Configure<ClockConfig>(config =>
{
    var path = Path.Combine(AppContext.BaseDirectory, "clock.json");
    if (File.Exists(path))
    {
        var json = File.ReadAllText(path);
        var temp = System.Text.Json.JsonSerializer.Deserialize<ClockConfig>(json);
        config.TriggerTimes = temp?.TriggerTimes ?? new() { "00:00", "12:00" };
        Console.WriteLine($"【成功加载】clock.json 已读取，时间点：{string.Join(", ", config.TriggerTimes)}");
    }
    else
    {
        config.TriggerTimes = new() { "00:00", "12:00" };
        Console.WriteLine("【警告】未找到 clock.json，使用默认时间 00:00, 12:00");
    }
});
// 加上热更新监听（这才是王道）
builder.Services.AddHostedService<DailyTriggerService>();

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
// 可选：默认跳转到 WebView
app.MapGet("/", () => Results.Redirect("/index.html")); //重定向

app.Run();