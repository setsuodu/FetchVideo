// Services/ScheduleConfigService.cs
using FetchVideo.Data;
using FetchVideo.Models;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Services;

public class ScheduleConfigService
{
    private readonly AppDbContext _db;

    // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
    // 改成 public！！！不然外面拿不到
    public static readonly List<string> DefaultTimes = new() { "08:00", "12:00", "18:00", "22:00" }; //UTC+8，Docker是UTC时间
    // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←

    public ScheduleConfigService(AppDbContext db) => _db = db;

    private const string GlobalKey = "GlobalTriggerTimes";

    public async Task<List<string>> GetAsync()
    {
        var entity = await _db.Set<ScheduleConfig>()
            .FirstOrDefaultAsync(x => x.Key == GlobalKey);

        return entity?.TriggerTimes?.Any() == true
            ? entity.TriggerTimes
            : DefaultTimes;
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