using FetchVideo.Models;

namespace FetchVideo.Services;

public interface ISharedService
{
    public string _downloadPath { get; }

    Task<bool> AddNewLiveRoom(LinkItem link);

    // 其他共享方法
}
