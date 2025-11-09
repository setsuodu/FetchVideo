using FetchVideo.Controllers;


//var webUrl = "https://www.bilibili.com/video/BV1ysySBsExt/"; // B站视频
string baseUrl = "https://api.bilibili.com/x/player/";
string bvId = "BV1Xe1LB5ENJ";
string referer(string bvId) => $"https://www.bilibili.com/video/{bvId}";
string part = ""; //视频名称


// B站视频下载示例
var webUrl = "https://www.bilibili.com/video/BV1ysySBsExt/"; // B站视频
//var extractor = new BilibiliController();
//await extractor.GetBilibiliUpInfoAsync(webUrl);
//await extractor.GetBilibiliVideoAsync("BV1ysySBsExt");


// YouTube视频下载示例
string fullUrl = "https://www.youtube.com/watch?v=ij89E9qABho";
string longUrl = "https://youtu.be/ij89E9qABho";
string shortUrl = "https://www.youtube.com/shorts/fOlW2f38PFE"; //含标题日文
//string url = "https://www.youtube.com/watch?v=CvDpSRuGsjY"; //长视频测试
//var extractor = new YoutubeController();
//await extractor.GetVideoInfoAsync(shortUrl);
//await extractor.GetYoutubeVideoAsync(shortUrl);