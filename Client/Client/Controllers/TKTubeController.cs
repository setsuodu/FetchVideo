using FetchVideo.Playwright;
using HtmlAgilityPack;
using System.Net;

namespace FetchVideo.Controllers;

public class TKTubeController
{
    public async Task Fetch(string url)
    {
        // 使用 Playwright 自动👉点击按钮👉看广告👉点跳过👉获取mp4地址

        /* HttpClient👉直接 403 (Forbidden) */

        // <div class="headline"> // 标题👉文件名
        HeadlessBrowser browser = new HeadlessBrowser();
        string html = await browser.GetHTML(url);
        //Console.WriteLine(html);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        string title = doc.DocumentNode.SelectSingleNode("//div[@class='headline']")?.InnerText.Trim() ?? "未找到";
        Console.WriteLine($"另存为: {title}.mp4");

        // 下面的东西在 F12 / Network 里
    }
}
