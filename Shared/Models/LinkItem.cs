using System.ComponentModel.DataAnnotations;

namespace FetchVideo.Models;

public class LinkItem
{
    [Key]  // 主键，通常自增
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Url { get; set; } = string.Empty;
    
    public bool Active { get; set; } = true;  // 默认激活
}