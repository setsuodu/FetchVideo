using System.Text;
using FetchVideo.Controllers;

// 仅显示问题,视频标题支持英中日韩等多语言字符
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("--- .NET URL 下载器 ---");
Console.Write("请输入要下载的 URL (例如: https://www.example.com): ");

// 1. 读取用户输入
string url = Console.ReadLine();

if (string.IsNullOrWhiteSpace(url))
{
    Console.WriteLine("URL 不能为空。程序退出。");
    return;
}

// 2. 开始下载
var route = new Router();
route.Check(url);

Console.WriteLine("\n按任意键退出...");
Console.ReadKey();

return;

// YouTube视频下载示例
string fullUrl = "https://www.youtube.com/watch?v=ij89E9qABho";
string longUrl = "https://youtu.be/ij89E9qABho"; // 同👆的短地址
string shortUrl = "https://www.youtube.com/shorts/fOlW2f38PFE"; //含标题日文
//string url = "https://www.youtube.com/watch?v=CvDpSRuGsjY"; //长视频测试
//var tube = new YoutubeController();
//await tube.GetVideoInfoAsync(shortUrl);
//await tube.GetYoutubeVideoAsync(shortUrl);


//Console.WriteLine("程序执行完毕，按任意键退出...");
//Console.ReadKey(); // 等待用户按键