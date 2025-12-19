using System.ComponentModel.DataAnnotations;

namespace FetchVideo.Models;

public class LinkItem
{
    [Key]  // 主键，通常自增
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty; // 主播用户名

    public string Url { get; set; } = string.Empty; // 直播间地址
    
    public bool IsSubscribed { get; set; } = false;  // 默认不订阅

    public int Duration { get; set; } = 2;  // 默认录制2分钟
}

// 请求模型（只用于单个操作）
public class SubscribeRequest
{
    public int? Id { get; set; }
}