using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 68 — RMSLimit Entity & Controller Logic Tests
// ============================================================

public class Day68RMSLimitTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private void SeedBrokerage(AppDbContext db, int id = 1)
    {
        db.BrokerageHouses.Add(new BrokerageHouse {
            Id = id, Name = "Pioneer", LicenseNumber = "DSE-TM-0001",
            Email = "info@pioneer.com", Phone = "01700000000",
            Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private RMSLimit MakeLimit(int brokerageId = 1, int? entityId = null,
        string entityType = "Investor", RMSLevel level = RMSLevel.Investor,
        RMSAction action = RMSAction.Block)
    => new RMSLimit
    {
        Level = level, EntityId = entityId, EntityType = entityType,
        BrokerageHouseId = brokerageId, MaxOrderValue = 5_000_000m,
        MaxDailyValue = 20_000_000m, MaxExposure = 50_000_000m,
        ConcentrationPct = 10m, ActionOnBreach = action,
        IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
    };

    // ── Entity defaults ──────────────────────────────────────

    [Fact]
    public void RMSLimit_DefaultIsActive_IsTrue()
        => Assert.True(new RMSLimit().IsActive);

    [Fact]
    public void RMSLimit_DefaultActionOnBreach_IsBlock()
        => Assert.Equal(RMSAction.Block, new RMSLimit().ActionOnBreach);

    [Fact]
    public void RMSLevel_Investor_IsOne()
        => Assert.Equal(1, (int)RMSLevel.Investor);

    [Fact]
    public void RMSLevel_Market_IsFive()
        => Assert.Equal(5, (int)RMSLevel.Market);

    [Fact]
    public void RMSAction_Warn_IsOne()
        => Assert.Equal(1, (int)RMSAction.Warn);

    [Fact]
    public void RMSAction_Block_IsTwo()
        => Assert.Equal(2, (int)RMSAction.Block);

    [Fact]
    public void RMSAction_Freeze_IsThree()
        => Assert.Equal(3, (int)RMSAction.Freeze);

    // ── DB persistence ───────────────────────────────────────

    [Fact]
    public async Task RMSLimit_CanBeSaved_ToDb()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        db.RMSLimits.Add(MakeLimit());
        await db.SaveChangesAsync();
        Assert.Equal(1, db.RMSLimits.Count());
    }

    [Fact]
    public async Task RMSLimit_MultipleLevels_AllPersist()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        db.RMSLimits.Add(MakeLimit(level: RMSLevel.Investor));
        db.RMSLimits.Add(MakeLimit(level: RMSLevel.Trader));
        db.RMSLimits.Add(MakeLimit(level: RMSLevel.Market));
        await db.SaveChangesAsync();
        Assert.Equal(3, db.RMSLimits.Count());
    }

    [Fact]
    public async Task RMSLimit_FilterByEntityType_ReturnsCorrect()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        db.RMSLimits.Add(MakeLimit(entityType: "Investor"));
        db.RMSLimits.Add(MakeLimit(entityType: "Trader"));
        db.RMSLimits.Add(MakeLimit(entityType: "Market"));
        await db.SaveChangesAsync();
        var investors = db.RMSLimits.Where(r => r.EntityType == "Investor").ToList();
        Assert.Single(investors);
    }

    [Fact]
    public async Task RMSLimit_FilterByIsActive_ReturnsOnlyActive()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        var active   = MakeLimit(); active.IsActive = true;
        var inactive = MakeLimit(); inactive.IsActive = false;
        db.RMSLimits.AddRange(active, inactive);
        await db.SaveChangesAsync();
        Assert.Single(db.RMSLimits.Where(r => r.IsActive));
    }

    [Fact]
    public async Task RMSLimit_DeactivateExisting_WhenNewSet()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        var old = MakeLimit(entityId: 5);
        db.RMSLimits.Add(old);
        await db.SaveChangesAsync();

        // Simulate set-limit: deactivate old, add new
        var existing = db.RMSLimits.Where(r => r.EntityId == 5 && r.IsActive).ToList();
        foreach (var e in existing) e.IsActive = false;
        db.RMSLimits.Add(MakeLimit(entityId: 5));
        await db.SaveChangesAsync();

        Assert.Equal(1, db.RMSLimits.Count(r => r.IsActive));
        Assert.Equal(2, db.RMSLimits.Count());
    }

    [Fact]
    public async Task RMSLimit_FilterByBrokerageHouseId()
    {
        var db = CreateDb();
        SeedBrokerage(db, 1);
        SeedBrokerage(db, 2);
        db.RMSLimits.Add(MakeLimit(brokerageId: 1));
        db.RMSLimits.Add(MakeLimit(brokerageId: 2));
        await db.SaveChangesAsync();
        Assert.Single(db.RMSLimits.Where(r => r.BrokerageHouseId == 1));
    }

    [Fact]
    public async Task RMSLimit_MaxOrderValue_Persists()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        var limit = MakeLimit();
        limit.MaxOrderValue = 10_000_000m;
        db.RMSLimits.Add(limit);
        await db.SaveChangesAsync();
        Assert.Equal(10_000_000m, db.RMSLimits.First().MaxOrderValue);
    }

    [Fact]
    public async Task RMSLimit_ConcentrationPct_Persists()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        var limit = MakeLimit();
        limit.ConcentrationPct = 15m;
        db.RMSLimits.Add(limit);
        await db.SaveChangesAsync();
        Assert.Equal(15m, db.RMSLimits.First().ConcentrationPct);
    }

    [Fact]
    public async Task RMSLimit_WarnAction_Persists()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        db.RMSLimits.Add(MakeLimit(action: RMSAction.Warn));
        await db.SaveChangesAsync();
        Assert.Equal(RMSAction.Warn, db.RMSLimits.First().ActionOnBreach);
    }

    [Fact]
    public async Task RMSLimit_FreezeAction_Persists()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        db.RMSLimits.Add(MakeLimit(action: RMSAction.Freeze));
        await db.SaveChangesAsync();
        Assert.Equal(RMSAction.Freeze, db.RMSLimits.First().ActionOnBreach);
    }

    [Fact]
    public async Task RMSLimit_EntityId_CanBeNull_ForGlobalLimit()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        db.RMSLimits.Add(MakeLimit(entityId: null));
        await db.SaveChangesAsync();
        Assert.Null(db.RMSLimits.First().EntityId);
    }

    [Fact]
    public async Task RMSLimit_SectorLevel_Persists()
    {
        var db = CreateDb();
        SeedBrokerage(db);
        db.RMSLimits.Add(MakeLimit(level: RMSLevel.Sector, entityType: "Sector"));
        await db.SaveChangesAsync();
        Assert.Equal(RMSLevel.Sector, db.RMSLimits.First().Level);
    }
}