namespace FetchVideo.Models;

public class ClockConfig
{
    public List<string> TriggerTimes { get; set; } = new() { "00:00", "12:00" };
}