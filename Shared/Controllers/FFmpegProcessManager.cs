using System.Collections.Concurrent;
using System.Diagnostics;
using FetchVideo.Models;

namespace FetchVideo.Controllers;

public class FFmpegProcessManager
{
    // 改为存储 Process + Info
    private readonly ConcurrentDictionary<string, (Process Process, FFmpegProcessInfo Info)> _processes
        = new();

    // 启动 FFmpeg 并返回任务 ID
    // 兼容（BV视频，B站直播，Youtube视频）
    // 传入实际命令 command
    // 传入任务描述
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
                _processes.TryRemove(taskId, out _);
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
    // 停止 FFmpeg 并返回成功/失败
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
                _processes.TryRemove(taskId, out _);
            }
        }
    }
}