using FetchVideo.Controllers;
using FetchVideo.Data;
using FetchVideo.Models;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Services;

public class SharedService : ISharedService
{
    private readonly AppDbContext _context;  // 示例依赖

    public string _downloadPath { get; private set; }

    public FFmpegManager _ffManager { get; private set; }

    public SharedService(AppDbContext context, IConfiguration configuration, FFmpegManager manager)
    {
        _context = context;
        _downloadPath = configuration["DownloadPath"] ?? "/app/downloads";
        _ffManager = manager;
    }

    public async Task<bool> AddNewLiveRoom(LinkItem link)
    {
        // 这里放业务逻辑（如数据库查询、计算等）
        var existingUrl = await _context.LinkItems.Where(x => link.Url == x.Url).FirstOrDefaultAsync();
        if (existingUrl != null)
        {
            return false;
        }
        _context.LinkItems.Add(link);
        await _context.SaveChangesAsync();
        return true;
    }
}