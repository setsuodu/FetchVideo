using FetchVideo.Data;
using FetchVideo.Models;
using FetchVideo.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Controllers;

// Controllers/LinkItemController.cs
[ApiController]
[Route("api/[controller]")]
public class LinkItemController : ControllerBase
{
    private readonly ISharedService _sharedService;
    private readonly AppDbContext _context;

    public LinkItemController(ISharedService sharedService)
    {
        _sharedService = sharedService;
        _context = sharedService._context;
    }

    // GET: api/LinkItem/get_rooms
    [HttpGet("get_rooms")]
    public async Task<IActionResult> GetLinkItems()
    {
        return Ok(await _sharedService.GetLinkItems_Pro());
    }

    // POST: api/LinkItem/set_rooms
    [HttpPost("set_rooms")]
    public async Task<IActionResult> AddLinkItems([FromBody] List<LinkItem> linkList)
    {
        if (linkList == null || !linkList.Any())
            return BadRequest("列表不能为空");

        var urls = linkList.Select(l => l.RoomId).ToList(); //先查询

        var existingUrls = await _context.LinkItems
            .Where(l => urls.Contains(l.RoomId))
            .Select(l => l.RoomId)
            .ToListAsync();

        // 过滤掉已存在的
        var newItems = linkList.Where(l => !existingUrls.Contains(l.RoomId)).ToList();

        if (newItems.Any())
        {
            _context.LinkItems.AddRange(newItems);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "批量添加成功", count = linkList.Count });
    }

    // 开始下载，检查是否添加 SQL
    // POST: api/LinkItem/add_live_room
    [HttpPost("add_live_room")]
    public async Task<IActionResult> AddLiveRoom([FromBody] LinkItem link)
    {
        if (link == null)
            return BadRequest("link不能为空");

        bool added = await _sharedService.AddNewLiveRoom(link);
        return added ? Ok(new { message = "添加成功", id = link.Id })
            : Conflict(new { message = "直播间已存在" });
    }


    // 订阅/取消订阅
    [HttpPost("toggle_subscribe")]
    public async Task<IActionResult> ToggleSubscribe([FromBody] SubscribeRequest request)
    {
        if (request == null || !request.Id.HasValue)
            return BadRequest("必须提供有效的 Id");

        var item = await _context.LinkItems
            .FirstOrDefaultAsync(l => l.Id == request.Id.Value);

        if (item == null)
            return NotFound("未找到对应的直播间");

        // 切换订阅状态
        item.IsSubscribed = !item.IsSubscribed;

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = item.IsSubscribed ? "订阅成功" : "取消订阅成功",
            id = item.Id,
            isSubscribed = item.IsSubscribed
        });
    }
}
