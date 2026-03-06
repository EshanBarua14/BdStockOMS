using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models;

// Every important action gets recorded here
// CCD (Shadow Admin) reads this for compliance
// Example: "User 5 approved Order 12 at 10:30 AM"
public class AuditLog
{
    public int Id { get; set; }

    // Who performed the action?
    public int UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty;
    // Example: "OrderApproved", "UserCreated", "OrderRejected"

    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;
    // What type of thing was affected?
    // Example: "Order", "User", "Stock"

    public int? EntityId { get; set; }
    // Which specific record was affected?
    // Example: Order ID 42

    // JSON string of old values before change
    // nullable — no old values for new records
    public string? OldValues { get; set; }

    // JSON string of new values after change
    public string? NewValues { get; set; }

    [MaxLength(50)]
    public string? IpAddress { get; set; }
    // User's IP address — for compliance tracking

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    [ForeignKey("UserId")]
    public virtual User User { get; set; } = null!;
}