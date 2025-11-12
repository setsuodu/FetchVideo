class YouTubeVideoInfo
{
    public string Title { get; set; }
    public string Author { get; set; }
    public string ChannelId { get; set; }
    public string Description { get; set; }
    public string ThumbnailUrl { get; set; }
    public DateTimeOffset? UploadDate { get; set; }
    public TimeSpan? Duration { get; set; }
}