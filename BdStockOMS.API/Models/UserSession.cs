namespace BdStockOMS.API.Models;

public class UserSession
{
    public int Id              { get; set; }
    public int UserId          { get; set; }
    public string SessionToken { get; set; } = string.Empty;
    public string IpAddress    { get; set; } = string.Empty;
    public string? UserAgent   { get; set; }
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt  { get; set; }
    public bool IsRevoked      { get; set; } = false;

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;

    // Navigation
    public User User { get; set; } = null!;
}
