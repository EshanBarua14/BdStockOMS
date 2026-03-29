using System.ComponentModel.DataAnnotations;
namespace BdStockOMS.API.Models;
public class BOGroup
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public int BrokerageHouseId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
    public virtual ICollection<BOGroupMember> Members { get; set; } = new List<BOGroupMember>();
}
public class BOGroupMember
{
    public int Id { get; set; }
    public int BOGroupId { get; set; }
    public int UserId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public virtual BOGroup BOGroup { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
