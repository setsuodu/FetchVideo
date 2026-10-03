namespace FetchVideo.Models;

/// <summary>
/// 油猴导出的图文（opus）条目
/// </summary>
public class BiliOpusItem
{
    public string Title { get; set; } = "";
    public string OpusId { get; set; } = "";
    public string OpusUrl { get; set; } = "";
}

public class BiliOpusBatchRequest
{
    public string UpName { get; set; } = "未知UP";
    public string Mid { get; set; } = "";
    public List<BiliOpusItem> Items { get; set; } = new();
}
