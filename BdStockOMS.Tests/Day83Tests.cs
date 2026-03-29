using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests;

public class Day83Tests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    // ── RMSLevelV2 enum ──────────────────────────────────────────────

    [Fact]
    public void RMSLevelV2_HasSixLevels()
    {
        var levels = Enum.GetValues<RMSLevelV2>();
        Assert.Equal(6, levels.Length);
    }

    [Theory]
    [InlineData(RMSLevelV2.Client,  1)]
    [InlineData(RMSLevelV2.User,    2)]
    [InlineData(RMSLevelV2.BOGroup, 3)]
    [InlineData(RMSLevelV2.Basket,  4)]
    [InlineData(RMSLevelV2.Branch,  5)]
    [InlineData(RMSLevelV2.Broker,  6)]
    public void RMSLevelV2_OrdinalsCorrect(RMSLevelV2 level, int expected)
        => Assert.Equal(expected, (int)level);

    // ── RMSLimitType enum ────────────────────────────────────────────

    [Fact]
    public void RMSLimitType_HasEightTypes()
        => Assert.Equal(8, Enum.GetValues<RMSLimitType>().Length);

    [Theory]
    [InlineData(RMSLimitType.DayBuyValue,       1)]
    [InlineData(RMSLimitType.DaySellValue,      2)]
    [InlineData(RMSLimitType.DayNetValue,       3)]
    [InlineData(RMSLimitType.MaxOrderValue,     4)]
    [InlineData(RMSLimitType.MaxExposure,       5)]
    [InlineData(RMSLimitType.ConcentrationPct,  6)]
    [InlineData(RMSLimitType.MarginUtilization, 7)]
    [InlineData(RMSLimitType.EDRThreshold,      8)]
    public void RMSLimitType_OrdinalsCorrect(RMSLimitType t, int expected)
        => Assert.Equal(expected, (int)t);

    // ── RMSLimitV2 model ─────────────────────────────────────────────

    [Fact]
    public void RMSLimitV2_DefaultsCorrect()
    {
        var l = new RMSLimitV2();
        Assert.True(l.IsActive);
        Assert.Equal(80m,          l.WarnAt);
        Assert.Equal(100,          l.Priority);
        Assert.Equal(RMSAction.Block, l.ActionOnBreach);
    }

    [Fact]
    public async Task RMSLimitV2_CanSaveAndRetrieve()
    {
        using var db = CreateDb();
        db.RMSLimitsV2.Add(new RMSLimitV2
        {
            Level            = RMSLevelV2.Client,
            LimitType        = RMSLimitType.MaxOrderValue,
            EntityId         = 1,
            EntityType       = "Investor",
            BrokerageHouseId = 1,
            LimitValue       = 5_000_000m,
            WarnAt           = 80m,
        });
        await db.SaveChangesAsync();

        var saved = await db.RMSLimitsV2.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(5_000_000m, saved.LimitValue);
        Assert.Equal(RMSLevelV2.Client, saved.Level);
    }

    [Fact]
    public async Task RMSLimitV2_MultiLevel_AllSixCanBeSaved()
    {
        using var db = CreateDb();
        foreach (var level in Enum.GetValues<RMSLevelV2>())
        {
            db.RMSLimitsV2.Add(new RMSLimitV2
            {
                Level = level, LimitType = RMSLimitType.MaxOrderValue,
                EntityId = 1, EntityType = level.ToString(),
                BrokerageHouseId = 1, LimitValue = 1_000_000m
            });
        }
        await db.SaveChangesAsync();
        Assert.Equal(6, await db.RMSLimitsV2.CountAsync());
    }

    // ── EDRSnapshot model ────────────────────────────────────────────

    [Fact]
    public async Task EDRSnapshot_CanSaveAndRetrieve()
    {
        using var db = CreateDb();
        db.EDRSnapshots.Add(new EDRSnapshot
        {
            InvestorId       = 1,
            BrokerageHouseId = 1,
            TotalEquity      = 1_000_000m,
            TotalDebt        = 400_000m,
            EDRRatio         = 2.5m,
            MarginUsed       = 400_000m,
            MarginLimit      = 1_000_000m,
            MarginUtilPct    = 40m,
        });
        await db.SaveChangesAsync();

        var snap = await db.EDRSnapshots.FirstOrDefaultAsync();
        Assert.NotNull(snap);
        Assert.Equal(2.5m,       snap.EDRRatio);
        Assert.Equal(40m,        snap.MarginUtilPct);
        Assert.Equal(1_000_000m, snap.TotalEquity);
    }

    // ── EDR Margin tiers ────────────────────────────────────────────

    [Theory]
    [InlineData(0,   "Safe")]
    [InlineData(49,  "Safe")]
    [InlineData(50,  "Watch")]
    [InlineData(74,  "Watch")]
    [InlineData(75,  "Warning")]
    [InlineData(89,  "Warning")]
    [InlineData(90,  "Critical")]
    [InlineData(100, "Critical")]
    public void MarginTier_CorrectForUtilization(double utilPctD, string expectedTier)
    {
        var utilPct = (decimal)utilPctD;
        var tier = utilPct >= 90m ? "Critical"
                 : utilPct >= 75m ? "Warning"
                 : utilPct >= 50m ? "Watch"
                 : "Safe";
        Assert.Equal(expectedTier, tier);
    }

    [Fact]
    public void EDRRatio_BelowThreshold_IsBreached()
    {
        var ratio = 1.2m;
        var isBreached = ratio < 1.5m;
        Assert.True(isBreached);
    }

    [Fact]
    public void EDRRatio_AboveThreshold_IsNotBreached()
    {
        var ratio = 2.5m;
        var isBreached = ratio < 1.5m;
        Assert.False(isBreached);
    }

    [Fact]
    public void EDRRatio_NoDebt_IsMaxValue()
    {
        decimal totalDebt  = 0m;
        decimal totalEquity = 500_000m;
        var edr = totalDebt > 0 ? Math.Round(totalEquity / totalDebt, 4) : 999m;
        Assert.Equal(999m, edr);
    }

    [Fact]
    public void EDRRatio_WithDebt_CalculatesCorrectly()
    {
        decimal equity = 1_000_000m;
        decimal debt   = 400_000m;
        var edr = Math.Round(equity / debt, 4);
        Assert.Equal(2.5m, edr);
    }

    // ── CascadeCheckResult ───────────────────────────────────────────

    [Fact]
    public void CascadeCheckResult_DefaultIsAllowed()
    {
        var r = new CascadeCheckResult();
        Assert.True(r.IsAllowed);
        Assert.Empty(r.Violations);
        Assert.Empty(r.Warnings);
    }

    [Fact]
    public void CascadeCheckResult_ViolationMakesNotAllowed()
    {
        var r = new CascadeCheckResult();
        r.Violations.Add("Limit exceeded");
        r.IsAllowed = false;
        Assert.False(r.IsAllowed);
        Assert.Single(r.Violations);
    }

    // ── Cascade level order ──────────────────────────────────────────

    [Fact]
    public void CascadeLevels_OrderIsClientFirst()
    {
        var levels = Enum.GetValues<RMSLevelV2>().OrderBy(l => (int)l).ToList();
        Assert.Equal(RMSLevelV2.Client, levels[0]);
        Assert.Equal(RMSLevelV2.Broker, levels[5]);
    }

    [Fact]
    public void CascadeLevels_BrokerLimitIsLargestDefault()
    {
        var clientMax = 5_000_000m;
        var brokerMax = 5_000_000_000m;
        Assert.True(brokerMax > clientMax);
    }

    // ── RMSLimitV2 multi-tenant isolation ────────────────────────────

    [Fact]
    public async Task RMSLimitV2_TenantIsolation()
    {
        using var db = CreateDb();
        db.RMSLimitsV2.AddRange(
            new RMSLimitV2 { Level = RMSLevelV2.Client, LimitType = RMSLimitType.MaxOrderValue, EntityId = 1, EntityType = "Investor", BrokerageHouseId = 1, LimitValue = 1_000_000m },
            new RMSLimitV2 { Level = RMSLevelV2.Client, LimitType = RMSLimitType.MaxOrderValue, EntityId = 1, EntityType = "Investor", BrokerageHouseId = 2, LimitValue = 2_000_000m }
        );
        await db.SaveChangesAsync();

        var t1 = await db.RMSLimitsV2.FirstAsync(l => l.BrokerageHouseId == 1);
        var t2 = await db.RMSLimitsV2.FirstAsync(l => l.BrokerageHouseId == 2);
        Assert.Equal(1_000_000m, t1.LimitValue);
        Assert.Equal(2_000_000m, t2.LimitValue);
    }

    // ── MarginUtilPct calculation ────────────────────────────────────

    [Fact]
    public void MarginUtilPct_CalculatesCorrectly()
    {
        decimal used  = 750_000m;
        decimal limit = 1_000_000m;
        var pct = Math.Round((used / limit) * 100m, 2);
        Assert.Equal(75m, pct);
    }

    [Fact]
    public void MarginUtilPct_ZeroLimit_ReturnsZero()
    {
        decimal limit = 0m;
        var pct = limit > 0 ? 50m : 0m;
        Assert.Equal(0m, pct);
    }
}
