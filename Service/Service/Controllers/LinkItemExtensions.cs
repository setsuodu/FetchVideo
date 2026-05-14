using FetchVideo.Data;           // AppDbContext 所在
using FetchVideo.Models;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Controllers;

public static class LinkItemExtensions
{
    /// <summary>
    /// 录制成功后更新最后录制时间
    /// </summary>
    public static async Task UpdateLastRecordedAsync(this AppDbContext context, string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId)) return;

        var item = await context.LinkItems
            .FirstOrDefaultAsync(x => x.RoomId == roomId);

        if (item != null)
        {
            item.LastRecordedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}