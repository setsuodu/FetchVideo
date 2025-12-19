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

public class LinkItemDisplayDto
{
    public int Id { get; set; }                  // 序号（主键）

    public string Name { get; set; } = string.Empty;   // 主播用户名

    public string Url { get; set; } = string.Empty;    // 直播间地址（可选显示或隐藏）

    public bool IsSubscribed { get; set; }       // 是否订阅

    public string CurrentStatus { get; set; } = "空闲";  // 当前状态：空闲 / 录制中 等

    // 新增：仅当正在录制时才有值，前端根据这两个字段计算剩余时间
    public DateTime? StartTime { get; set; }  // 录制开始时间（UTC 或本地时间，保持一致即可）

    public int DurationSeconds { get; set; } = 0;  // 计划录制时长（秒）
}

// 请求模型（只用于单个操作）
public class SubscribeRequest
{
    public int? Id { get; set; }
}