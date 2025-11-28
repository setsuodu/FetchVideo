namespace FetchVideo.Models;

// Models/TodoItem.cs
//public class TodoItem
//{
//    public long Id { get; set; }
//    public string? Name { get; set; }
//    public bool IsComplete { get; set; }
//}

// 主播列表
public class FavoriteUPs
{
    public List<UpItem> upItems { get; set; }
}
public class UpItem
{
    public string? Url { get; set; } // 直播间地址
    public int length { get; set; } // 录制时长
}