using FetchVideo.Controllers;
using FetchVideo.Data;
using FetchVideo.Models;

namespace FetchVideo.Services;

public interface ISharedService
{
    public AppDbContext _context { get; }

    public string _downloadPath { get; }

    public FFmpegManager _ffManager { get; }

    Task<bool> AddNewLiveRoom(LinkItem link);

    Task<List<LinkItem>> GetLinkItems();
    Task<List<LinkItemDisplayDto>> GetLinkItems_Pro();

    // 其他共享方法
}
