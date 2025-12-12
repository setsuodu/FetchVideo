namespace FetchVideo.Models;

/// <summary>
/// 麻豆/杏吧视频信息实体类
/// </summary>
public class MadouDto
{
    /// <summary>
    /// 视频标题（用于文件名）
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// m3u8 播放地址
    /// </summary>
    public string Url { get; set; } = string.Empty;

    // 可选：以后想加的字段直接在这里扩展
    // public string Cover { get; set; }
    // public string Duration { get; set; }
    // public int Aid { get; set; }
}