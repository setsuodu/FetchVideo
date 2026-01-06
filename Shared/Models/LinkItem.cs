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

public static class LinkItemSQL
{
    public static List<LinkItem> NewList()
    {
        return new List<LinkItem>
        {
            // 不订阅的
            new LinkItem { Name = "软水妮",         Url = "https://live.bilibili.com/1842861593", IsSubscribed = false },
            new LinkItem { Name = "至尊强者小知恩", Url = "https://live.bilibili.com/1728867738", IsSubscribed = false },
            new LinkItem { Name = "空空学妹-Sub",   Url = "https://live.bilibili.com/1725923601", IsSubscribed = false },
            new LinkItem { Name = "你别芭乐我_",    Url = "https://live.bilibili.com/1772923624", IsSubscribed = false },
            new LinkItem { Name = "小蜜疯_璀璨",    Url = "https://live.bilibili.com/1729389539", IsSubscribed = false },
            new LinkItem { Name = "蔓柠er",         Url = "https://live.bilibili.com/1786565278", IsSubscribed = false },
            new LinkItem { Name = "甜奈-好运常伴",  Url = "https://live.bilibili.com/1984186978", IsSubscribed = false },
            new LinkItem { Name = "婉婉每天吃十个舰长", Url = "https://live.bilibili.com/1909460797", IsSubscribed = false },
            // 订阅的
            new LinkItem { Name = "牛角包去睡了",   Url = "https://live.bilibili.com/1904551806", IsSubscribed = true },
            new LinkItem { Name = "废宅三文鱼",     Url = "https://live.bilibili.com/1871979231", IsSubscribed = true },
            new LinkItem { Name = "小福包iu_",      Url = "https://live.bilibili.com/1868870262", IsSubscribed = true },
            new LinkItem { Name = "羊莓杨莓",       Url = "https://live.bilibili.com/1792597682", IsSubscribed = true },
            new LinkItem { Name = "枳月味奶片",     Url = "https://live.bilibili.com/1804469695", IsSubscribed = true },
            new LinkItem { Name = "钟意大堡包",     Url = "https://live.bilibili.com/1986467930", IsSubscribed = true },
            new LinkItem { Name = "你没下周可爱",   Url = "https://live.bilibili.com/1872972713", IsSubscribed = true },
            new LinkItem { Name = "小四桃子",       Url = "https://live.bilibili.com/1724212928", IsSubscribed = true },
            new LinkItem { Name = "星梨梨S",        Url = "https://live.bilibili.com/1747118475", IsSubscribed = true },
            new LinkItem { Name = "aeri酱咩",       Url = "https://live.bilibili.com/1948312359", IsSubscribed = true },
            new LinkItem { Name = "bili家家",       Url = "https://live.bilibili.com/1828675450", IsSubscribed = true },
            new LinkItem { Name = "青清禾月",       Url = "https://live.bilibili.com/1786263208", IsSubscribed = true },
            new LinkItem { Name = "秋茗ovo",        Url = "https://live.bilibili.com/1946284495", IsSubscribed = true },
            new LinkItem { Name = "开心螺蛳粉宝宝", Url = "https://live.bilibili.com/1868871042", IsSubscribed = true },
            new LinkItem { Name = "香脆小海苔",     Url = "https://live.bilibili.com/1842356567", IsSubscribed = true },
            new LinkItem { Name = "冻泥不是冰的",   Url = "https://live.bilibili.com/1898932569", IsSubscribed = true },
            new LinkItem { Name = "憨憨打不服",     Url = "https://live.bilibili.com/1779344403", IsSubscribed = true },
            new LinkItem { Name = "来份鱼酱耶",     Url = "https://live.bilibili.com/1814375548", IsSubscribed = true },
            new LinkItem { Name = "yy的小歪",       Url = "https://live.bilibili.com/1712256876", IsSubscribed = true },
            new LinkItem { Name = "Umi今天也发财",  Url = "https://live.bilibili.com/1840178918", IsSubscribed = true },
            new LinkItem { Name = "Aaa薯条大王_",   Url = "https://live.bilibili.com/1787120212", IsSubscribed = true },
            new LinkItem { Name = "音音子ing",      Url = "https://live.bilibili.com/1930401379", IsSubscribed = true },
            new LinkItem { Name = "小乖姿",         Url = "https://live.bilibili.com/1849301415", IsSubscribed = true },
            new LinkItem { Name = "池_渔",          Url = "https://live.bilibili.com/1849823023", IsSubscribed = true },
            new LinkItem { Name = "未薇i",          Url = "https://live.bilibili.com/1861375125", IsSubscribed = true },
            new LinkItem { Name = "小利萝",         Url = "https://live.bilibili.com/31734361", IsSubscribed = true },
            new LinkItem { Name = "草莓果酱呐",     Url = "https://live.bilibili.com/30950163", IsSubscribed = true },
            new LinkItem { Name = "小鱼要饿死了",   Url = "https://live.bilibili.com/32443468", IsSubscribed = true },
            new LinkItem { Name = "濛雨清波",       Url = "https://live.bilibili.com/21156534", IsSubscribed = true },
            new LinkItem { Name = "呀思思",         Url = "https://live.bilibili.com/11368496", IsSubscribed = true },
            new LinkItem { Name = "梗洋洋",         Url = "https://live.bilibili.com/27880410", IsSubscribed = true },
            new LinkItem { Name = "路人饼饼ovo",    Url = "https://live.bilibili.com/4533196", IsSubscribed = true },
            // 可以继续添加更多默认链接
            new LinkItem { Name = "是只萌宠",       Url = "https://live.bilibili.com/31735043", IsSubscribed = true },
            new LinkItem { Name = "小栩小不点-",    Url = "https://live.bilibili.com/32270626", IsSubscribed = true },
            new LinkItem { Name = "_橘子_味的猫",   Url = "https://live.bilibili.com/32058161", IsSubscribed = true },
            new LinkItem { Name = "林一璇-",        Url = "https://live.bilibili.com/14695348", IsSubscribed = true },
            new LinkItem { Name = "番外の宅",       Url = "https://live.bilibili.com/1930407195", IsSubscribed = true },// like=牛角包
            new LinkItem { Name = "样样不知道",     Url = "https://live.bilibili.com/1791356093", IsSubscribed = true },
            new LinkItem { Name = "草莓萱萱-",      Url = "https://live.bilibili.com/1851391546", IsSubscribed = true },
            new LinkItem { Name = "在下小颖是也",   Url = "https://live.bilibili.com/1904557421", IsSubscribed = true },
            new LinkItem { Name = "我是小龙虾大王", Url = "https://live.bilibili.com/1874223564", IsSubscribed = true },
            new LinkItem { Name = "林尤奈",         Url = "https://live.bilibili.com/1977907120", IsSubscribed = true },
            new LinkItem { Name = "佐伊Z0E",        Url = "https://live.bilibili.com/1732021672", IsSubscribed = true },
            new LinkItem { Name = "doki小美-",      Url = "https://live.bilibili.com/1870440922", IsSubscribed = true },
            new LinkItem { Name = "小芋喵_以梦冠",  Url = "https://live.bilibili.com/1904559280", IsSubscribed = true },
            new LinkItem { Name = "萱萱xxOvO",      Url = "https://live.bilibili.com/1964696515", IsSubscribed = true },
            new LinkItem { Name = "一只小梨涡a_",   Url = "https://live.bilibili.com/1771626222", IsSubscribed = true },
            new LinkItem { Name = "星予iuk",        Url = "https://live.bilibili.com/1768502866", IsSubscribed = true },
            new LinkItem { Name = "周周aaaaaa-",    Url = "https://live.bilibili.com/1871518514", IsSubscribed = true },
            new LinkItem { Name = "熙熙兔ツ",       Url = "https://live.bilibili.com/1991384022", IsSubscribed = true },
            new LinkItem { Name = "沐川琦",         Url = "https://live.bilibili.com/1799898897", IsSubscribed = true },
            new LinkItem { Name = "饼饼的酱",       Url = "https://live.bilibili.com/1808576670", IsSubscribed = true },
            new LinkItem { Name = "鲨鱼摆摆崽",     Url = "https://live.bilibili.com/1889475119", IsSubscribed = true },
            new LinkItem { Name = "牙牙大王yy-串串惯", Url = "https://live.bilibili.com/1921278229", IsSubscribed = true },
            new LinkItem { Name = "眠眠鱼菜菜-idea宠", Url = "https://live.bilibili.com/1955141527", IsSubscribed = true },
        };
    }
}