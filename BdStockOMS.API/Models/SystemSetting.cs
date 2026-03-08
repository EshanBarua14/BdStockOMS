namespace BdStockOMS.API.Models;

public class SystemSetting
{
    public int Id              { get; set; }
    public string Key          { get; set; } = string.Empty;
    public string Value        { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category     { get; set; } = string.Empty; // Trading, Security, Commission
    public bool IsEncrypted    { get; set; } = false;
    public DateTime UpdatedAt  { get; set; } = DateTime.UtcNow;
    public int? UpdatedByUserId { get; set; }

    // Navigation
    public User? UpdatedBy { get; set; }
}
