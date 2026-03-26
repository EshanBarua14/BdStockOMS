// Models/Order.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models;

public enum OrderType
{
    Buy,    // Investor wants to buy shares
    Sell    // Investor wants to sell shares
}

public enum OrderCategory
{
    Market, // Execute at current market price immediately
    Limit   // Execute only when price reaches specified limit
}

public enum OrderStatus
{
    Pending,         // 0 — Just placed, awaiting exchange
    Open,            // 1 — Accepted by exchange, in queue
    PartiallyFilled, // 2 — Some qty filled
    Filled,          // 3 — Fully executed
    Completed,       // 4 — Settlement done by CCD
    Cancelled,       // 5 — Cancelled before fill
    Rejected         // 6 — Failed validation
}

public enum SettlementType
{
    T2,   // T+2 — pay within 2 working days (A,B,G,N category)
    T0    // T+0 — pay same day (Z, Spot category)
}

public enum PlacedByRole
{
    Investor,   // Investor placed their own order
    Trader      // Trader placed on behalf of investor
}

public class Order
{
    public int Id { get; set; }

    // ── FOREIGN KEYS ──────────────────────────────
    public int InvestorId { get; set; }         // Whose order is this?
    public int? TraderId { get; set; }          // Who placed it? (null if investor placed it)
    public int StockId { get; set; }            // Which stock?
    public int BrokerageHouseId { get; set; }   // Which brokerage?

    // ── ORDER DETAILS ─────────────────────────────
    public OrderType OrderType { get; set; }           // Buy or Sell
    public OrderCategory OrderCategory { get; set; }   // Market or Limit
    public int Quantity { get; set; }                  // How many shares?

    // Price when order was placed (market price at that moment)
    public decimal PriceAtOrder { get; set; }

    // For Limit orders — execute only at this price
    public decimal? LimitPrice { get; set; }

    // Actual price trade was executed at
    public decimal? ExecutionPrice { get; set; }

    // T+2 for normal, T+0 for Z/Spot category
    public SettlementType SettlementType { get; set; }

    // Was it placed by Investor themselves or by Trader?
    public PlacedByRole PlacedBy { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [MaxLength(500)]
    public string? Notes { get; set; }

    [MaxLength(500)]
    public string? RejectionReason { get; set; }

    // ── TIMESTAMPS ────────────────────────────────
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }

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
