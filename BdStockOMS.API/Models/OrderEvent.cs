using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace BdStockOMS.API.Models;

public class OrderEvent
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    [MaxLength(50)]
    public string FromStatus { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ToStatus   { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Reason    { get; set; }

    [MaxLength(200)]
    public string? TriggeredBy { get; set; } // UserId or "System"

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    // ── NAVIGATION ───────────────────────────────
    [ForeignKey("OrderId")]
    public virtual Order Order { get; set; } = null!;
}
