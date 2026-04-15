using System.Diagnostics;

namespace FetchVideo.Models;

// 👇里面有 Process，即有敏感信息，又无法直接返回，需要做 Dto
public class FFmpegTask
{
    public string TaskId { get; set; } = string.Empty; // Guid生成
    public string Command { get; set; } = string.Empty;
    public Process Process { get; set; }

    public string UpName { get; set; } = string.Empty; // 主播名
    public string RoomId { get; set; } = string.Empty; // 直播间Id

    public DateTime StartTime { get; set; } // 开始时间
    public int Duration { get; set; } = 2; // 录制时间（分钟）
    public string Status { get; set; } = "Running"; // Running / Stopped / Error
}
// 纯数据传输对象，没有 Process 之类的
public class FFmpegTaskDto
{
    public string TaskId { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;

    public string UpName { get; set; } = string.Empty;
    public string RoomId { get; set; } = string.Empty; // 直播间Id

    public DateTime StartTime { get; set; } // 开始时间
    public string StartTimeDisplay => StartTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string RunningTime => (DateTime.Now - StartTime).ToString(@"hh\:mm\:ss"); // 已运行时长

    public int Duration { get; set; } = 2;
    public DateTime EndTime => StartTime.AddMinutes(Duration); // 结束时间
    public string EndTimeDisplay => EndTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string LeftTime => (EndTime - DateTime.Now).ToString(@"hh\:mm\:ss"); // 剩余时长

    public string Status { get; set; } = "Running";   // 空闲 / 录制中

    // 可选额外信息（推荐加）
    //public int? ExitCode { get; set; } // 只有结束的进程才有
    public long? PeakMemoryMb { get; set; } // MB，峰值内存（可通过 process.PeakWorkingSet64 采集）
    //public double? CpuUsage { get; set; } // 当前 CPU 使用率（需要额外采集）
}