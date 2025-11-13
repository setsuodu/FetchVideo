using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;

namespace FetchService.Controllers;

public class FFmpegProcessManager
{
    private readonly ConcurrentDictionary<string, Process> _processes = new();

    // 启动 FFmpeg 并返回任务 ID
    public Process StartFFmpeg(string ffmpegArgs)
    {
        var taskId = Guid.NewGuid().ToString();
        Console.WriteLine($"ffmpeg任务: {taskId}");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg", // 假设 ffmpeg 在 PATH 中
                Arguments = ffmpegArgs, // 如 "-i input.mp4 output.mp4"
                RedirectStandardInput = true, // 用于发送 'q'
                UseShellExecute = false,
                CreateNoWindow = false, // 打印到console
            },
            EnableRaisingEvents = true
        };

        process.Exited += (sender, e) => RemoveProcess(taskId); // 进程退出时自动移除
        process.Start();

        _processes.TryAdd(taskId, process);
        return process;
    }

    // 停止指定任务的 FFmpeg
    public async Task<bool> StopFFmpeg(string taskId)
    {
        if (_processes.TryRemove(taskId, out var process) && !process.HasExited)
        {
            try
            {
                // 尝试优雅停止：发送 'q'
                await process.StandardInput.WriteLineAsync("q");
                await Task.Delay(2000); // 等待 2 秒让 FFmpeg 响应

                if (!process.HasExited)
                {
                    process.Kill(); // 如果未停止，强制杀死
                }
                process.Dispose();
                return true;
            }
            catch
            {
                return false;
            }
        }
        return false; // 进程不存在或已退出
    }

    private void RemoveProcess(string taskId)
    {
        _processes.TryRemove(taskId, out _);
    }



    // 合并音视频文件
    public string MergeAudioVideo(string videoPath, string audioPath, string outputPath)
    {
        var taskId = Guid.NewGuid().ToString();

        var ffmpeg = new Process();
        //ffmpeg.StartInfo.FileName = "D:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"; // ffmpeg.exe 路径
        ffmpeg.StartInfo.FileName = "ffmpeg"; // 直接写命令名
        ffmpeg.StartInfo.Arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c copy \"{outputPath}\" -y";
        ffmpeg.StartInfo.RedirectStandardInput = true; // 用于发送 'q'
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.CreateNoWindow = true;
        ffmpeg.EnableRaisingEvents = true;
        ffmpeg.Start();
        ffmpeg.WaitForExit();

        // 只有当 FFmpeg 进程退出后，代码才会执行到这里
        //删除源视频的代码 // <-- 这里的代码
        File.Delete(videoPath);
        File.Delete(audioPath);
        return "合并音视频文件";
    }
    // M3U8 转 MP4
    public static void ConvertM3U8toMP42(string room_id, string m3u8Url, string outputPath)
    {
        var ffmpeg = new Process();
        //ffmpeg.StartInfo.FileName = "D:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"; // ffmpeg.exe 路径
        ffmpeg.StartInfo.FileName = "ffmpeg"; // 直接写命令名
        ffmpeg.StartInfo.Arguments = $"-headers \"Referer: {Shared.BILI_LIVE}{room_id}\r\nUser-Agent: Mozilla/5.0\" -i \"{m3u8Url}\" -c copy \"{outputPath}\" -y"; // -y 直接覆盖同名文件，不用交互式选择
        // -t 01:00:00"; // 录制1h自动停止
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.CreateNoWindow = false; //关键①，true不执行
        ffmpeg.Start();
        ffmpeg.WaitForExit();
    }
}