using FetchVideo;
using FetchVideo.Controllers;
using FetchVideo.Data;
using FetchVideo.Models;
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

// 注册服务(内部调用)
builder.Services.AddScoped<LinkItemController>();
builder.Services.AddScoped<BilibiliController>();
builder.Services.AddScoped<RouteController>();
builder.Services.AddScoped<ScheduleConfigService>();

// SQLite 配置（数据库文件会生成在容器里的 /app/data 目录）
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite("Data Source=app.db"));
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
        var defaultLinks = new List<LinkItem>
        {
            new LinkItem { Name = "aeri酱咩",       Url = "https://live.bilibili.com/1948312359", Active = true },
            new LinkItem { Name = "软水妮",         Url = "https://live.bilibili.com/1842861593", Active = true },
            new LinkItem { Name = "羊莓杨莓",       Url = "https://live.bilibili.com/1792597682", Active = true },
            new LinkItem { Name = "你别芭乐我_",    Url = "https://live.bilibili.com/1772923624", Active = true },
            new LinkItem { Name = "至尊强者小知恩", Url = "https://live.bilibili.com/1728867738", Active = true },
            new LinkItem { Name = "开心螺蛳粉宝宝", Url = "https://live.bilibili.com/1868871042", Active = true },
            new LinkItem { Name = "枳月味奶片",     Url = "https://live.bilibili.com/1804469695", Active = true },
            new LinkItem { Name = "憨憨打不服",     Url = "https://live.bilibili.com/1779344403", Active = true },
            new LinkItem { Name = "钟意大堡包",     Url = "https://live.bilibili.com/1986467930", Active = true },
            new LinkItem { Name = "香脆小海苔",     Url = "https://live.bilibili.com/1842356567", Active = true },
            new LinkItem { Name = "小福包iu_",      Url = "https://live.bilibili.com/1868870262", Active = true },
            new LinkItem { Name = "小四桃子",       Url = "https://live.bilibili.com/1724212928", Active = true },
            new LinkItem { Name = "星梨梨S",        Url = "https://live.bilibili.com/1747118475", Active = true },
            new LinkItem { Name = "yy的小歪",       Url = "https://live.bilibili.com/1712256876", Active = true },
            new LinkItem { Name = "空空学妹-Sub",   Url = "https://live.bilibili.com/1725923601", Active = true },
            new LinkItem { Name = "小利萝",         Url = "https://live.bilibili.com/31734361", Active = true },
            new LinkItem { Name = "池_渔",          Url = "https://live.bilibili.com/1849823023", Active = true },
            new LinkItem { Name = "来份鱼酱耶",     Url = "https://live.bilibili.com/1814375548", Active = true },
            new LinkItem { Name = "草莓果酱呐",     Url = "https://live.bilibili.com/30950163", Active = true },
            new LinkItem { Name = "Umi今天也发财",  Url = "https://live.bilibili.com/1840178918", Active = true },
            new LinkItem { Name = "濛雨清波",       Url = "https://live.bilibili.com/21156534", Active = true },
            new LinkItem { Name = "婉婉十一月减肥版", Url = "https://live.bilibili.com/1909460797", Active = true },
            new LinkItem { Name = "小鱼要饿死了",   Url = "https://live.bilibili.com/32443468", Active = true },
            new LinkItem { Name = "呀思思",         Url = "https://live.bilibili.com/11368496", Active = true },
            new LinkItem { Name = "冻泥不是冰的",   Url = "https://live.bilibili.com/1898932569", Active = true },
            new LinkItem { Name = "牛角包去睡了",   Url = "https://live.bilibili.com/1904551806", Active = true },
            new LinkItem { Name = "小蜜疯_璀璨",    Url = "https://live.bilibili.com/1729389539", Active = true },
            new LinkItem { Name = "甜奈-好运常伴",  Url = "https://live.bilibili.com/1984186978", Active = true },
            new LinkItem { Name = "路人饼饼ovo",    Url = "https://live.bilibili.com/4533196", Active = true },
            // 可以继续添加更多默认链接
        };

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
//var svc = app.Services.GetRequiredService<ScheduleConfigService>();
app.MapPost("/api/schedule/update", async (List<string> times_utc8, DailyTriggerService service) =>
{
    // 这里提交的时间，客户端上已经转成UTC了
    var times = times_utc8;

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
    List<string> times = new List<string>();
    using (var scope = app.Services.CreateScope())
    {
        var svc = scope.ServiceProvider.GetRequiredService<ScheduleConfigService>();
        // 读取
        var timesNew = await svc.GetAsync();   // 直接得到 List<string>
        Console.WriteLine($"SQLite读取: length=[{timesNew.Count}]:`{string.Join(", ", timesNew)}");

        times.AddRange(timesNew);
    }

    //var times = service.GetCurrentTriggerTimes(); // 去读取写死的配置
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