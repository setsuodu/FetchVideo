using System.Text.RegularExpressions;

namespace FetchService.Controllers;

public class Shared
{
    public const string BILI_VIDEO = "https://www.bilibili.com/video/";
    public const string BILI_LIVE = "https://live.bilibili.com/";
    public const string BILI_PLAYER = "https://api.bilibili.com/x/player/";
    public const string BILI_SPACE = "https://api.bilibili.com/x/space/";
    public const string BILI_INTERFACE = "https://api.bilibili.com/x/web-interface/";
    public const string BILI_ROOM = "https://api.live.bilibili.com/room/v1/Room/";

    public static string GetBvId(string url)
    {
        // 正则表达式 (Regex) 提取BV
        string pattern = @"(BV[a-zA-Z0-9]+)/?";
        Match match = Regex.Match(url, pattern);
        string bvId = match.Groups[0].Value.TrimEnd('/'); // 使用 TrimEnd('/') 确保去除末尾可选的斜杠
        Console.WriteLine($"提取到的 BV 号码: **{bvId}**");
        return bvId;
    }
    public static string GetRoomId(string url)
    {
        // 正则表达式 (Regex) 提取房间号
        string pattern = @"live\.bilibili\.com/(\d+)";
        Match match = Regex.Match(url, pattern);
        string roomId = match.Groups[1].Value;
        Console.WriteLine($"提取到的 房间号: **{roomId}**");
        return roomId;
    }

    // Windows文件名不允许文件名含（\ / : * ? " < > |）
    // 替换为 下划线 _
    public static string MakeFileNameSafe(string name)
    {
        // 常见所有系统的不合法字符
        //char[] invalidChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }; //跨平台写法
        char[] invalidChars = Path.GetInvalidFileNameChars(); //Windows写法

        // 过滤
        foreach (char c in invalidChars)
        {
            name = name.Replace(c, '_');
        }
        return name;
    }


    // 短链👉长链
    public static async Task<string> Curl_I(string shortUrl)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 ...");

        // 1. 构造请求对象
        var request = new HttpRequestMessage(HttpMethod.Head, shortUrl);
        // 2. 发送（等价于 curl -I）
        var response = await client.SendAsync(request);
        var location = response.Headers.Location?.ToString() ?? response.RequestMessage.RequestUri.ToString();

        location = CleanUrl(location);

        if (location.Contains("bilibili.com/video") || location.Contains("/av") || location.Contains("/BV"))
            return $"B站视频: {location}";
        if (location.Contains("live.bilibili.com"))
            return $"B站直播: {location}";
        if (location.Contains("youtube.com/watch"))
            return $"YouTube视频: {location}";

        return $"未知: {location}";
    }

    // 裁掉 "/h5" 和 "?后面多余的"
    public static string CleanUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;

        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch
        {
            return url; // 非法 URL 原样返回
        }

        // 1. 处理 /h5：裁掉 /h5 及之后部分
        string path = uri.AbsolutePath;
        int h5Index = path.IndexOf("/h5", StringComparison.OrdinalIgnoreCase);
        if (h5Index >= 0)
        {
            path = path.Substring(0, h5Index); // 裁掉 /h5 及之后
        }

        // 2. 保留一个 ?，去掉所有查询参数
        string baseUrl = uri.Scheme + "://" + uri.Authority + path;

        // 如果原始 URL 有 ?，保留一个 ?（但不带参数）
        if (url.Contains('?'))
        {
            baseUrl += "?";
        }

        return baseUrl;
    }
}
