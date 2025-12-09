using System.Collections.Concurrent;
using System.Diagnostics;

namespace FetchVideo.Controllers;

public class FFmpegProcessManager
{
    // 改为存储 Process + Info
    private readonly ConcurrentDictionary<string, (Process Process, FFmpegProcessInfo Info)> _processes
        = new();

    // 启动 FFmpeg 并返回任务 ID
    public FFmpegProcessInfo StartFFmpeg(string command, string up_time)
    {
        string up_name = up_time.Split('_')[0]; //去掉时间字串

        var taskId = Guid.NewGuid().ToString();
        Console.WriteLine($"ffmpeg任务: {taskId}, up: {up_name}");
        var startTime = DateTime.UtcNow;

        var info = new FFmpegProcessInfo
        {
            TaskId = taskId,
            UpName = up_name ?? "主播",
            StartTime = startTime,
            Command = command,
            Status = "Running"
        };

        var process = new Process();
        {
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = command,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false, // 打印到console
            };
            process.EnableRaisingEvents = true;
            process.Exited += (s, e) =>
            {
                Console.WriteLine($"监听到 Exited");
                info.Status = process.ExitCode == 0 ? "Completed" : "Error";
                RemoveProcess(taskId);
            };

            try
            {
                process.Start();
                info.process = process;
                _processes.TryAdd(taskId, (process, info));
                Console.WriteLine($"info.process : {info.process != null}");
                return info;
            }
            catch (Exception ex)
            {
                info.Status = "Failed to start";
                throw new InvalidOperationException($"FFmpeg 启动失败: {ex.Message}", ex);
            }
        }
    }

    public async Task<bool> StopFFmpeg(string taskId)
    {
        if (!_processes.TryGetValue(taskId, out var entry))
            return false;

        var (process, info) = entry;

        if (process.HasExited)
        {
            _processes.TryRemove(taskId, out _);
            return false;
        }

        try
        {
            // 优雅停止
            await process.StandardInput.WriteLineAsync("q");
            await Task.Delay(3000); // 等待最多 3 秒

            if (!process.HasExited)
            {
                process.Kill();
            }

            info.Status = "Stopped";
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            _processes.TryRemove(taskId, out _);
            process.Dispose();
        }
    }

    private void RemoveProcess(string taskId)
    {
        _processes.TryRemove(taskId, out _);
    }

    // 可选：获取运行中的任务列表
    public List<FFmpegTaskDto> GetRunningTasks()
    {
        var running = _processes.Values
        .Where(x => x.Info.process != null && !x.Info.process.HasExited)
        .Select(x => new FFmpegTaskDto
        {
            TaskId = x.Info.TaskId,
            UpName = x.Info.UpName,
            StartTime = x.Info.StartTime,
            Command = x.Info.Command,
            Status = "Running",
            // 可选：实时读取一些进程信息
            //PeakMemoryMb = x.Info.process.PeakWorkingSet64 / 1024 / 1024,
            // CPU 使用率需要通过性能计数器或多次采样计算，这里简化
        })
        .OrderByDescending(x => x.StartTime)
        .ToList();
        return running;
    }
    public async Task StopTasks()
    {
        foreach (var process in _processes)
        {
            var taskId = process.Value.Info.TaskId;
            var success = await StopFFmpeg(taskId);
            if (success)
            {
                // 已停止
                RemoveProcess(taskId);
            }
        }
    }
}

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