using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models;

// ALL 6 roles are stored in this ONE table
// The RoleId column tells us which role this user has
public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // We NEVER store plain passwords
    // We store the HASHED version (Day 4)
    // MaxLength(255) because hashed passwords are long
    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    // ── FOREIGN KEYS ──────────────────────────────
    // FK to Roles table — which role is this user?
    // int = stores the Role's Id number
    public int RoleId { get; set; }

    // FK to BrokerageHouses table — which firm?
    public int BrokerageHouseId { get; set; }

    // Traders are assigned investors to manage
    // nullable (int?) = not every user has a trader
    // Investors have a trader, others don't
    public int? AssignedTraderId { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } = false;
    // IsLocked = IT Support can lock accounts

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    // DateTime? = nullable — null until first login

    // ── NAVIGATION PROPERTIES ─────────────────────
    // These tell EF Core about relationships
    // They don't create extra columns —
    // EF uses the FK above (RoleId) to JOIN

    // Which role does this user have?
    // [ForeignKey("RoleId")] links to RoleId above
    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
    // null! = "trust me compiler, this won't be null
    //          at runtime — EF will populate it"

    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;

    // Self-referencing — a User (Trader) assigned to User (Investor)
    [ForeignKey("AssignedTraderId")]
    public virtual User? AssignedTrader { get; set; }

    // One Trader has many Investors assigned
    public virtual ICollection<Order> Orders { get; set; }
        = new List<Order>();
}