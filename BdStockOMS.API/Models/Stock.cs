// Models/Stock.cs
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models;

public enum StockCategory
{
    A,      // Profitable, regular dividend, T+2
    B,      // Irregular dividend, T+2
    G,      // Govt treasury bonds
    N,      // Newly listed (first 30 days)
    Z,      // Defaulter — SPOT only, no margin
    Spot    // Special spot transactions, T+0
}

public class Stock
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string TradingCode { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CompanyName { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Exchange { get; set; } = string.Empty;

    // Stock category — determines settlement type and margin eligibility
    public StockCategory Category { get; set; } = StockCategory.A;

    public decimal LastTradePrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal ClosePrice { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
    public long Volume { get; set; }
    public decimal ValueInMillionTaka { get; set; }

    // Circuit breaker — DSE/CSE sets max/min price allowed today
    // Orders outside this range are automatically rejected
    public decimal CircuitBreakerHigh { get; set; }
    public decimal CircuitBreakerLow { get; set; }

    // Minimum quantity per order (usually 1 on DSE/CSE)
    public int BoardLotSize { get; set; } = 1;

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Order> Orders { get; set; }
        = new List<Order>();

    public virtual ICollection<Portfolio> Portfolios { get; set; }
        = new List<Portfolio>();
}
