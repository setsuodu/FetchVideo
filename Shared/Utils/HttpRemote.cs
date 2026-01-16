namespace FetchVideo.Utils;

public static class HttpRemote
{
    // .NET 9 顶级性能处理器
    private static readonly SocketsHttpHandler _handler = new SocketsHttpHandler
    {
        PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        KeepAlivePingDelay = TimeSpan.FromSeconds(60),
        MaxConnectionsPerServer = 50,
        AutomaticDecompression = System.Net.DecompressionMethods.All
    };

    public static readonly HttpClient Client = new HttpClient(_handler)
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    static HttpRemote()
    {
        Client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
    }

    /// <summary>
    /// 下载图片到指定路径（自动创建目录，支持常见图片格式）
    /// </summary>
    /// <returns>返回实际保存的文件路径</returns>
    public static async Task<string> DownloadImageAsync(
        string imageUrl,
        string saveDirectory,
        string? customFileName = null,
        CancellationToken ct = default)
    {
        // 确保目录存在
        Directory.CreateDirectory(saveDirectory);

        // 获取响应
        using var response = await Client.GetAsync(imageUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        // 尝试从 Content-Type 或 URL 推测扩展名
        string extension = ".jpg"; // 默认
        var contentType = response.Content.Headers.ContentType?.MediaType;

        if (contentType != null)
        {
            extension = contentType switch
            {
                "image/jpeg" => ".jpg",
                "image/png" => ".png",
                "image/webp" => ".webp",
                "image/gif" => ".gif",
                _ => ".jpg"
            };
        }
        else if (Path.HasExtension(imageUrl))
        {
            extension = Path.GetExtension(imageUrl);
        }

        // 文件名处理
        string fileName = customFileName
            ?? $"{Guid.NewGuid():N}{extension}"
            ?? $"{Path.GetFileNameWithoutExtension(imageUrl)}{extension}";

        string fullPath = Path.Combine(saveDirectory, fileName);

        // 下载
        using var contentStream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);

        await contentStream.CopyToAsync(fileStream, 81920, ct);

        return fullPath;
    }
}