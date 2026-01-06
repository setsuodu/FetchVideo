using FetchVideo.Controllers;
using FetchVideo.Data;
using FetchVideo.Models;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Services;

public class SharedService : ISharedService
{
    public AppDbContext _context { get; private set; }

    public string _downloadPath { get; private set; }

    public FFmpegManager _ffManager { get; private set; }

    public SharedService(AppDbContext context, IConfiguration configuration, FFmpegManager manager)
    {
        _context = context;
        // 如果配置中没找到，就用 "/app/downloads";
        _downloadPath = configuration["DownloadPath"] ?? "/app/downloads";
        _ffManager = manager;
    }

    public async Task<bool> AddNewLiveRoom(LinkItem link)
    {
        // 这里放业务逻辑（如数据库查询、计算等）
        var existingUrl = await _context.LinkItems
            .Where(x => (link.Name == x.Name || link.Url == x.Url))
            .FirstOrDefaultAsync();
        if (existingUrl != null)
        {
            Console.WriteLine("AddNewLiveRoom: 已存在");
            return false;
        }
        Console.WriteLine($"AddNewLiveRoom: {link.Name}:{link.Url}");
        _context.LinkItems.Add(link);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<LinkItem>> GetLinkItems()
    {
        // 这里放业务逻辑（如数据库查询、计算等）
        return await _context.LinkItems
            .OrderBy(x => x.Id)
            .ToListAsync();
    }

    public async Task<List<LinkItemDisplayDto>> GetLinkItems_Pro()
    {
        // 获取当前正在运行的 FFmpeg 任务
        List<FFmpegTaskDto> runningTasks = _ffManager.GetRunningTasks();

        // 获取数据库中所有直播间项
        List<LinkItem> subsList = await GetLinkItems();

#if DEBUG
        LinkItemSQL.DebugSort(subsList);
#endif

        // 将正在录制的 UpName 提取为 HashSet，快速匹配（忽略大小写和空格）
        var runningNames = runningTasks
            .Where(t => !string.IsNullOrWhiteSpace(t.UpName))
            .Select(t => t.UpName.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        // 转换为 DisplayDto
        var displayList = subsList.Select(item =>
        {
            string trimmedName = item.Name?.Trim() ?? string.Empty;
            bool isRecording = !string.IsNullOrEmpty(trimmedName) && runningNames.Contains(trimmedName);

            DateTime? startTime = null;
            int durationSeconds = 0;
            string status = "空闲";

            if (isRecording)
            {
                status = "录制中";

                // 找到对应的任务，取出开始时间和时长
                var task = runningTasks.FirstOrDefault(t =>
                    string.Equals(t.UpName?.Trim(), trimmedName, StringComparison.OrdinalIgnoreCase));

                if (task != null)
                {
                    startTime = task.StartTime;                    // 假设 FFmpegTaskDto 有 DateTime? StartTime
                    durationSeconds = task.Duration * 60;          // 假设有 int Duration（秒）
                                                                   // 如果你的字段名不同，请对应修改（如 task.RecordingDuration）
                }
            }

            return new LinkItemDisplayDto
            {
                Id = item.Id,
                Name = item.Name ?? string.Empty,
                Url = item.Url ?? string.Empty,
                IsSubscribed = item.IsSubscribed,
                CurrentStatus = status,
                StartTime = startTime,
                DurationSeconds = durationSeconds
            };
        }).ToList();

        return displayList;
    }
}