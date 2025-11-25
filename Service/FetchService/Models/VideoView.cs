namespace FetchService.Models;

public class VideoView
{
    public Owner owner { get; set; }
    public string title { get; set; }
    public string cid { get; set; }
}

public class Owner
{
    public string mid { get; set; }
    public string name { get; set; }
    public string face { get; set; }
}