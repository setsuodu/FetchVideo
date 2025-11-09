using System.Diagnostics;

namespace FetchVideo.Controllers;

public class Shared
{
    public static void MergeAudioVideo(string videoPath, string audioPath, string outputPath)
    {
        var ffmpeg = new Process();
        ffmpeg.StartInfo.FileName = "D:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"; // ffmpeg.exe 路径
        ffmpeg.StartInfo.Arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c copy \"{outputPath}\" -y";
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.CreateNoWindow = true;
        ffmpeg.Start();
        ffmpeg.WaitForExit();
    }

    public static void M3U8toMP4(string room_id, string m3u8Path, string outputPath)
    {
        var ffmpeg = new Process();
        ffmpeg.StartInfo.FileName = "D:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"; // ffmpeg.exe 路径
        //ffmpeg.StartInfo.Arguments = $" -headers \"Referer: https://live.bilibili.com/{room_id}\\r\\nUser-Agent: Mozilla/5.0\" -i \"{m3u8Path}\" -c copy \"{outputPath}\" -y"; // -y 直接覆盖同名文件，不用交互式选择
        ffmpeg.StartInfo.Arguments = $"-headers \"Referer: https://live.bilibili.com/{room_id}\r\nUser-Agent: Mozilla/5.0\" -i \"{m3u8Path}\" -c copy \"live_record.mp4\" -y"; // -y 直接覆盖同名文件，不用交互式选择
        // -t 01:00:00"; // 录制1h自动停止
        ffmpeg.StartInfo.UseShellExecute = false;
        ffmpeg.StartInfo.CreateNoWindow = false; //关键①，true不执行
        ffmpeg.Start();
        ffmpeg.WaitForExit();
    }

    // Windows文件名不允许文件名含（\ / : * ? " < > |）
    // 替换为 下划线 _
    public static string MakeFileNameSafe(string name)
    {
        // 常见所有系统的不合法字符
        //char[] invalidChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' }; //跨平台写法
        char[] invalidChars = Path.GetInvalidFileNameChars(); //Windows写法

        // 过滤
        foreach (char c in invalidChars)
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
