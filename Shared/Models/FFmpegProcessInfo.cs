using System.Diagnostics;

namespace FetchVideo.Models;

// 👇里面有 Process，即有敏感信息，又无法直接返回，需要做 Dto
public class FFmpegProcessInfo
{
    public string TaskId { get; set; } = string.Empty;
    public string UpName { get; set; } = string.Empty; // 主播名
    public DateTime StartTime { get; set; }
    public int Minute { get; set; } // 录制时间
    public string Command { get; set; } = string.Empty;
    public string Status { get; set; } = "Running"; // Running / Stopped / Error
    public Process process { get; set; }
}
public class FFmpegTaskDto
{
    public string TaskId { get; set; } = string.Empty;

    public string UpName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }          // 开始时间
    public string StartTimeDisplay => StartTime.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    public int Munite { get; set; } = 2;

    public string RunningTime => (DateTime.Now - StartTime).ToString(@"hh\:mm\:ss"); // 已运行时长

    public string Command { get; set; } = string.Empty;

    // 可选：只展示命令的一部分，避免太长前端显示不下
    public string ShortCommand => Command.Length > 100
        ? Command.Substring(0, 97) + "..."
        : Command;

    public string Status { get; set; } = "Running";   // Running / Completed / Error / Stopped

    // 可选额外信息（推荐加）
    //public int? ExitCode { get; set; }                // 只有结束的进程才有
    //public long? PeakMemoryMb { get; set; }           // 峰值内存（可通过 process.PeakWorkingSet64 采集）
    //public double? CpuUsage { get; set; }             // 当前 CPU 使用率（需要额外采集）
}
