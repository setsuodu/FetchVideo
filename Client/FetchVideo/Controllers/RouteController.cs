namespace FetchVideo.Controllers;

public class RouteController //路由器
{
    public async void Check(string url)
    {
        // ①B站视频
        // ②B站直播
        // ③YouTube视频
        // missav访问时用油猴自动提交SQL，没有则记入本地文件
        if (url.Contains("bilibili.com/video/BV"))
        {
            // https://www.bilibili.com/video/BV1ysySBsExt/
            // 获取视频标题
            string bvId = Shared.GetBvId(url);
            Console.WriteLine($"是 Bilibili视频: bvId={bvId}");

            var bili = new BilibiliController();
            //await bili.GetUpInfo(bvId); // 获取Up信息，不需要
            await bili.GetBilibiliVideoAsync(bvId); // 获取视频
        }
        else if (url.Contains("live.bilibili"))
        {
            // https://live.bilibili.com/1792597682
            // Up主信息 + 当前时间戳
            string roomId = Shared.GetRoomId(url);
            Console.WriteLine($"是 Bilibili直播: 房间: {roomId}");
            var bili = new BilibiliController();
            string title = await bili.GetTitleAsync(url);
            Console.WriteLine($"直播标题: {title}");
            await bili.GetM3U8(roomId, title);
        }
        else if (url.Contains("youtu"))
        {
            //string fullUrl = "https://www.youtube.com/watch?v=ij89E9qABho"; // 标准地址
            //string sUrl = "https://youtu.be/ij89E9qABho"; // 短地址
            //string shortUrl = "https://www.youtube.com/shorts/fOlW2f38PFE"; // Short地址（含标题日文）
            //string longUrl = "https://www.youtube.com/watch?v=CvDpSRuGsjY"; // 标准地址2（测试）
            // 获取视频标题
            Console.WriteLine($"是 Youtube视频: ");

            var tube = new YoutubeController();
            await tube.GetYoutubeVideoAsync(url);
        }
        else
        {
            Console.WriteLine($"不支持的网站: {url}");
        }
    }
}
