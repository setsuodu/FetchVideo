using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FetchVideo.Services;

// 继承 BackgroundService，这是实现 IHostedService 的推荐方式
public class ApiPollingService : BackgroundService
{
    private readonly ILogger<ApiPollingService> _logger;
    private readonly HttpClient _httpClient;

    // 设定轮询间隔为 30 秒
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(30);

    // 通过依赖注入获取 Logger 和 HttpClient
    public ApiPollingService(
        ILogger<ApiPollingService> logger,
        HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    // 核心逻辑：执行后台任务
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("API 轮询服务已启动。");

        // 应用程序启动后延迟 5 秒开始第一次轮询
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        // 只要应用程序没有被停止，就一直循环
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("正在轮询 API, 当前时间: {Time}", DateTimeOffset.Now);

            try
            {
                // ** 轮询 API 的逻辑 **
                var apiUrl = "https://your-external-api.com/status";

                // 使用 stoppingToken 确保在程序关闭时取消正在进行的请求
                var response = await _httpClient.GetAsync(apiUrl, stoppingToken);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync(stoppingToken);
                    _logger.LogInformation("API 轮询成功。返回数据长度: {Length} 字节", content.Length);
                    // TODO: 在这里处理获取到的数据
                }
                else
                {
                    _logger.LogWarning("API 返回非成功状态码: {StatusCode}", response.StatusCode);
                }
            }
            catch (TaskCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // 这是应用程序关闭时预期发生的异常，我们在此处忽略
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API 轮询过程中发生错误。");
            }

            // 等待直到下一次轮询间隔
            await Task.Delay(_pollingInterval, stoppingToken);
        }

        _logger.LogInformation("API 轮询服务已停止。");
    }
}