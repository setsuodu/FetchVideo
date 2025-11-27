namespace FetchVideo.Models;

public class RoomInfo
{
    public double uid { get; set; } //B站用户ID: 3632310192704110 / 441807008
    public byte live_status { get; set; } //0:未开播 / 1:一开播 / 2:轮播
    public string title { get; set; } //主播自定义标题
    public string user_cover { get; set; } //封面图
    public string keyframe { get; set; } //即时截图
    public string live_time { get; set; } //开播时间: "2025-11-27 07:55:03",
    public string parent_area_name { get; set; } //主分区: "娱乐",
    public string area_name { get; set; } //分区: "颜值" / "萌宅领域",
}