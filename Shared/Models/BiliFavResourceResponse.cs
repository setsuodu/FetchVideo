namespace FetchVideo.Models;

// 关键 JSON 结构（只取需要的字段，保持轻量）
public class BiliFolderResponse
{
    public int Code { get; set; }
    public string Message { get; set; }
    public BiliFolderData Data { get; set; }
}

public class BiliFolderData
{
    public List<BiliFolder> List { get; set; }
}

public class BiliFolder
{
    public long Id { get; set; }         // media_id
    public string Title { get; set; }
    public int MediaCount { get; set; } // 视频数量
    public int Attr { get; set; }        // 0=公开, 1=私密
    // 还有 fid、cover 等字段，可自行添加
}

public class BiliFavResourceResponse
{
    public int Code { get; set; }
    public string Message { get; set; }
    public BiliFavData Data { get; set; }
}

public class BiliFavData
{
    public BiliFavInfo Info { get; set; }
    public List<BiliFavVideo> Medias { get; set; }
    public bool has_more { get; set; }
}

public class BiliFavInfo
{
    public long Id { get; set; }
    public string Title { get; set; }
    public int MediaCount { get; set; }
}

public class BiliFavVideo
{
    public string Title { get; set; }
    public string Bvid { get; set; }
    public string Cover { get; set; }
    public long FavTime { get; set; }   // 收藏时间戳
    public BiliCntInfo CntInfo { get; set; }
    // 还有 upper（UP主）、duration 等字段
}

public class BiliCntInfo
{
    public int Play { get; set; }   // 播放量（实际返回的是整数）
    // 还有 collect、danmaku 等
}