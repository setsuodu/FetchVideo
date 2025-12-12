using System.Collections.Concurrent;
using System.Diagnostics;
using FetchVideo.Models;

namespace FetchVideo.Utils;

public class MadouVideoDownloader
{
    private const int MaxDegreeOfParallelism = 2;   // m3u8 推荐 2~3 并发
    private const int MaxRetryCount = 5;
    private const int RetryDelayMs = 5000;

    public async Task DownloadAllAsync(List<MadouDto> videoList, string saveFolder)
    {
        if (videoList == null || videoList.Count == 0)
        {
            Console.WriteLine("视频列表为空，无需下载。");
            return;
        }

        Directory.CreateDirectory(saveFolder);

        var queue = new BlockingCollection<MadouDto>(new ConcurrentQueue<MadouDto>(videoList));

        var tasks = new Task[MaxDegreeOfParallelism];
        for (int i = 0; i < MaxDegreeOfParallelism; i++)
        {
            tasks[i] = Task.Run(() => Worker(queue, saveFolder));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine("所有视频下载任务已完成！");
    }

    private static void Worker(BlockingCollection<MadouDto> queue, string saveFolder)
    {
        while (!queue.IsCompleted)
        {
            if (!queue.TryTake(out MadouDto video, millisecondsTimeout: 1000))
                continue;

            string safeFileName = GetSafeFileName(video.Title) + ".mp4";
            string outputPath = Path.Combine(saveFolder, safeFileName);

            // 如果文件已存在且大于 10MB，视为已完整下载，跳过
            if (File.Exists(outputPath) && new FileInfo(outputPath).Length > 10 * 1024 * 1024)
            {
                Console.WriteLine($"已存在，跳过: {video.Title}");
                continue;
            }

            bool success = false;
            for (int retry = 1; retry <= MaxRetryCount && !success; retry++)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 下载 ({retry}/{MaxRetryCount}): {video.Title}");

                success = DownloadWithFFmpeg(video.Url, outputPath);

                if (!success)
                {
                    Console.WriteLine($"第 {retry} 次失败，{(retry < MaxRetryCount ? $"{RetryDelayMs / 1000}s 后重试..." : "放弃")}");
                    if (retry < MaxRetryCount)
                        Thread.Sleep(RetryDelayMs);
                }
                else
                {
                    Console.WriteLine($"下载完成: {safeFileName}");
                }
            }

            if (!success)
            {
                Console.WriteLine($"最终失败: {video.Title} → {video.Url}");
            }
        }
    }

    private static bool DownloadWithFFmpeg(string m3u8Url, string outputPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments = $"-i \"{m3u8Url}\" " +
                "-c copy " +
                "-bsf:a aac_adtstoasc " +
                "-y " +                     // 覆盖临时文件
                $"\"{outputPath}\"",
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = false,
        };

        try
        {
            using var process = Process.Start(startInfo);
            if (process == null) return false;

            // 读取 ffmpeg 输出（进度在 stderr）
            _ = process.StandardError.ReadToEndAsync(); // 不阻塞主线程
            process.WaitForExit(7200_000); // 最多等 2 小时

            return process.HasExited && process.ExitCode == 0 && File.Exists(outputPath) && new FileInfo(outputPath).Length > 0;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// 清理 Windows/Linux 不允许的文件名字符
    /// </summary>
    private static string GetSafeFileName(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            title = "未命名视频";

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            title = title.Replace(c, '_');
        }

        title = title.Trim('.', ' '); // 去掉首尾点和空格

        // 限制长度，避免超长路径
        if (title.Length > 150)
            title = title.Substring(0, 150);

        return title;
    }
}