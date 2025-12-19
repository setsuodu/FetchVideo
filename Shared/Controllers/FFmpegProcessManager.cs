using FetchVideo.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FetchVideo.Controllers;

public class FFmpegProcessManager
{
    // 改为存储 Process + Info
    private readonly ConcurrentDictionary<string, FFmpegTask> _processes = new();

    // 启动 FFmpeg 并返回任务 ID
    // 兼容（BV视频，B站直播，Youtube视频）
    // 传入实际命令 command
    // 传入任务描述
    public FFmpegTask StartFFmpeg(string command, string up_name = "", int minute = 0)
    {
        var taskId = Guid.NewGuid().ToString();
        Console.WriteLine($"ffmpeg任务: {taskId}, up: {up_name}");

        var task = new FFmpegTask
        {
            TaskId = taskId,
            Command = command,
            Process = null,

            UpName = up_name,
            StartTime = DateTime.UtcNow,
            Duration = minute,
            Status = "Running",
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
                task.Status = process.ExitCode == 0 ? "Completed" : "Error";
                _processes.TryRemove(taskId, out _);
            };

            try
            {
                process.Start();
                task.Process = process;
                _processes.TryAdd(taskId, task);
                Console.WriteLine($"info.process : {task.Process != null}");
                return task;
            }
            catch (Exception ex)
            {
                task.Status = "Failed to start";
                throw new InvalidOperationException($"FFmpeg 启动失败: {ex.Message}", ex);
            }
        }
    }
    
    // 获取运行中的任务列表
    public List<FFmpegTaskDto> GetRunningTasks()
    {
        var running = _processes.Values
        .Where(x => x.Process != null && !x.Process.HasExited)
        .Select(x => ConvertDto(x))
        //.OrderByDescending(x => x.StartTime)
        .ToList();
        return running;
    }

    // 停止 FFmpeg
    public async Task<bool> StopFFmpeg(string taskId)
    {
        if (!_processes.TryGetValue(taskId, out var task))
            return false;

        var process = task.Process;

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

            task.Status = "Stopped";
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
    public async Task StopAll()
    {
        foreach (var process in _processes)
        {
            var taskId = process.Value.TaskId;
            var success = await StopFFmpeg(taskId);
            if (success)
            {
                // 已停止
                _processes.TryRemove(taskId, out _);
            }
        }
    }

    /// <summary>
    /// 从 FFmpeg 命令字符串中提取输出文件（假设只有一个输出，且在 -c copy 之后）
    /// </summary>
    /// <param name="command">完整的 FFmpeg 命令字符串，例如：ffmpeg -i input.mp4 -c copy output.mp4</param>
    /// <returns>输出文件名，如果未找到返回 null</returns>
    public static string ExtractOutput(string command)
    {
        // 先去除开头的 "ffmpeg" 或 "ffmpeg.exe"
        command = command.Trim();
        if (command.StartsWith("ffmpeg.exe", StringComparison.OrdinalIgnoreCase))
            command = command.Substring("ffmpeg.exe".Length).Trim();
        else if (command.StartsWith("ffmpeg", StringComparison.OrdinalIgnoreCase))
            command = command.Substring("ffmpeg".Length).Trim();

        // 分割参数（粗糙但对大多数 FFmpeg 命令有效，处理带空格的路径用引号包围）
        List<string> args = new List<string>();
        Regex regex = new Regex(@"""([^""]+)""|(\S+)");
        foreach (Match m in regex.Matches(command))
        {
            if (m.Groups[1].Success)
                args.Add(m.Groups[1].Value);  // 带引号的参数
            else
                args.Add(m.Groups[2].Value);  // 无引号的参数
        }

        // 找到 "-c" "copy" 的位置
        int cIndex = args.IndexOf("-c");
        if (cIndex == -1 || cIndex + 1 >= args.Count || args[cIndex + 1] != "copy")
            return null;

        // 从 -c copy 之后开始找第一个非选项的参数（即输出文件）
        for (int i = cIndex + 2; i < args.Count; i++)
        {
            string arg = args[i];
            if (!arg.StartsWith("-"))  // 非选项参数，通常就是输出文件
            {
                return arg;
            }
        }

        return null;  // 未找到
    }
    public static FFmpegTaskDto ConvertDto(FFmpegTask task)
    {
        return new FFmpegTaskDto
        {
            TaskId = task.TaskId,
            Command = task.Command,
            UpName = task.UpName,
            StartTime = task.StartTime,
            Duration = task.Duration,
            Status = task.Status,
            //PeakMemoryMb = task.Process?.PeakWorkingSet64 / 1024 / 1024, //MB，已经Exit，无法获取
        };
    }
}