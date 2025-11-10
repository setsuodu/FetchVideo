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

//Console.WriteLine("\n按任意键退出...");
Console.ReadKey();