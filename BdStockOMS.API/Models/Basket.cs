using System.ComponentModel.DataAnnotations;
namespace BdStockOMS.API.Models;
public class Basket
{
    public int Id { get; set; }
    [Required, MaxLength(100)] public string Name { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public int BrokerageHouseId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
    public virtual ICollection<BasketStock> Stocks { get; set; } = new List<BasketStock>();
}
public class BasketStock
{
    public int Id { get; set; }
    public int BasketId { get; set; }
    public int StockId { get; set; }
    public decimal? MaxOrderValue { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public virtual Basket Basket { get; set; } = null!;
    public virtual Stock Stock { get; set; } = null!;
}
