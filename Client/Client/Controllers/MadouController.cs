using System.Text.RegularExpressions;
using FetchVideo.Models;
using FetchVideo.Utils;
using HtmlAgilityPack;

namespace FetchVideo.Controllers;

public class MadouController
{
    const string DOMAIN_URL = "https://www.madou.io";
    const string BASE_URL = "https://www.madou.io/index.php/vod";

    public static (int typeId, int page) ExtractTypeAndPage(string url)
    {
        // 正则匹配 /type/id/{type_id} 或 /type/id/{type_id}/page/{page}
        string pattern = @"/type/id/(\d+)(?:/page/(\d+))?\.html";
        Match match = Regex.Match(url, pattern);

        if (match.Success)
        {
            int typeId = int.Parse(match.Groups[1].Value);
            int page = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
            return (typeId, page);
        }

        throw new ArgumentException("URL 格式不匹配，无法提取 type_id 或 page。");
    }

    // 翻页批处理所有视频列表
    // BASE_URL = "https://www.madou.io/index.php/vod";
    // 第一页开始翻页，最大页数未知
    // 列表第一页 https://www.madou.io/index.php/vod/type/id/21.html
    // 第一页 https://www.madou.io/index.php/vod/type/id/21/page/1.html
    // 第七页 https://www.madou.io/index.php/vod/type/id/21/page/7.html
    public async Task<List<MadouDto>> ParseAllPages(int typeId)
    {
        Console.WriteLine($"开始翻页批处理[{typeId}]类所有视频列表");
        List<MadouDto> all = new List<MadouDto>();

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

        return all;
    }

    // 分析页面上的视频
    public async Task<List<MadouDto>> ParsePage(int typeId, int page)
    {
        var videoList = new List<MadouDto>();

        Console.WriteLine($"分析本页上的视频: typeId={typeId}, page={page}");
        string url = $"{BASE_URL}/type/id/{typeId}/page/{page}.html";
        Console.WriteLine($"url: {url}");

        string html = await Shared.GetHTML(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var liNodes = doc.DocumentNode.SelectNodes("//div[@class='detail_right_div']//ul//li//p[@class='img']/a");
        if (liNodes == null || liNodes.Count == 0)
        {
            Console.WriteLine("没有找到更多视频条目，结束翻页。");
            return videoList;
        }

        //Console.WriteLine($"第{page}页，找到 {liNodes.Count} 个视频条目");
        foreach (var linkNode in liNodes)
        {
            string href = linkNode.GetAttributeValue("href", "");
            string video_url = DOMAIN_URL + href;
            Console.WriteLine($"href: {video_url}");
            var dto = await ParseVideo(video_url);
            videoList.Add(dto);
        }
        return videoList;
    }

    // 解析单个视频
    //https://www.madou.io/index.php/vod/play/id/29146/sid/1/nid/1.html
    //https://www.madou.io/index.php/vod/play/id/29147/sid/1/nid/1.html
    public async Task<MadouDto> ParseVideo(string url)
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

        MadouDto dto = new MadouDto { Title = title, Url = m3u8Url };
        return dto;
    }

    public async Task Parallel(List<MadouDto> list)
    {
        var downloader = new MadouVideoDownloader();
        await downloader.DownloadAllAsync(list, @"V:\【杏吧】");
    }
}