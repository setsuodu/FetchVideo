// Models/ScheduleConfig.cs   （名字都改得更贴切）
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace FetchVideo.Models;

[Index(nameof(Key), IsUnique = true)]  // 保证全局只有一条
public class ScheduleConfig
{
    [Key]
    public int Id { get; set; }                          // EF Core 必须要有主键，留着就行

    public string Key { get; set; } = "GlobalTriggerTimes";  // 固定写死这一个 key

    public string TriggerTimesJson { get; set; } = "[]";     // 唯一真正有用的字段

    // 方便你代码里直接读写 List<string>
    [NotMapped, JsonIgnore]
    public List<string> TriggerTimes
    {
        get => string.IsNullOrWhiteSpace(TriggerTimesJson)
            ? new List<string>()
            : System.Text.Json.JsonSerializer.Deserialize<List<string>>(TriggerTimesJson)!;
        set => TriggerTimesJson = System.Text.Json.JsonSerializer.Serialize(value);
    }
}