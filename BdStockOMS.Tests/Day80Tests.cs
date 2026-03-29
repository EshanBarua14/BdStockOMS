using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using BdStockOMS.API.Services.Interfaces;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BdStockOMS.Tests;

public class Day80Tests
{
    private static AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public void TenantFeatureFlag_DefaultsCorrect()
    {
        var flag = new TenantFeatureFlag();
        Assert.False(flag.IsEnabled);
        Assert.Null(flag.Value);
        Assert.Null(flag.Description);
        Assert.Null(flag.SetByUserId);
        Assert.True(flag.CreatedAt <= DateTime.UtcNow);
        Assert.True(flag.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void TenantFeatureFlag_CanSetFields()
    {
        var flag = new TenantFeatureFlag
        {
            BrokerageHouseId = 1,
            FeatureKey       = "ENABLE_CSE_TRADING",
            IsEnabled        = true,
            Value            = "true",
            Description      = "Enable CSE exchange trading",
            SetByUserId      = "admin@test.com"
        };
        Assert.Equal(1,                    flag.BrokerageHouseId);
        Assert.Equal("ENABLE_CSE_TRADING", flag.FeatureKey);
        Assert.True(flag.IsEnabled);
        Assert.Equal("true",               flag.Value);
        Assert.Equal("admin@test.com",     flag.SetByUserId);
    }

    [Fact]
    public async Task TenantFeatureFlag_CanSaveAndRetrieve()
    {
        using var db = CreateInMemoryDb();
        db.TenantFeatureFlags.Add(new TenantFeatureFlag
        {
            BrokerageHouseId = 1,
            FeatureKey       = "BLOCK_BOARD_TRADING",
            IsEnabled        = true
        });
        await db.SaveChangesAsync();

        var retrieved = await db.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.FeatureKey == "BLOCK_BOARD_TRADING");

        Assert.NotNull(retrieved);
        Assert.True(retrieved.IsEnabled);
        Assert.Equal(1, retrieved.BrokerageHouseId);
    }

    [Fact]
    public async Task TenantFeatureFlag_MultiTenant_IsolationWorks()
    {
        using var db = CreateInMemoryDb();
        db.TenantFeatureFlags.AddRange(
            new TenantFeatureFlag { BrokerageHouseId = 1, FeatureKey = "FEATURE_X", IsEnabled = true },
            new TenantFeatureFlag { BrokerageHouseId = 2, FeatureKey = "FEATURE_X", IsEnabled = false }
        );
        await db.SaveChangesAsync();

        var t1 = await db.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.BrokerageHouseId == 1 && f.FeatureKey == "FEATURE_X");
        var t2 = await db.TenantFeatureFlags
            .FirstOrDefaultAsync(f => f.BrokerageHouseId == 2 && f.FeatureKey == "FEATURE_X");

        Assert.True(t1!.IsEnabled);
        Assert.False(t2!.IsEnabled);
    }

    [Fact]
    public async Task TenantFeatureFlag_CanUpdate()
    {
        using var db = CreateInMemoryDb();
        db.TenantFeatureFlags.Add(new TenantFeatureFlag
        {
            BrokerageHouseId = 1,
            FeatureKey       = "MARGIN_TRADING",
            IsEnabled        = false
        });
        await db.SaveChangesAsync();

        var flag = await db.TenantFeatureFlags.FirstAsync();
        flag.IsEnabled = true;
        flag.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var updated = await db.TenantFeatureFlags.FirstAsync();
        Assert.True(updated.IsEnabled);
    }

    [Fact]
    public async Task TenantFeatureFlag_CanDelete()
    {
        using var db = CreateInMemoryDb();
        db.TenantFeatureFlags.Add(new TenantFeatureFlag
        {
            BrokerageHouseId = 1,
            FeatureKey       = "TEMP_FEATURE",
            IsEnabled        = true
        });
        await db.SaveChangesAsync();

        var flag = await db.TenantFeatureFlags.FirstAsync();
        db.TenantFeatureFlags.Remove(flag);
        await db.SaveChangesAsync();

        Assert.Equal(0, await db.TenantFeatureFlags.CountAsync());
    }

