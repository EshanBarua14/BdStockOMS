using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services;

public interface IAuditService
{
    Task LogAsync(int userId, string action, string entityName,
                  int? entityId, string? oldValue, string? newValue, string? ip);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db)
    {
        _db = db;
    }

    public async Task LogAsync(int userId, string action, string entityName,
                               int? entityId, string? oldValue, string? newValue, string? ip)
    {
        var log = new AuditLog
        {
            UserId     = userId,
            Action     = action,
            EntityType = entityName,
            EntityId   = entityId,
            OldValues  = oldValue,
            NewValues  = newValue,
            IpAddress  = ip,
            CreatedAt  = DateTime.UtcNow
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
