using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace FetchVideo.Utils;

public class Shared
{
    public const string BILI_VIDEO = "https://www.bilibili.com/video/";
    public const string BILI_LIVE = "https://live.bilibili.com/"; // 直播间前缀
    public const string BILI_PLAYER = "https://api.bilibili.com/x/player/";
    public const string BILI_SPACE = "https://api.bilibili.com/x/space/";
    public const string BILI_INTERFACE = "https://api.bilibili.com/x/web-interface/";
    public const string BILI_ROOM = "https://api.live.bilibili.com/room/v1/Room/";
    public const string BILI_PK = "https://api.live.bilibili.com/xlive/web-room/v1/index/getInfoByRoom";

    public static string GetBvId(string url)
    {
        // 正则表达式 (Regex) 提取BV
        string pattern = @"(BV[a-zA-Z0-9]+)/?";
        Match match = Regex.Match(url, pattern);
        string bvId = match.Groups[0].Value.TrimEnd('/'); // 使用 TrimEnd('/') 确保去除末尾可选的斜杠
        Console.WriteLine($"提取到的 BV 号码: **{bvId}**");
        return bvId;
    }
    public static string GetRoomId(string url)
    {
        // 正则表达式 (Regex) 提取房间号
        string pattern = @"live\.bilibili\.com/(\d+)";
        Match match = Regex.Match(url, pattern);
        string roomId = match.Groups[1].Value;
        Console.WriteLine($"提取到的 房间号: **{roomId}**");
        return roomId;
    }

