using Microsoft.AspNetCore.Mvc;
using FetchVideo.Models;
using FetchVideo.Data;
using Microsoft.EntityFrameworkCore;

namespace FetchVideo.Controllers;

// Controllers/LinkItemController.cs
[ApiController]
[Route("api/[controller]")]
public class LinkItemController : ControllerBase
{
    private readonly AppDbContext _context;

    public LinkItemController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/LinkItem/get_rooms
    [HttpGet("get_rooms")]
    public async Task<IActionResult> GetLinkItems()
    {
        var existingUrls = await _context.LinkItems
            .Select(l => l.Url)
            .ToListAsync();

        return Ok(existingUrls);
    }

    // POST: api/LinkItem/set_rooms
    [HttpPost("set_rooms")]
    public async Task<IActionResult> AddLinkItems([FromBody] List<LinkItem> linkList)
    {
        if (linkList == null || !linkList.Any())
            return BadRequest("列表不能为空");

        var urls = linkList.Select(l => l.Url).ToList(); //先查询

        var existingUrls = await _context.LinkItems
            .Where(l => urls.Contains(l.Url))
            .Select(l => l.Url)
            .ToListAsync();

        // 过滤掉已存在的
        var newItems = linkList.Where(l => !existingUrls.Contains(l.Url)).ToList();

        if (newItems.Any())
        {
            _context.LinkItems.AddRange(newItems);
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "批量添加成功", count = linkList.Count });
    }
}
