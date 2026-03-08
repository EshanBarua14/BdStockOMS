namespace BdStockOMS.API.Models;

public class TwoFactorOtp
{
    public int Id          { get; set; }
    public int UserId      { get; set; }
    public string OtpCode  { get; set; } = string.Empty;
    public string Purpose  { get; set; } = string.Empty; // LOGIN, PASSWORD_RESET
    public DateTime ExpiresAt  { get; set; }
    public bool IsUsed         { get; set; } = false;
    public DateTime CreatedAt  { get; set; } = DateTime.UtcNow;

    // Navigation
    public User User { get; set; } = null!;
}
