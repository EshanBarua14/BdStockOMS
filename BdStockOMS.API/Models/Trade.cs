using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BdStockOMS.API.Models;

public enum TradeStatus
{
    Filled,
    PartialFill,
    Cancelled
}

public class Trade
{
    public int Id { get; set; }

    public int OrderId           { get; set; }
    public int StockId           { get; set; }
    public int InvestorId        { get; set; }
    public int BrokerageHouseId  { get; set; }

    [MaxLength(50)]
    public string Side           { get; set; } = string.Empty; // "Buy" or "Sell"

    public int     Quantity      { get; set; }
    public decimal Price         { get; set; }
    public decimal TotalValue    { get; set; }  // Quantity * Price

    [MaxLength(100)]
    public string? ExchangeTradeId { get; set; }

    public TradeStatus Status    { get; set; } = TradeStatus.Filled;
    public DateTime    TradedAt  { get; set; } = DateTime.UtcNow;

    // ── NAVIGATION ───────────────────────────────
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;

    [ForeignKey("StockId")]
    public virtual Stock Stock { get; set; } = null!;

    [ForeignKey("InvestorId")]
    public virtual User Investor { get; set; } = null!;

    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
}
