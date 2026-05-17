using FetchVideo.Models;
using FetchVideo.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/maintenance/linkitems")]
public class LinkItemMaintenanceController : ControllerBase
{
    private readonly ISharedService _sharedService;

    public LinkItemMaintenanceController(ISharedService sharedService)
    {
        _sharedService = sharedService;
    }

    [HttpGet("default")]
    public IActionResult DefaultList()
    {
        var list = LinkItemSQL.DefaultList();
        return Ok(new { success = true, count = list.Count, source = "memory/json" });
    }

    [HttpGet("load")]
    public IActionResult LoadFromJson()
    {
        var list = LinkItemSQL.Load();
        return Ok(new { success = true, count = list.Count, message = "已从JSON加载" });
    }

    // ★★★★★ 你真正想要的：把当前数据库里的最新数据备份到JSON ★★★★★
    [HttpGet("save")]
    public async Task<IActionResult> SaveCurrentToJson()
    {
        try
        {
            // 这里关键：从数据库/SQL里读取最新完整列表（而不是只读JSON）
            var currentList = await _sharedService.GetLinkItems();   // ← 你需要实现这个

            LinkItemSQL.Save(currentList);   // 写入JSON

            return Ok(new
            {
                success = true,
                count = currentList.Count,
                message = $"✅ 已把数据库最新 {currentList.Count} 条数据备份到 JSON"
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }
}