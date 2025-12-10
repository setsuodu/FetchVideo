using FetchVideo.Playwright;
using HtmlAgilityPack;
using Microsoft.Playwright;

namespace FetchVideo.Controllers;

public class TKTubeController
{
    public async Task<IPage> Fetch(string url)
    {
        // 使用 Playwright 自动👉点击按钮👉看广告👉点跳过👉获取mp4地址

        /* HttpClient👉直接 403 (Forbidden) */

        // <div class="headline"> // 标题👉文件名
        HeadlessBrowser browser = new HeadlessBrowser();
        var page = await browser.GetHTML(url);
        string html = await page.ContentAsync();
        //Console.WriteLine(html);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        string title = doc.DocumentNode.SelectSingleNode("//div[@class='headline']")?.InnerText.Trim() ?? "未找到";
        Console.WriteLine($"另存为: {title}.mp4");

        // 下面的东西在 F12 / Network 里

        // 找【Play】按钮，模拟点击
        //  👉会跳到 https://zh.tklivechat.com/ 同时开始加载广告 5s
        // <button class="kt-api-btn-start" onclick="live_link()" style="background: #1bff0c;font-weight: bold;font-family: Arial;font-size: 20px;line-height: 20px;display: inline-block; margin: 20px 0px 2px 0px; padding: 0 7px; border-radius: 8px;transition: all .3s;cursor: pointer;text-decoration: none;white-space: nowrap;color: #fff;background-color: #79A500;">Open AD &amp; Play</button>

        // (可选)关闭广告
        // < button class="close-button--a-8tK" type="button">Close ad ×</button>

        // 找【跳过】按钮，模拟点击
        // Skip AD 5→1
        // < div class="fp-ui-skip-ad" style="display: block;">Skip AD</div>


        // 找 mp4 地址
        // https://tktube.46cef6b61ea40ccea4e8afd496db1aea.r2.cloudflarestorage.com/eu11/68000/68598/68598_720p.mp4?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=7aa3f996af4167345fa2f25e16199479%2F20251209%2Fauto%2Fs3%2Faws4_request&X-Amz-Date=20251209T145645Z&X-Amz-SignedHeaders=host&X-Amz-Expires=3600&X-Amz-Signature=2f73527d48ed4c604213b903d3cb3b8bdf7eb7d448027488a91e28b8df11bfc1
        // https://tktube....._720p.mp4?X-Amz-Content-Sha256......

        return page;
    }


    public async Task<IPage> Click(string url)
    {
        return null;
    }
}