    [Fact]
    public async Task TenantFeatureFlag_BulkFlagsPerTenant()
    {
        using var db = CreateInMemoryDb();
        var features = new[]
        {
            "ENABLE_CSE", "BLOCK_BOARD", "MARGIN_TRADING",
            "ICEBERG_ORDERS", "PRIVATE_ORDERS", "FOK_ORDERS"
        };
        foreach (var key in features)
            db.TenantFeatureFlags.Add(new TenantFeatureFlag
            {
                BrokerageHouseId = 1,
                FeatureKey       = key,
                IsEnabled        = true
            });
        await db.SaveChangesAsync();

        var count = await db.TenantFeatureFlags
            .CountAsync(f => f.BrokerageHouseId == 1);
        Assert.Equal(6, count);
    }

    [Fact]
    public void BrokerageConnection_DefaultsCorrect()
    {
        var conn = new BrokerageConnection();
        Assert.False(conn.IsActive);
        Assert.Equal(string.Empty, conn.ConnectionString);
        Assert.Equal(string.Empty, conn.DatabaseName);
        Assert.Null(conn.Notes);
    }

    [Fact]
    public async Task BrokerageConnection_CanSaveAndRetrieve()
    {
        using var db = CreateInMemoryDb();
        db.BrokerageConnections.Add(new BrokerageConnection
        {
            BrokerageHouseId = 1,
            ConnectionString = "Server=.;Database=BdStockOMS_Test;",
            DatabaseName     = "BdStockOMS_Test",
            IsActive         = true
        });
        await db.SaveChangesAsync();

        var conn = await db.BrokerageConnections
            .FirstOrDefaultAsync(c => c.BrokerageHouseId == 1);

        Assert.NotNull(conn);
        Assert.True(conn.IsActive);
        Assert.Equal("BdStockOMS_Test", conn.DatabaseName);
    }

    [Fact]
    public void SharedTenantDbContextFactory_IsNotPerTenant()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var logger = NullLogger<SharedTenantDbContextFactory>.Instance;
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

        var factory = new SharedTenantDbContextFactory(db, config, logger);
        Assert.False(factory.IsPerTenantDbEnabled);
    }

    [Fact]
    public async Task SharedTenantDbContextFactory_ReturnsSharedDb_WhenNoConnection()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);
        var logger = NullLogger<SharedTenantDbContextFactory>.Instance;
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build();

        var factory = new SharedTenantDbContextFactory(db, config, logger);
        var result = await factory.CreateForTenantAsync(99);

        Assert.Same(db, result);
    }

    [Fact]
    public void TenantProvisioningService_SanitizesDatabaseName()
    {
        Assert.Equal("BdStockOMS_PioneerSecuritiesLtd",
            TenantProvisioningService.SanitizeDatabaseName("Pioneer Securities Ltd!"));
        Assert.Equal("BdStockOMS_ABCBrokerage",
            TenantProvisioningService.SanitizeDatabaseName("ABC Brokerage"));
        Assert.Equal("BdStockOMS_TestBroker123",
            TenantProvisioningService.SanitizeDatabaseName("Test Broker 123"));
    }

    [Fact]
    public void TenantProvisioningService_SanitizeRemovesSpecialChars()
    {
        var result = TenantProvisioningService.SanitizeDatabaseName("ABC & XYZ (Dhaka)!");
        Assert.StartsWith("BdStockOMS_", result);
        Assert.DoesNotContain(" ",  result);
        Assert.DoesNotContain("&",  result);
        Assert.DoesNotContain("(",  result);
        Assert.DoesNotContain("!",  result);
    }

    [Fact]
    public void ITenantContext_HasIsFeatureEnabled()
    {
        var type   = typeof(ITenantContext);
        var method = type.GetMethod("IsFeatureEnabled");
        Assert.NotNull(method);
        Assert.Equal(typeof(bool), method!.ReturnType);
        var param = method.GetParameters();
        Assert.Single(param);
        Assert.Equal(typeof(string), param[0].ParameterType);
    }

    [Fact]
    public void ITenantContext_HasAllRequiredMembers()
    {
        var type = typeof(ITenantContext);
        Assert.NotNull(type.GetProperty("BrokerageHouseId"));
        Assert.NotNull(type.GetProperty("UserId"));
        Assert.NotNull(type.GetProperty("Role"));
        Assert.NotNull(type.GetProperty("IsSuperAdmin"));
        Assert.NotNull(type.GetMethod("IsFeatureEnabled"));
    }

    [Fact]
    public void ITenantDbContextFactory_HasCorrectMembers()
    {
        var type = typeof(ITenantDbContextFactory);
        Assert.NotNull(type.GetMethod("CreateForTenantAsync"));
        Assert.NotNull(type.GetProperty("IsPerTenantDbEnabled"));
    }
}
