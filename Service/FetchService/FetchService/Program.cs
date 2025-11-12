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

app.Run();