using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BdStockOMS.API.Services;

public class SharedTenantDbContextFactory : ITenantDbContextFactory
{
    private readonly AppDbContext _sharedDb;
    private readonly IConfiguration _config;
    private readonly ILogger<SharedTenantDbContextFactory> _logger;

    public bool IsPerTenantDbEnabled => false;

    public SharedTenantDbContextFactory(
        AppDbContext sharedDb,
        IConfiguration config,
        ILogger<SharedTenantDbContextFactory> logger)
    {
        _sharedDb = sharedDb;
        _config = config;
        _logger = logger;
    }

    public async Task<AppDbContext> CreateForTenantAsync(int brokerageHouseId)
    {
        var conn = await _sharedDb.BrokerageConnections
            .FirstOrDefaultAsync(c => c.BrokerageHouseId == brokerageHouseId && c.IsActive);

        if (conn == null || !IsPerTenantDbEnabled)
        {
            _logger.LogDebug(
                "SharedTenantDbContextFactory: returning shared DB for tenant {Id}", brokerageHouseId);
            return _sharedDb;
        }

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(conn.ConnectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
