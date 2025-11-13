using System.Diagnostics;
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
}
