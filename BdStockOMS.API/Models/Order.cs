using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models;

// Enum = fixed set of allowed values
// Stored as INT in database (0,1,2,3,4,5)
// But we use readable names in code
public enum OrderType
{
    Buy,   // = 0 — investor wants to buy shares
    Sell   // = 1 — investor wants to sell shares
}

public enum OrderStatus
{
    Pending,    // = 0 — just placed by investor
    Approved,   // = 1 — approved by admin
    Rejected,   // = 2 — rejected by admin
    Executed,   // = 3 — executed by trader
    Completed,  // = 4 — fully processed
    Cancelled   // = 5 — cancelled by investor
}

public class Order
{
    public int Id { get; set; }

    // ── FOREIGN KEYS ──────────────────────────────
    public int InvestorId { get; set; }
    // Which investor placed this order?

    public int? TraderId { get; set; }
    // nullable — assigned when admin approves

    public int StockId { get; set; }
    // Which stock is being bought/sold?

    public int BrokerageHouseId { get; set; }
    // Which firm does this order belong to?

    // ── ORDER DETAILS ─────────────────────────────
    public OrderType OrderType { get; set; }
    // Buy or Sell — uses our enum above

    public int Quantity { get; set; }
    // How many shares?

    public decimal PriceAtOrder { get; set; }
    // Stock price WHEN order was placed
    // Important — price changes, we record the moment

    public decimal? ExecutionPrice { get; set; }
    // nullable — filled in WHEN trader executes
    // null until executed

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    // Default = Pending when first created

    [MaxLength(500)]
    public string? Notes { get; set; }
    // Optional notes — rejection reason, etc.

    // ── TIMESTAMPS ────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }   // null until approved
    public DateTime? ExecutedAt { get; set; }   // null until executed
    public DateTime? CompletedAt { get; set; }  // null until completed

    // ── NAVIGATION PROPERTIES ─────────────────────
    [ForeignKey("InvestorId")]
    public virtual User Investor { get; set; } = null!;

    [ForeignKey("TraderId")]
    public virtual User? Trader { get; set; }

    [ForeignKey("StockId")]
    public virtual Stock Stock { get; set; } = null!;

    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
}