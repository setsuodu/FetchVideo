using Microsoft.Playwright;

namespace FetchVideo.Playwright;

public class HeadlessBrowser
{
    // 使用 Playwright 模仿浏览器行为获取 html
    // ❌Playwright放服务器太重了，直接 +200MB，编译5分钟❌
    // ❌B站对Linux反爬更严格，建议功能移到客户端❌
    public async Task<IPage> GetHTML(string url)
    {
        // 自动安装浏览器（第一次运行会下载 Chromium，后面就不会了）
        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        // 无头模式（不显示浏览器窗口）
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            //Headless = true,  // 改成 false 可以看到浏览器窗口，便于调试
            Headless = false,  // 先改成 false！有窗口才能通过大部分检测
            Args = new[]
            {
                "--no-sandbox",
                "--disable-setuid-sandbox",
                "--disable-infobars",
                "--window-position=0,0",
                "--disable-extensions",
                "--disable-blink-features=AutomationControlled"
            }
        });
        var context = await browser.NewContextAsync(new()
        {
            ViewportSize = new() { Width = 1920, Height = 1080 },
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/130.0.0.0 Safari/537.36"
        });

        // 关键反检测代码（这一段必须加！）
        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => false });
            window.chrome = { runtime: {}, app: {}, loadTimes: () => {} };
            Object.defineProperty(navigator, 'languages', { get: () => ['zh-CN', 'zh'] });
            Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3, 4, 5] });
        ");
        var page = await browser.NewPageAsync();

        await page.GotoAsync(url);

        // 等待页面主要内容加载完成（推荐用 NetworkIdle，比固定延时更可靠）
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // 可选：再等一下确保动态内容渲染完（B 站有时稍慢）
        await Task.Delay(2000);

        // 获取完整的渲染后 HTML
        string html = await page.ContentAsync();

        // 输出到控制台（实际项目可以保存到文件）
        Console.WriteLine("=== 页面 HTML 长度 ===");
        Console.WriteLine(html.Length);
        Console.WriteLine("\n=== 前 1000 个字符预览 ===");
        Console.WriteLine(html.Substring(0, Math.Min(1000, html.Length)));

        // 保存到文件（可选）
        await System.IO.File.WriteAllTextAsync("C:\\Users\\33913\\Desktop\\up主页面.html", html);
        Console.WriteLine("完整 HTML 已保存到 up主页面.html");

        // 按任意键退出
        //Console.WriteLine("按任意键退出...");
        //Console.ReadKey();

        return page;
    }
}
