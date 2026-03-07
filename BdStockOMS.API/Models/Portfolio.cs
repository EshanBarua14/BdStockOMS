// Models/Portfolio.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models;

public class Portfolio
{
    public int Id { get; set; }

    // ── FOREIGN KEYS ──────────────────────────────
    public int InvestorId { get; set; }     // Whose portfolio?
    public int StockId { get; set; }        // Which stock holding?
    public int BrokerageHouseId { get; set; }

    // ── HOLDINGS ──────────────────────────────────
    // How many shares this investor currently holds
    public int Quantity { get; set; }

    // Average price paid per share across all buys
    // Example: bought 100 @ 380, then 100 @ 400
    //          AverageBuyPrice = 390
    public decimal AverageBuyPrice { get; set; }

    // Total value = Quantity × current market price
    // Calculated at query time, not stored
    // (we store AverageBuyPrice for profit/loss calc)

    public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;

    // ── NAVIGATION PROPERTIES ─────────────────────
    [ForeignKey("InvestorId")]
    public virtual User Investor { get; set; } = null!;

    [ForeignKey("StockId")]
    public virtual Stock Stock { get; set; } = null!;

    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;
}
