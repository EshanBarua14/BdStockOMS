// Models/User.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BdStockOMS.API.Models;

public enum AccountType
{
    Cash,   // Must have full cash to buy
    Margin  // Can borrow up to margin limit from broker
}

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

    [Required]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Phone { get; set; } = string.Empty;

    // ── FOREIGN KEYS ──────────────────────────────
    public int RoleId { get; set; }
    public int BrokerageHouseId { get; set; }

    // Traders are assigned investors to manage
    public int? AssignedTraderId { get; set; }

    // ── BO ACCOUNT (Beneficiary Owner Account) ────
    // Only Investors have BO accounts
    // Issued by CDBL — format: 1201950012345678
    [MaxLength(20)]
    public string? BONumber { get; set; }

    // Cash or Margin account type
    public AccountType? AccountType { get; set; }

    // Available cash balance in taka
    public decimal CashBalance { get; set; } = 0;

    // Maximum margin the broker will lend (Margin accounts only)
    public decimal MarginLimit { get; set; } = 0;

    // How much margin is currently in use
    public decimal MarginUsed { get; set; } = 0;

    // CCD can freeze a BO account
    public bool IsBOAccountActive { get; set; } = false;

    // ── STATUS ────────────────────────────────────
    public bool IsActive { get; set; } = true;
    public bool IsLocked { get; set; } = false;
    public int FailedLoginCount { get; set; } = 0;
    public DateTime? LockoutUntil { get; set; }
    public bool ForcePasswordChange { get; set; } = false;
    public DateTime? PasswordChangedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // ── NAVIGATION PROPERTIES ─────────────────────
    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;

    [ForeignKey("BrokerageHouseId")]
    public virtual BrokerageHouse BrokerageHouse { get; set; } = null!;

    [ForeignKey("AssignedTraderId")]
    public virtual User? AssignedTrader { get; set; }

    public virtual ICollection<Order> Orders { get; set; }
        = new List<Order>();

    public virtual ICollection<Portfolio> Portfolios { get; set; }
        = new List<Portfolio>();
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public virtual ICollection<LoginHistory> LoginHistories { get; set; } = new List<LoginHistory>();
}
