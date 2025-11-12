//https://api.bilibili.com/x/player/playurl?bvid=BV1ysySBsExt&cid=33495319307&qn=80&fnval=16
//参数说明：
//bvid：BV号
//cid：视频分P ID
//qn：清晰度（80 表示1080P）
//fnval = 16：表示请求 DASH 流（音视频分离）去掉不返回dash

//返回 JSON 中 data.dash 里会有：
//video 数组：视频流 .m4s
//audio 数组：音频流 .m4s
//如果只想 mp4，可能要使用 data.durl（flv/mp4 流）

public class dataSegmentBaseType
{
    public string Initialization { get; set; }

    public string indexRange { get; set; }
}

public class dataSegment_baseType
{
    public string initialization { get; set; }

    public string index_range { get; set; }
}

public class dataVideoType
{
    public int id { get; set; }

    public string baseUrl { get; set; }

    public string base_url { get; set; }

    public List<string> backupUrl { get; set; }

    public List<string> backup_url { get; set; }

    public int bandwidth { get; set; }

    public string mimeType { get; set; }

    public string mime_type { get; set; }

    public string codecs { get; set; }

    public int width { get; set; }

    public int height { get; set; }

    public string frameRate { get; set; }

    public string frame_rate { get; set; }

    public string sar { get; set; }

    public int startWithSap { get; set; }

    public int start_with_sap { get; set; }

    public dataSegmentBaseType SegmentBase { get; set; }

    public dataSegment_baseType segment_base { get; set; }

    public int codecid { get; set; }
}

public class dataAudioType
{
    public int id { get; set; }

    public string baseUrl { get; set; }

    public string base_url { get; set; }

    public List<string> backupUrl { get; set; }

    public List<string> backup_url { get; set; }

    public int bandwidth { get; set; }

    public string mimeType { get; set; }

    public string mime_type { get; set; }

    public string codecs { get; set; }

    public int width { get; set; }

    public int height { get; set; }

    public string frameRate { get; set; }

    public string frame_rate { get; set; }

    public string sar { get; set; }

    public int startWithSap { get; set; }

    public int start_with_sap { get; set; }

    public dataSegmentBaseType SegmentBase { get; set; }

    public dataSegment_baseType segment_base { get; set; }

    public int codecid { get; set; }

}

public class dataDolbyType
{
    public int type { get; set; }

    public object audio { get; set; }
}

public class DashType
{
    public int duration { get; set; }

    public double minBufferTime { get; set; }

    public double min_buffer_time { get; set; }

    public List<dataVideoType> video { get; set; }

    public List<dataAudioType> audio { get; set; }

    public dataDolbyType dolby { get; set; }

    public object flac { get; set; }

}

public class Support_formatsType
{
    public int quality { get; set; }

    public string format { get; set; }

    public string new_description { get; set; }

    public string display_desc { get; set; }

    public string superscript { get; set; }

    public List<string> codecs { get; set; }

}

public class Play_confType
{
    public bool is_new_description { get; set; }

}

public class DataType
{
    public string from { get; set; }

    public string result { get; set; }

    public string message { get; set; }

    public int quality { get; set; }

    public string format { get; set; }

    public int timelength { get; set; }

    public string accept_format { get; set; }

    public List<string> accept_description { get; set; }

    public List<int> accept_quality { get; set; }

    public int video_codecid { get; set; }

    public string seek_param { get; set; }

    public string seek_type { get; set; }

    public DashType dash { get; set; }

    public List<Support_formatsType> support_formats { get; set; }

    public object high_format { get; set; }

    public int last_play_time { get; set; }

    public int last_play_cid { get; set; }

    public object view_info { get; set; }

    public Play_confType play_conf { get; set; }

    public string cur_language { get; set; }

    public int cur_production_type { get; set; }

}

public class Root
{
    public int code { get; set; }

    public string message { get; set; }

    public int ttl { get; set; }

    public DataType data { get; set; }

}