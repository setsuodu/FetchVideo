using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;

public class TktubeMp4Extractor : IAsyncDisposable
{
    private readonly IPlaywright _playwright;
    private readonly IBrowser _browser;
    private readonly IBrowserContext _context;

    public TktubeMp4Extractor()
    {
        _playwright = Playwright.CreateAsync().GetAwaiter().GetResult();
        _browser = _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = new[] { "--no-sandbox", "--disable-setuid-sandbox", "--mute-audio" }
        }).GetAwaiter().GetResult();

        _context = _browser.NewContextAsync(new()
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0 Safari/537.36",
            ViewportSize = new() { Width = 1920, Height = 1080 },
            BypassCSP = true
        }).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 专治 tktube 新流程：先点“Open AD & Play” → 等5秒广告 → 点“Skip AD” → 抓真实MP4
    /// </summary>
    public async Task<string?> GetRealMp4UrlAsync(string pageUrl)
    {
        var page = await _context.NewPageAsync();

        // 注入监听器（专抓 <video> 的 src 变化）
        await page.EvaluateAsync("""
        () => {
            window.FINAL_MP4 = null;
            new MutationObserver((mutations, obs) => {
                for (const m of mutations) {
                    if (m.attributeName === 'src' && m.target.tagName === 'VIDEO') {
                        const src = m.target.src;
                        if (src?.includes('cloudflarestorage.com') && src?.includes('.mp4')) {
                            window.FINAL_MP4 = src;
                            obs.disconnect();
                        }
                    }
                }
            }).observe(document, { attributes: true, subtree: true, attributeFilter: ['src'] });
        }
        """);

        await page.GotoAsync(pageUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });

        // 暴力点击：两种页面都通杀
        await page.EvaluateAsync("""
            () => {
                // 方案1：绿色大按钮
                const green = document.querySelector('button.kt-api-btn-start, button[onclick="live_link()"], button:has-text("Open AD")');
                if (green && green.offsetParent !== null) {
                    green.click();
                    return;
                }
                // 方案2：点播放器中间
                const player = document.getElementById('kt_player') || document.querySelector('.fp-player');
                if (player) {
                    const rect = player.getBoundingClientRect();
                    const x = rect.left + rect.width / 2;
                    const y = rect.top + rect.height / 2;
                    player.dispatchEvent(new MouseEvent('click', {bubbles: true, clientX: x, clientY: y}));
                }
            }
        """);

        // 等 Skip AD 并点掉
        try
        {
            await page.WaitForSelectorAsync(".fp-ui-skip-ad, text=Skip AD", new PageWaitForSelectorOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = 25000
            });
            await page.ClickAsync(".fp-ui-skip-ad, text=Skip AD");
        }
        catch { /* 可能直接就是直链 */ }

        // 等真实地址（最多 35 秒）
        await page.WaitForFunctionAsync("!!window.FINAL_MP4", 35000);

        return await page.EvaluateAsync<string>("() => window.FINAL_MP4");
    }

    public async ValueTask DisposeAsync()
    {
        await _context.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }
}