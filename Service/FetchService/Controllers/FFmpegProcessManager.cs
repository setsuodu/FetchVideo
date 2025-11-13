using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FetchService.Controllers;

public class FFmpegProcessManager
{
    // 改为存储 Process + Info
    private readonly ConcurrentDictionary<string, (Process Process, FFmpegProcessInfo Info)> _processes
        = new();

    // 启动 FFmpeg 并返回任务 ID
    public FFmpegProcessInfo StartFFmpeg(string command)
    {
        var taskId = Guid.NewGuid().ToString();
        Console.WriteLine($"ffmpeg任务: {taskId}");
        var startTime = DateTime.UtcNow;
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = command,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = false, // 打印到console
                RedirectStandardError = true, // 可选：捕获错误日志
                RedirectStandardOutput = true
            },
            EnableRaisingEvents = true
        };

        var info = new FFmpegProcessInfo
        {
            TaskId = taskId,
            StartTime = startTime,
            Command = command,
            Status = "Running"
        };

        process.Exited += (s, e) =>
        {
            info.Status = process.ExitCode == 0 ? "Completed" : "Error";
            RemoveProcess(taskId);
        };

        try
        {
            process.Start();
            info.ProcessId = process.Id; // 记录系统 PID
            _processes.TryAdd(taskId, (process, info));
            return info;
        }
        catch (Exception ex)
        {
            info.Status = "Failed to start";
            throw new InvalidOperationException($"FFmpeg 启动失败: {ex.Message}", ex);
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
    public List<FFmpegProcessInfo> GetRunningTasks()
    {
        return _processes.Values
            .Where(x => !x.Process.HasExited)
            .Select(x => x.Info)
            .ToList();
    }
}

public class FFmpegProcessInfo
{
    public string TaskId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public string Command { get; set; } = string.Empty;
    public string Status { get; set; } = "Running"; // Running, Stopped, Error
    public int? ProcessId { get; set; } // 可选：系统 PID
}