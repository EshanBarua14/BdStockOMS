using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BdStockOMS.API.Models;

public class MarketDepth
{
    public int Id      { get; set; }
    public int StockId { get; set; }

    [MaxLength(10)]
    public string Exchange { get; set; } = string.Empty;

    // Bid levels (buy orders waiting) — Level 1 is best bid
    public decimal Bid1Price { get; set; } public long Bid1Qty { get; set; }
    public decimal Bid2Price { get; set; } public long Bid2Qty { get; set; }
    public decimal Bid3Price { get; set; } public long Bid3Qty { get; set; }
    public decimal Bid4Price { get; set; } public long Bid4Qty { get; set; }
    public decimal Bid5Price { get; set; } public long Bid5Qty { get; set; }

    // Ask levels (sell orders waiting) — Level 1 is best ask
    public decimal Ask1Price { get; set; } public long Ask1Qty { get; set; }
    public decimal Ask2Price { get; set; } public long Ask2Qty { get; set; }
    public decimal Ask3Price { get; set; } public long Ask3Qty { get; set; }
    public decimal Ask4Price { get; set; } public long Ask4Qty { get; set; }
    public decimal Ask5Price { get; set; } public long Ask5Qty { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // ── NAVIGATION ───────────────────────────────
    [ForeignKey("StockId")]
    public virtual Stock Stock { get; set; } = null!;
}
