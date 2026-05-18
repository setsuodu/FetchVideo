namespace FetchVideo.Models;

public class BiliBatchRequest
{
    public string UpName { get; set; } = "未知UP";
    public string Mid { get; set; } = "";
    public List<BiliVideoItem> Videos { get; set; } = new();
}