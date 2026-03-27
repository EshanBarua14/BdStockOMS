using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 63 — BrokerageSettingsService Tests
// ============================================================

public class Day63BrokerageSettingsTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private void SeedBrokerage(AppDbContext db, int id = 1)
    {
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = id, Name = "Pioneer Securities",
            LicenseNumber = "DSE-TM-0001",
            Email = "info@pioneer.com", Phone = "01700000000",
            Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    [Fact]
    public async Task GetOrCreateSettings_NewBrokerage_CreatesDefaults()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        var result = await new BrokerageSettingsService(db).GetOrCreateSettingsAsync(1);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetOrCreateSettings_CalledTwice_OnlyOneRecord()
    {
        var db  = CreateDb();
        SeedBrokerage(db);
        var svc = new BrokerageSettingsService(db);
        await svc.GetOrCreateSettingsAsync(1);
        await svc.GetOrCreateSettingsAsync(1);
        Assert.Equal(1, db.BrokerageSettings.Count());
    }

    [Fact]
    public async Task GetOrCreateSettings_ExistingBrokerage_ReturnsSameId()
    {
        var db  = CreateDb();
        SeedBrokerage(db);
        var svc = new BrokerageSettingsService(db);
        var r1  = await svc.GetOrCreateSettingsAsync(1);
        var r2  = await svc.GetOrCreateSettingsAsync(1);
        Assert.Equal(r1.Id, r2.Id);
    }

    [Fact]
    public async Task IsFeatureEnabled_UnknownFeature_ReturnsFalse()
    {
        var db  = CreateDb();
        SeedBrokerage(db);
        var ok  = await new BrokerageSettingsService(db).IsFeatureEnabledAsync(1, "NonExistentFeature");
        Assert.False(ok);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsUpdatedSettings()
    {
        var db  = CreateDb();
        SeedBrokerage(db);
        var svc = new BrokerageSettingsService(db);
        await svc.GetOrCreateSettingsAsync(1);
        var req = new UpdateSettingsRequest { IsMarginTradingEnabled = true };
        var r   = await svc.UpdateSettingsAsync(1, req);
        Assert.NotNull(r);
    }

    [Fact]
    public async Task GetOrCreateSettings_TwoBrokerages_SeparateSettings()
    {
        var db = CreateDb();
        SeedBrokerage(db, 1);
        SeedBrokerage(db, 2);
        var svc = new BrokerageSettingsService(db);
        var r1  = await svc.GetOrCreateSettingsAsync(1);
        var r2  = await svc.GetOrCreateSettingsAsync(2);
        Assert.NotEqual(r1.Id, r2.Id);
        Assert.Equal(2, db.BrokerageSettings.Count());
    }
}
