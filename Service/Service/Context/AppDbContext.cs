// Data/AppDbContext.cs
using FetchVideo.Models;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Data;

// 一个csproj →对应→ 一个数据库 →对应→ 一个 DbContext
public class AppDbContext : DbContext
{
    // 全局唯一配置表（永远只有一条记录）
    public DbSet<ScheduleConfig> ScheduleConfigs => Set<ScheduleConfig>();
    public DbSet<LinkItem> LinkItems { get; set; }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // 保证 Key 唯一（其实我们永远只用 GlobalTriggerTimes 这一个 key）
        modelBuilder.Entity<ScheduleConfig>()
            .HasIndex(x => x.Key)
            .IsUnique();

        //modelBuilder.Entity<LinkItem>().ToTable("LinkItems");  // 表名
        modelBuilder.Entity<LinkItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Duration).HasDefaultValue(2);
            entity.Property(e => e.IsSubscribed).HasDefaultValue(false);
        });

        base.OnModelCreating(modelBuilder);
    }
}