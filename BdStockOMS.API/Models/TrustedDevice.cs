namespace BdStockOMS.API.Models;

public class TrustedDevice
{
    public int Id              { get; set; }
    public int UserId          { get; set; }
    public string DeviceToken  { get; set; } = string.Empty;
    public string DeviceName   { get; set; } = string.Empty;
    public string IpAddress    { get; set; } = string.Empty;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt  { get; set; }
    public bool IsRevoked      { get; set; } = false;

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    // Navigation
    public User User { get; set; } = null!;
}
