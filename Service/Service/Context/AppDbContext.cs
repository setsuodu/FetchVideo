// Data/AppDbContext.cs
using FetchVideo.Models;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
    // 全局唯一配置表（永远只有一条记录）
    public DbSet<ScheduleConfig> ScheduleConfigs => Set<ScheduleConfig>();
    // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 保证 Key 唯一（其实我们永远只用 GlobalTriggerTimes 这一个 key）
        modelBuilder.Entity<ScheduleConfig>()
            .HasIndex(x => x.Key)
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }
}