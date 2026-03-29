using System.Security.Claims;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class TenantContext : ITenantContext
{
    public int    BrokerageHouseId { get; }
    public string UserId           { get; }
    public string Role             { get; }
    public bool   IsSuperAdmin     => Role == "SuperAdmin";

    private readonly AppDbContext _db;

    public TenantContext(IHttpContextAccessor accessor, AppDbContext db)
    {
        _db = db;
        var user = accessor.HttpContext?.User;

        UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Role   = user?.FindFirstValue(ClaimTypes.Role)           ?? string.Empty;

        var bhClaim = user?.FindFirstValue("BrokerageHouseId")
                   ?? user?.FindFirstValue("brokerageHouseId");
        BrokerageHouseId = int.TryParse(bhClaim, out var id) ? id : 0;
    }

    public bool IsFeatureEnabled(string featureKey)
    {
        if (IsSuperAdmin) return true;
        if (BrokerageHouseId == 0) return false;

        var flag = _db.TenantFeatureFlags
            .AsNoTracking()
            .FirstOrDefault(f =>
                f.BrokerageHouseId == BrokerageHouseId &&
                f.FeatureKey == featureKey);

        return flag?.IsEnabled ?? false;
    }
}
