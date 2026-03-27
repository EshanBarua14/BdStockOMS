using System.ComponentModel.DataAnnotations;
namespace BdStockOMS.API.Models;

public class AppSetting {
    [Key] public string Key { get; set; } = null!;
    public string Value { get; set; } = "";
    public string Category { get; set; } = "general";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = "system";
}
public class FeeStructure {
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = null!;
    public decimal BrokeragePercent { get; set; } = 0.40m;
    public decimal SecdFeePercent { get; set; } = 0.015m;
    public decimal CdblFeePercent { get; set; } = 0.015m;
    public decimal VatPercent { get; set; } = 15m;
    public decimal AitPercent { get; set; } = 0.05m;
    public decimal MinBrokerage { get; set; } = 10m;
    public string ApplyToCategory { get; set; } = "ALL";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public class SystemRole {
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = false;
    public string Permissions { get; set; } = "[]";
    public int UserCount { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
public class ApiKey {
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Name { get; set; } = null!;
    [Required] public string KeyHash { get; set; } = null!;
    public string KeyPrefix { get; set; } = "";
    public string Scopes { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = "system";
}
public class Announcement {
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Title { get; set; } = null!;
    [Required] public string Body { get; set; } = null!;
    public string Type { get; set; } = "info";
    public bool Active { get; set; } = true;
    public bool Pinned { get; set; } = false;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
public class BackupHistory {
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    public string? Size { get; set; }
    public string? Duration { get; set; }
    public string Status { get; set; } = "running";
    public string? FilePath { get; set; }
    public string? S3Key { get; set; }
    public string? ErrorDetail { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
public class IpWhitelistEntry {
    [Key] public string Id { get; set; } = Guid.NewGuid().ToString();
    [Required] public string Ip { get; set; } = null!;
    public string? Label { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public string AddedBy { get; set; } = "admin";
}