    // Windows 保留设备名（不区分大小写，不能作为文件名，即便加了扩展名也不行）
    private static readonly string[] ReservedNames =
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1","COM2","COM3","COM4","COM5","COM6","COM7","COM8","COM9",
        "LPT1","LPT2","LPT3","LPT4","LPT5","LPT6","LPT7","LPT8","LPT9"
    };

    /// <summary>
    /// 生成 Windows + Linux 双兼容的安全文件名。
    /// 不依赖 Path.GetInvalidFileNameChars()（该方法是运行时相关的，
    /// 在 Linux 上跑只会挡掉 "/" 和 "\0"，Windows 的 : * ? " &lt; &gt; | 不会被拦下）。
    /// 同时清理零宽字符 / 双向文本控制符 / 变体选择符 / 组合符号炸弹等
    /// 肉眼看不见但会导致乱码、隐藏扩展名、脚本比对失败的 Unicode "地雷"。
    /// </summary>
    /// <param name="name">原始文件名（不含目录）</param>
    /// <param name="maxBytes">UTF-8 字节数上限，默认 200（Linux ext4 单文件名硬限 255 字节）</param>
    public static string MakeFileNameSafe(string name, int maxBytes = 200)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "untitled";

        // 1. Unicode 归一化，避免同形异义字符/多余组合符导致的诡异结果
        name = name.Normalize(NormalizationForm.FormC);

        // 2. Windows 明确禁止的字符： \ / : * ? " < > |
        //    （Linux 只硬性禁止 / 和 \0，但这些字符留着挪到 Windows 用就炸，所以两边都清）
        char[] hardInvalid = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
        foreach (char c in hardInvalid)
        {
            name = name.Replace(c, '_');
        }

        // 3. 控制字符 (0x00-0x1F, 0x7F)：可能导致终端/脚本解析错误
        name = Regex.Replace(name, @"[\x00-\x1F\x7F]", "_");

        // 4. 零宽字符 & BOM：肉眼不可见，会让"看起来一样"的文件名实际不同
        //    ZWSP U+200B, ZWNJ U+200C, ZWJ U+200D, BOM U+FEFF
        name = Regex.Replace(name, @"[\u200B-\u200D\uFEFF]", "");

        // 5. 双向文本控制符：可用来伪造文件名/隐藏扩展名（经典的 U+202E RTL override 攻击）
        name = Regex.Replace(name, @"[\u202A-\u202E\u2066-\u2069]", "");

        // 6. 变体选择符：会让普通字符变成 emoji 样式(如 👍 U+FE0F)，BMP 内 + 补充平面 VS17-256
        name = Regex.Replace(name, @"[\uFE00-\uFE0F]", "");
        name = Regex.Replace(name, @"\uDB40[\uDD00-\uDDEF]", ""); // U+E0100–E01EF 代理对

        // 7. 组合符号炸弹：连续多个变音符号(Mn/Me/Mc)只保留 1 个，防止渲染异常/长度失控
        name = Regex.Replace(name, @"(\p{Mn}|\p{Me}|\p{Mc}){2,}", "$1");

        // 8. 白名单：只保留 字母(\p{L}，含中日韩) / 数字(\p{N}) / 空格 / 常见分隔标点
        //    其余（颜文字、装饰符号、emoji 等）统一替换为 _
        name = Regex.Replace(name, @"[^\p{L}\p{N}\s\-_\(\)\[\]【】\.,!?]", "_");

        // 9. 折叠连续下划线/空格
        name = Regex.Replace(name, @"[_\s]{2,}", "_");

        // 10. Windows 不允许文件名以空格/点/下划线堆结尾那么难看
        name = name.Trim();
        name = name.TrimEnd('.', ' ', '_');

        if (string.IsNullOrWhiteSpace(name))
            name = "untitled";

        // 11. Windows 保留设备名处理（CON.mp4 这种也算保留名，一样要加前缀）
        string baseName = Path.GetFileNameWithoutExtension(name);
        string ext = Path.GetExtension(name);
        if (Array.IndexOf(ReservedNames, baseName.ToUpperInvariant()) >= 0)
        {
            name = "_" + baseName + ext;
        }

        // 12. 按 UTF-8 字节数截断（不是字符数！中文一个字 3 字节，
        //     Linux ext4 单文件名硬限 255 字节，超了会直接报错）
        name = TruncateByUtf8Bytes(name, maxBytes);

        return name;
    }

    /// <summary>
    /// 按 UTF-8 字节数安全截断字符串，不会切断多字节字符导致乱码。
    /// </summary>
    private static string TruncateByUtf8Bytes(string name, int maxBytes)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(name);
        if (bytes.Length <= maxBytes) return name;

        int len = maxBytes;
        // 回退到字符边界：UTF-8 续字节的高两位是 10xxxxxx
        while (len > 0 && (bytes[len] & 0xC0) == 0x80) len--;

        return Encoding.UTF8.GetString(bytes, 0, len);
    }

    // "又长大了是时候夺回属于我的一切了 - 沐汐BB - 哔哩哔哩直播，二次元弹幕直播平台"
    // 比封面可爱一点点 - -阿少少Ash - 哔哩哔哩直播，二次元弹幕直播平台
    public static string GetMiddleText(string input)
    {
        Console.WriteLine($"GetMiddleText: {input}");
        var match = Regex.Match(input, @" - \s*(.*?)\s* - ");
        return match.Success ? match.Groups[1].Value.Trim() : null;
    }

    // 确保不是404
    // 复用单个 HttpClient 实例（线程安全）
    private static readonly HttpClient _client = new HttpClient(new HttpClientHandler
    {
        // 自动处理重定向（默认就是 true）
        AllowAutoRedirect = true,
        MaxAutomaticRedirections = 10
    })
    {
        Timeout = TimeSpan.FromSeconds(30)  // 可根据需要调整超时时间
    };

    /// <summary>
    /// 按顺序检查 urls 数组中的 URL，返回第一个有效的 URL。
    /// 如果全部无效，返回 null。
    /// </summary>
    /// <param name="urls">要检查的 URL 数组</param>
    /// <param name="cancellationToken">可选的取消令牌</param>
    /// <returns>第一个有效的 URL，或 null</returns>
    public static async Task<string?> GetFirstValidUrlAsync(string[] urls, CancellationToken cancellationToken = default)
    {
        if (urls == null) throw new ArgumentNullException(nameof(urls));

        foreach (var url in urls)
        {
            if (string.IsNullOrWhiteSpace(url)) continue;

            bool isValid = await IsUrlValidAsync(url.Trim(), cancellationToken);
            if (isValid)
            {
                return url.Trim();  // 直接返回有效的原始 URL
            }
        }

        return null; // 全部无效
    }
    /// <summary>
    /// 检查单个 URL 是否有效（优先 HEAD，失败时回退 GET）
    /// </summary>
    private static async Task<bool> IsUrlValidAsync(string url, CancellationToken ct)
    {
        try
        {
            // 优先尝试 HEAD 请求（只取响应头）
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);

            // 关键点：必须模拟 B 站官方环境
            headRequest.Headers.Referrer = new Uri("https://live.bilibili.com/");
            headRequest.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

            using var headResponse = await _client.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            Console.WriteLine($"检查单个 URL 是否有效: {url}👉{headResponse.StatusCode}");

            if (headResponse.IsSuccessStatusCode)
            {
                return true;
            }

            // 如果 HEAD 返回 404，直接判定无效
            if (headResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }

            // 如果 HEAD 返回 405（Method Not Allowed）或其他非成功码，尝试 GET
            if (headResponse.StatusCode == System.Net.HttpStatusCode.MethodNotAllowed ||
                !headResponse.IsSuccessStatusCode)
            {
                using var getResponse = await _client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

                if (getResponse.IsSuccessStatusCode)
                {
                    return true;
                }

                if (getResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return false;
                }
            }

            return false;
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            // 超时
            Console.WriteLine($"请求超时: {url}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            // 网络错误、DNS 失败等
            Console.WriteLine($"请求失败 ({url}): {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"未知错误 ({url}): {ex.Message}");
            return false;
        }
    }

    // 短链👉长链
    public static async Task<string> Curl_I(string shortUrl)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 ...");

        // 1. 构造请求对象
        var request = new HttpRequestMessage(HttpMethod.Head, shortUrl);
        // 2. 发送（等价于 curl -I）
        var response = await client.SendAsync(request);
        var location = response.Headers.Location?.ToString() ?? response.RequestMessage.RequestUri.ToString();

        location = CleanUrl(location);
        location = location.Replace("/h5/", "/");
        return location;
    }

    // 裁掉 "/h5" 和 "?及后面多余的"
    public static string CleanUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return url;

        Uri uri;
        try
        {
            uri = new Uri(url);
        }
        catch
        {
            return url; // 非法 URL 原样返回
        }

        // 1. 处理 /h5：裁掉 /h5 及之后部分
        string path = uri.AbsolutePath;
        int h5Index = path.IndexOf("/h5", StringComparison.OrdinalIgnoreCase);
        if (h5Index >= 0)
        {
            path = path.Substring(0, h5Index); // 裁掉 /h5 及之后
        }

        // 2. 保留一个 ?，去掉所有查询参数
        string baseUrl = uri.Scheme + "://" + uri.Authority + path;

        return baseUrl;
    }

    /// <summary>
    /// 转换 UTC+8 配置时间列表 到 服务器本地时间，并打印详细的调试信息。
    /// 仅支持 HH:mm 格式的输入 (e.g., "00:00", "12:00")。
    /// 修正 ArgumentException 错误：通过将 Kind 设置为 Unspecified 解决。
    /// </summary>
    /// <param name="utc8Times">输入的 UTC+8 时间字符串列表，如 "00:00", "12:00"。</param>
    /// <returns>转换后的服务器本地时间字符串列表 (HH:mm)，并按时间排序。</returns>
    public static List<string> ConvertUtc8ConfigToLocal(List<string> utc8Times)
    {
        if (utc8Times == null) throw new ArgumentNullException(nameof(utc8Times));

        // 1. 关键：手动创建 UTC+8 时区，保证跨平台和独立性。
        TimeZoneInfo utc8Zone = TimeZoneInfo.CreateCustomTimeZone(
            "Custom UTC+8",
            TimeSpan.FromHours(8),
            "UTC+8",
            "UTC+8");

        // 2. 获取服务器的本地时区。
        TimeZoneInfo localZone = TimeZoneInfo.Local;
        TimeSpan localOffset = localZone.BaseUtcOffset;

        Console.WriteLine("--- 时区转换信息 ---");
        Console.WriteLine($"🚀 服务器本地时区 (Local Zone): {localZone.Id}");
        Console.WriteLine($"🌐 服务器 UTC 偏移量: UTC{(localOffset >= TimeSpan.Zero ? "+" : "")}{localOffset:hh\\:mm}");
        Console.WriteLine($"⏱️ 原始配置时区 (UTC+8): UTC+08:00");
        Console.WriteLine($"---------------------");
        Console.WriteLine($"原始配置 (UTC+8): {string.Join(", ", utc8Times)}");
        Console.WriteLine($"---------------------");

        var localList = new List<string>();

        // 获取一个固定的基准日期（服务器本地的今天午夜 00:00:00）
        DateTime todayDate = DateTime.Today;

        foreach (var timeStr in utc8Times)
        {
            string input = timeStr?.Trim() ?? string.Empty;

            // 3. 解析输入的时间字符串
            if (!TimeSpan.TryParseExact(input,
                new[] { @"hh\:mm" },
                CultureInfo.InvariantCulture,
                TimeSpanStyles.None,
                out TimeSpan ts))
            {
                Console.WriteLine($"\n⚠️ 警告：时间格式无法识别，原样返回 → {timeStr}");
                localList.Add(timeStr);
                continue;
            }

            // 4. 构造 UTC+8 的完整时间点，并【修正 Kind 属性】
            DateTime utc8DateTimeUnspecified = todayDate.Add(ts);

            // 关键修正：将 Kind 强制设置为 Unspecified，以满足 TimeZoneInfo.ConvertTimeToUtc 的要求。
            DateTime utc8DateTime = DateTime.SpecifyKind(utc8DateTimeUnspecified, DateTimeKind.Unspecified);

            // 5. 核心转换路径：UTC+8 → UTC → 服务器本地时间

            // 步骤 A: 将 UTC+8 时间（Kind=Unspecified）转换为 UTC 时间。
            // 现在可以成功执行，因为它不再认为源 DateTime 是 TimeZoneInfo.Local 的时间。
            DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(utc8DateTime, utc8Zone);

            // 步骤 B: 将 UTC 时间转换为服务器本地时间。
            DateTime localDateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, localZone);

            string result = localDateTime.ToString(@"HH\:mm");

            // 详细打印转换过程
            Console.WriteLine($"\n⚙️ 转换过程 for '{input}'");
            Console.WriteLine($"   1. 配置时间 (UTC+8)  : {utc8DateTime:yyyy-MM-dd HH:mm:ss} (Kind: {utc8DateTime.Kind})");
            Console.WriteLine($"   2. 转换为 UTC        : {utcDateTime:yyyy-MM-dd HH:mm:ss} (Kind: {utcDateTime.Kind})");
            Console.WriteLine($"   3. 转换为本地时间    : {localDateTime:yyyy-MM-dd HH:mm:ss} (Kind: {localDateTime.Kind})");
            Console.WriteLine($"   => 最终结果 (HH:mm)  : {result}");

            localList.Add(result);
        }

        // 6. 按时间排序后返回
        var sortedList = localList
            .OrderBy(t => TimeSpan.TryParse(t, out TimeSpan ts) ? ts : TimeSpan.MaxValue)
            .ToList();

        Console.WriteLine($"\n---------------------");
        Console.WriteLine($"✅ 最终本地时间列表 (已排序): {string.Join(", ", sortedList)}");
        Console.WriteLine($"---------------------");

        return sortedList;
    }
    /// <summary>
    /// 转换 服务器本地时区 的配置时间列表 到 UTC+8 时区配置。
    /// 兼容 Windows / Linux / macOS / Docker / 任何时区环境。
    /// 仅支持 HH:mm 格式的输入 (e.g., "00:00", "12:00")。
    /// </summary>
    /// <param name="localTimes">输入的服务器本地时间字符串列表，如 "00:00", "12:00"。</param>
    /// <returns>转换后的 UTC+8 时间字符串列表 (HH:mm)，并按时间排序。</returns>
    public static List<string> ConvertLocalConfigToUtc8(List<string> localTimes)
    {
        if (localTimes == null) throw new ArgumentNullException(nameof(localTimes));

        // 1. 关键：手动创建 UTC+8 时区
        TimeZoneInfo utc8Zone = TimeZoneInfo.CreateCustomTimeZone(
            "Custom UTC+8",
            TimeSpan.FromHours(8),
            "UTC+8",
            "UTC+8");

        // 2. 获取服务器的本地时区。
        TimeZoneInfo localZone = TimeZoneInfo.Local;
        TimeSpan localOffset = localZone.BaseUtcOffset;

        Console.WriteLine("--- 时区转换信息 ---");
        Console.WriteLine($"🚀 原始配置时区 (Local Zone): {localZone.Id}");
        Console.WriteLine($"🌐 服务器 UTC 偏移量: UTC{(localOffset >= TimeSpan.Zero ? "+" : "")}{localOffset:hh\\:mm}");
        Console.WriteLine($"⏱️ 目标配置时区 (UTC+8): UTC+08:00");
        Console.WriteLine($"---------------------");
        Console.WriteLine($"原始配置 (本地): {string.Join(", ", localTimes)}");
        Console.WriteLine($"---------------------");

        var utc8List = new List<string>();

        // 获取一个固定的基准日期（服务器本地的今天午夜 00:00:00）
        DateTime todayDate = DateTime.Today;

        foreach (var timeStr in localTimes)
        {
            string input = timeStr?.Trim() ?? string.Empty;

            // 3. 解析输入的时间字符串 (HH:mm 格式)
            if (!TimeSpan.TryParseExact(input,
                new[] { @"hh\:mm" },
                CultureInfo.InvariantCulture,
                TimeSpanStyles.None,
                out TimeSpan ts))
            {
                Console.WriteLine($"\n⚠️ 警告：时间格式无法识别，原样返回 → {timeStr}");
                utc8List.Add(timeStr);
                continue;
            }

            // 4. 构造本地时间点，并明确指定 Kind 为 Local（与 DateTime.Today 一致）
            // 这里可以直接使用 DateTime.Today 得到的 Local Kind，
            // 因为我们将使用 TimeZoneInfo.ConvertTimeToUtc(localDateTime, localZone) 进行转换。
            DateTime localDateTimeWithKind = todayDate.Add(ts);

            // 确保 Kind 是 Local (虽然 DateTime.Today 默认就是 Local，但明确指定更安全)
            DateTime localDateTime = DateTime.SpecifyKind(localDateTimeWithKind, DateTimeKind.Local);

            // 💡 注意：如果您需要在 Docker/Linux 环境中绝对依赖 Unspecified/UTC，
            // 最好使用 DateTimeOffset 来避免 Local Kind 的复杂性，
            // 但为了保持与您现有代码风格的一致性，我们遵循 TimeZoneInfo 的标准做法：
            // Local Kind 必须配合 TimeZoneInfo.Local 使用。

            // 5. 核心转换路径：本地时区 → UTC → UTC+8

            // 步骤 A: 将本地时间转换为 UTC 时间。
            // 因为 localDateTime.Kind 是 Local，这里必须使用 TimeZoneInfo.Local
            DateTime utcDateTime = TimeZoneInfo.ConvertTimeToUtc(localDateTime, localZone);

            // 步骤 B: 将 UTC 时间转换为 UTC+8 时间。
            DateTime utc8DateTime = TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, utc8Zone);

            string result = utc8DateTime.ToString(@"HH\:mm");

            // 详细打印转换过程
            Console.WriteLine($"\n⚙️ 转换过程 for '{input}'");
            Console.WriteLine($"   1. 配置时间 (本地) : {localDateTime:yyyy-MM-dd HH:mm:ss} (Kind: {localDateTime.Kind})");
            Console.WriteLine($"   2. 转换为 UTC      : {utcDateTime:yyyy-MM-dd HH:mm:ss} (Kind: {utcDateTime.Kind})");
            Console.WriteLine($"   3. 转换为 UTC+8    : {utc8DateTime:yyyy-MM-dd HH:mm:ss} (Kind: {utc8DateTime.Kind})");
            Console.WriteLine($"   => 最终结果 (HH:mm): {result}");

            utc8List.Add(result);
        }

        // 6. 按时间排序后返回
        var sortedList = utc8List
            .OrderBy(t => TimeSpan.TryParse(t, out TimeSpan ts) ? ts : TimeSpan.MaxValue)
            .ToList();

        Console.WriteLine($"\n---------------------");
        Console.WriteLine($"✅ 最终 UTC+8 时间列表 (已排序): {string.Join(", ", sortedList)}");
        Console.WriteLine($"---------------------");

        return sortedList;
    }
    // 排序计划列表
    public static List<string> ScheduleConfigSort(List<string> times)
    {
        var list = times.OrderBy(x => x).ToList();
        return list;
    }

    // 客户端用 👇
    public static void MergeAudioVideo(string videoPath, string audioPath, string outputPath)
    {
        using (var ffmpeg = new Process())
        {
            ffmpeg.StartInfo.FileName = "D:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"; // ffmpeg.exe 路径
            ffmpeg.StartInfo.Arguments = $"-i \"{videoPath}\" -i \"{audioPath}\" -c copy \"{outputPath}\" -y";
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.CreateNoWindow = true;
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            // 当 using 块结束时，process.Dispose() 会被自动调用
        }

        // 只有当 FFmpeg 进程退出后，代码才会执行到这里
        //删除源视频的代码 // <-- 这里的代码
        File.Delete(videoPath);
        File.Delete(audioPath);
    }
    // 客户端用 👇
    public static void M3U8toMP4(string room_id, string m3u8Url, string outputPath)
    {
        using (var ffmpeg = new Process())
        {
            ffmpeg.StartInfo.FileName = "D:\\Program Files\\ffmpeg\\bin\\ffmpeg.exe"; // ffmpeg.exe 路径
            ffmpeg.StartInfo.Arguments = $"-headers \"Referer: {BILI_LIVE}{room_id}\r\nUser-Agent: Mozilla/5.0\" -i \"{m3u8Url}\" -c copy \"{outputPath}\" -y"; // -y 直接覆盖同名文件，不用交互式选择
            ffmpeg.StartInfo.UseShellExecute = false;
            ffmpeg.StartInfo.CreateNoWindow = false; //关键①，true不执行
            ffmpeg.Start();
            ffmpeg.WaitForExit();

            // 当 using 块结束时，process.Dispose() 会被自动调用
        }
    }

    // 获取网页
    public static async Task<string> GetHTML(string url)
    {
        Console.WriteLine($"获取网页: {url}");

        using (var http = new HttpClient())
        {
            // 一些 headers 模拟浏览器访问
            http.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                "AppleWebKit/537.36 (KHTML, like Gecko) " +
                "Chrome/122.0.0.0 Safari/537.36");

            string html = await http.GetStringAsync(url);
            //Console.WriteLine(html);
            return html;
        }
    }
}