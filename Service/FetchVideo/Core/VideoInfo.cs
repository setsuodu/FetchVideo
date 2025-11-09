namespace FetchVideo.Core;

public class VideoInfo
{
    public string Title { get; set; }
    public string Author { get; set; }
    public TimeSpan Duration { get; set; }
    public List<StreamInfo> Streams { get; set; } = new();
}

public class StreamInfo
{
    public string Quality { get; set; }
    public string Format { get; set; } // mp4, dash, hls...
    public long Size { get; set; }
    public string Url { get; set; }
    public bool IsDash { get; set; }
}
