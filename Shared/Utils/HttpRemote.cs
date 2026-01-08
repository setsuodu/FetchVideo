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
}