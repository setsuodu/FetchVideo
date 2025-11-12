var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(); // 添加服务必须在 app build 之前

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
// fnOS         | /data/download
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
app.MapGet("/", () => Results.Redirect("/index.html"));

app.Run();