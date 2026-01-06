// Services/ScheduleConfigService.cs
using FetchVideo.Data;
using FetchVideo.Models;
using FetchVideo.Utils;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Services;

public class ScheduleConfigService
{
    private readonly AppDbContext _db;

    // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
    // 改成 public！！！不然外面拿不到
    // 这里配的是北京时间UTC-8（阅读友好设计）
    // 宿主机跑的时间不一定：Docker是UTC-0，UNIX是UTC-8或其他，使之兼容
    private static readonly List<string> DefaultTimes = new() { "00:00", "08:00", "12:00", "18:00", "20:00", "22:00" }; //UTC+8，Docker是UTC时间
    public static List<string> GetHostTimes()
    {
        // 先读取宿主机时区
        // 把配置的UTC-8时间，转成宿主机格式
        return Shared.ConvertUtc8ConfigToLocal(DefaultTimes);
    }
    // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←

    public ScheduleConfigService(AppDbContext db) => _db = db;

    private const string GlobalKey = "GlobalTriggerTimes";

    public async Task<List<string>> GetAsync()
    {
        var entity = await _db.Set<ScheduleConfig>()
            .FirstOrDefaultAsync(x => x.Key == GlobalKey);

        return entity?.TriggerTimes?.Any() == true
            ? entity.TriggerTimes
            : GetHostTimes();
    }

    public async Task SaveAsync(List<string> times)
    {
        if (times == null) throw new ArgumentNullException(nameof(times));

        var entity = await _db.Set<ScheduleConfig>()
            .FirstOrDefaultAsync(x => x.Key == GlobalKey) ?? new ScheduleConfig { Key = GlobalKey };

        if (entity.Id == 0) _db.Set<ScheduleConfig>().Add(entity);

        entity.TriggerTimes = times;
        await _db.SaveChangesAsync();
    }
}