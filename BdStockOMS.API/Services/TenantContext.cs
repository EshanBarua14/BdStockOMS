using System.Security.Claims;

namespace BdStockOMS.API.Services;

public class TenantContext : ITenantContext
{
    public int    BrokerageHouseId { get; }
    public string UserId           { get; }
    public string Role             { get; }
    public bool   IsSuperAdmin     => Role == "SuperAdmin";

    public TenantContext(IHttpContextAccessor accessor)
    {
        var user = accessor.HttpContext?.User;

        UserId = user?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        Role   = user?.FindFirstValue(ClaimTypes.Role)           ?? string.Empty;

        var bhClaim = user?.FindFirstValue("BrokerageHouseId");
        BrokerageHouseId = int.TryParse(bhClaim, out var id) ? id : 0;
    }
}
