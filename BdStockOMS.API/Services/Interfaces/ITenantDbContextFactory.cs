using System.Threading.Tasks;
using BdStockOMS.API.Data;

namespace BdStockOMS.API.Services.Interfaces;

public interface ITenantDbContextFactory
{
    Task<AppDbContext> CreateForTenantAsync(int brokerageHouseId);
    bool IsPerTenantDbEnabled { get; }
}
