namespace BdStockOMS.API.Models;

public class UserPermission
{
    public int Id              { get; set; }
    public int UserId          { get; set; }
    public string Permission   { get; set; } = string.Empty; // e.g. "orders.approve", "kyc.view"
    public string Module       { get; set; } = string.Empty; // e.g. "Orders", "KYC", "Reports"
    public bool IsGranted      { get; set; } = true;         // false = explicitly denied
    public int GrantedByUserId { get; set; }
    public DateTime GrantedAt  { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }                 // null = never expires
    public bool IsActive => IsGranted &&
                            (!ExpiresAt.HasValue || ExpiresAt > DateTime.UtcNow);
    // Navigation
    public User User      { get; set; } = null!;
    public User GrantedBy { get; set; } = null!;
}
