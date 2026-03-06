using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.Models;

// Application logs — errors, warnings, info
// IT Support reads these to monitor system health
public enum LogLevel
{
    Info,       // = 0 normal operations
    Warning,    // = 1 something to watch
    Error,      // = 2 something went wrong
    Critical    // = 3 system breaking issue
}

public class SystemLog
{
    public int Id { get; set; }

    public LogLevel Level { get; set; } = LogLevel.Info;

    [Required]
    [MaxLength(100)]
    public string Source { get; set; } = string.Empty;
    // Where did this log come from?
    // Example: "ScraperService", "AuthService"

    [Required]
    public string Message { get; set; } = string.Empty;
    // The actual log message

    // Full error details if exception occurred
    public string? StackTrace { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}