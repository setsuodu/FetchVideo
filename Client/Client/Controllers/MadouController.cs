using System.Text.RegularExpressions;
using FetchVideo.Utils;
using HtmlAgilityPack;

namespace FetchVideo.Controllers;

public class MadouController
{
    const string BASE_URL = "https://www.madou.io/index.php/vod";

    // 翻页批处理所有视频列表
    // BASE_URL = "https://www.madou.io/index.php/vod";
    // 第一页开始翻页，最大页数未知
    // 列表第一页 https://www.madou.io/index.php/vod/type/id/21.html
    // 第一页 https://www.madou.io/index.php/vod/type/id/21/page/1.html
    // 第七页 https://www.madou.io/index.php/vod/type/id/21/page/7.html
    public async Task ParseAllPages(int typeId)
    {
        Console.WriteLine($"开始翻页批处理[{typeId}]类所有视频列表");

        var all = new List<string>();

        int page = 1;
        while (true)
        {
            var list = await ParsePage(typeId, page);
            Console.WriteLine($"第{page}页，找到 {list.Count} 个视频");
            all.AddRange(list);
            if (list.Count <= 0)
            {
                Console.WriteLine($"没有更多");
                break;
            }
            page++;
        }
    }

    // 分析页面上的视频
    public async Task<List<string>> ParsePage(int typeId, int page)
    {
        var videoList = new List<string>();

        Console.WriteLine($"分析本页上的视频: typeId={typeId}, page={page}");
        string url = $"{BASE_URL}/type/id/{typeId}/page/{page}.html";
        //Console.WriteLine($"处理第 {page} 页: {url}");
        
        string html = await Shared.GetHTML(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var liNodes = doc.DocumentNode.SelectNodes("//div[@class='detail_right_div']//ul//li");
        if (liNodes == null || liNodes.Count == 0)
        {
            Console.WriteLine("没有找到更多视频条目，结束翻页。");
            return videoList;
        }

        //Console.WriteLine($"第{page}页，找到 {liNodes.Count} 个视频条目");
        foreach (var video in liNodes)
        {
            videoList.Add(video.InnerText);
        }
        return videoList;
    }

    // 单个视频页面，解析视频地址，下载
    //https://www.madou.io/index.php/vod/play/id/29146/sid/1/nid/1.html
    //https://www.madou.io/index.php/vod/play/id/29147/sid/1/nid/1.html
    public async Task ParseVideo(string url)
    {
        Console.WriteLine("解析视频地址");
        string html = await Shared.GetHTML(url);
        // 用 HtmlAgilityPack 解析 HTML
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        string title = titleNode != null ? titleNode.InnerText.Trim() : "未知标题";
        Console.WriteLine($"视频标题: {title}");
        var scriptContent = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'player_aaaa')]")?
                .InnerText ?? "not found";
        // 再从这个 script 标签的内容里抠 url
        string raw = Regex.Match(scriptContent ?? "", @"\""url\""\s*:\s*\""([^\""]+)\""", RegexOptions.IgnoreCase)
                          .Groups[1].Value;
        string m3u8Url = raw.Replace(@"\/", "/");
        Console.WriteLine($"m3u8: {m3u8Url}");

        // 执行下载
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string downloadPath = Path.Combine(desktopPath, title);
        string command = $"-i \"{m3u8Url}\" -c copy \"{downloadPath}.mp4\"";
        var _manager = new FFmpegProcessManager();
        var processInfo = _manager.StartFFmpeg(command, "madou");
        processInfo.Command = "Convert";
        await processInfo.process.WaitForExitAsync();
    }
}