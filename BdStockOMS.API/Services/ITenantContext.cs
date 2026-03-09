namespace BdStockOMS.API.Services;

public interface ITenantContext
{
    int    BrokerageHouseId { get; }
    string UserId           { get; }
    string Role             { get; }
    bool   IsSuperAdmin     { get; }
}
