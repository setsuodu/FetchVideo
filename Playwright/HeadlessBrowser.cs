using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace FetchVideo.Playwright;

public class HeadlessBrowser
{
    // 使用 Playwright 模仿浏览器行为获取 html
    // ❌Playwright放服务器太重了，直接 +200MB，编译5分钟❌
    // ❌B站对Linux反爬更严格，建议功能移到客户端❌
    public async Task<string> GetHTML(string url)
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
        Console.WriteLine($"前往: {url}");

        // 等待页面主要内容加载完成（推荐用 NetworkIdle，比固定延时更可靠）
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        Console.WriteLine($"已经加载完成");

        // 可选：再等一下确保动态内容渲染完
        await Task.Delay(1000);
        Console.WriteLine($"再等一下确保动态内容渲染");

        // 获取完整的渲染后 HTML
        string html = await page.ContentAsync();
        Console.WriteLine($"此时获取完整HTML");

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


        // 通过文字点击按钮 Open AD & Play
        var btn = page.Locator("button.kt-api-btn-start");

        // 等它出现并可点击（最多等 30 秒）
        await btn.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
        Console.WriteLine("第一次等待");
        await btn.WaitForAsync(new() { State = WaitForSelectorState.Attached });
        Console.WriteLine("第二次等待");

        await btn.ClickAsync(new()
        {
            Force = true,        // 强制点击，忽略遮罩、disabled 等
            Timeout = 10000
        });

        Console.WriteLine("成功点击 Open AD & Play 按钮");

        return html;
    }

    public async Task<string> TestButton(string url)
    {
        Console.WriteLine($"Test: {url}");

        using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new()
        {
            Headless = false,                      // 必须有头！广告页最怕无头
            Args = new[] { "--start-maximized", "--disable-blink-features=AutomationControlled" }
        });

        var context = await browser.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
            ViewportSize = null,
            BypassCSP = true,
            Locale = "zh-CN"
        });

        // 关键反检测脚本（绕过 99% 的广告页检测）
        await context.AddInitScriptAsync(@"
            Object.defineProperty(navigator, 'webdriver', { get: () => false });
            Object.defineProperty(navigator, 'languages', { get: () => ['zh-CN', 'zh'] });
            Object.defineProperty(navigator, 'plugins', { get: () => [1,2,3,4,5] });
            window.chrome = { runtime: {}, app: {}, webstore: {} };
            delete navigator.__proto__.webdriver;
        ");

        var page = await context.NewPageAsync();

        // ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓ 把这里改成你要打开的真实网址 ↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓↓
        await page.GotoAsync("https://xxxxxx.com/xxxxxx", new() { WaitUntil = WaitUntilState.NetworkIdle });
        // ↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑↑

        Console.WriteLine("页面加载完成，开始找 Open AD & Play 按钮…");

        // 强迫滚到底部触发广告加载
        await page.EvaluateAsync("window.scrollTo(0, document.body.scrollHeight)");
        await page.WaitForTimeoutAsync(5000);

        // 终极多重定位 + 自动点到为止
        var selectors = new[]
        {
            "button.kt-api-btn-start",
            "button[class*='kt-api-btn']",
            "button:has-text('Open AD')",
            "button:has-text('AD & Play')",
            "//button[contains(., 'Open AD') or contains(., 'AD & Play')]"
        };

        bool clicked = false;
        foreach (var sel in selectors)
        {
            try
            {
                var locator = sel.StartsWith("//")
                    ? page.Locator($"xpath={sel}")
                    : page.Locator(sel);

                if (await locator.CountAsync() > 0)
                {
                    Console.WriteLine($"成功定位 → {sel}");
                    await locator.First.ClickAsync(new() { Force = true, Timeout = 10000 });
                    Console.WriteLine("已成功点击 Open AD & Play 按钮！");
                    clicked = true;
                    break;
                }
            }
            catch { /* 继续试下一个 */ }
        }

        // 如果主页面还没点到，扫一遍所有 iframe
        if (!clicked)
        {
            Console.WriteLine("主页面没找到，开始扫 iframe…");
            var frames = page.Frames;
            foreach (var frame in frames)
            {
                var btn = frame.GetByText("Open AD & Play", new() { Exact = true })
                               .Or(frame.Locator("button.kt-api-btn-start"));

                if (await btn.CountAsync() > 0)
                {
                    await btn.ClickAsync(new() { Force = true });
                    Console.WriteLine("在 iframe 里成功点击！");
                    clicked = true;
                    break;
                }
            }
        }

        if (!clicked)
        {
            await page.ScreenshotAsync(new() { Path = "找不到按钮时的截图.png", FullPage = true });
            Console.WriteLine("所有方法都失效了，已保存全页截图：找不到按钮时的截图.png");
        }
        else
        {
            Console.WriteLine("任务完成，按钮已点！");
        }

        return "3";
    }
}