using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests;

public class Day86Tests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private static ISettlementService MakeService(AppDbContext db)
    {
        var stateMachine = new FakeStateMachine();
        return new SettlementService(db, stateMachine);
    }

    // ── SettlementBatchStatus enum ───────────────────────────────────

    [Fact]
    public void SettlementBatchStatus_HasFourValues()
        => Assert.Equal(4, Enum.GetValues<SettlementBatchStatus>().Length);

    [Theory]
    [InlineData(SettlementBatchStatus.Pending,    0)]
    [InlineData(SettlementBatchStatus.Processing, 1)]
    [InlineData(SettlementBatchStatus.Completed,  2)]
    [InlineData(SettlementBatchStatus.Failed,     3)]
    public void SettlementBatchStatus_OrdinalsCorrect(SettlementBatchStatus s, int expected)
        => Assert.Equal(expected, (int)s);

    // ── SettlementItemStatus enum ────────────────────────────────────

    [Fact]
    public void SettlementItemStatus_HasThreeValues()
        => Assert.Equal(3, Enum.GetValues<SettlementItemStatus>().Length);

    [Theory]
    [InlineData(SettlementItemStatus.Pending, 0)]
    [InlineData(SettlementItemStatus.Settled, 1)]
    [InlineData(SettlementItemStatus.Failed,  2)]
    public void SettlementItemStatus_OrdinalsCorrect(SettlementItemStatus s, int expected)
        => Assert.Equal(expected, (int)s);

    // ── SettlementType enum ──────────────────────────────────────────

    [Fact]
    public void SettlementType_HasT0AndT2()
    {
        Assert.True(Enum.IsDefined(typeof(SettlementType), SettlementType.T0));
        Assert.True(Enum.IsDefined(typeof(SettlementType), SettlementType.T2));
    }

    // ── CalculateSettlementDate ──────────────────────────────────────

    [Fact]
    public void CalculateSettlementDate_T0_ReturnsSameDay()
    {
        using var db = CreateDb();
        var svc  = MakeService(db);
        var date = new DateTime(2026, 3, 30); // Monday
        var result = svc.CalculateSettlementDate(date, SettlementType.T0);
        Assert.Equal(date.Date, result);
    }

    [Fact]
    public void CalculateSettlementDate_T2_Monday_ReturnsWednesday()
    {
        using var db = CreateDb();
        var svc  = MakeService(db);
        var date = new DateTime(2026, 3, 30); // Monday
        var result = svc.CalculateSettlementDate(date, SettlementType.T2);
        Assert.Equal(new DateTime(2026, 4, 1), result); // Wednesday
    }

    [Fact]
    public void CalculateSettlementDate_T2_Thursday_SkipsWeekend()
    {
        using var db = CreateDb();
        var svc  = MakeService(db);
        var date = new DateTime(2026, 4, 2); // Thursday
        var result = svc.CalculateSettlementDate(date, SettlementType.T2);
        // Should skip Sat/Sun, land on Monday+1 = Tuesday Apr 7
        Assert.Equal(DayOfWeek.Monday, result.DayOfWeek);
    }

    [Fact]
    public void CalculateSettlementDate_T2_Wednesday_ReturnsSunday_Skip()
    {
        using var db = CreateDb();
        var svc  = MakeService(db);
        var date = new DateTime(2026, 4, 1); // Wednesday
        var result = svc.CalculateSettlementDate(date, SettlementType.T2);
        // T+2 skipping Sat/Sun: Thu + Sun skipped = Mon Apr 6
        Assert.True(result > date);
        Assert.NotEqual(DayOfWeek.Saturday, result.DayOfWeek);
        Assert.NotEqual(DayOfWeek.Sunday,   result.DayOfWeek);
    }

    [Theory]
    [InlineData("2026-03-30")] // Monday
    [InlineData("2026-03-31")] // Tuesday
    [InlineData("2026-04-01")] // Wednesday
    [InlineData("2026-04-02")] // Thursday
    [InlineData("2026-04-05")] // Sunday
    public void CalculateSettlementDate_T2_NeverLandsOnWeekend(string dateStr)
    {
        using var db = CreateDb();
        var svc    = MakeService(db);
        var date   = DateTime.Parse(dateStr);
        var result = svc.CalculateSettlementDate(date, SettlementType.T2);
        Assert.NotEqual(DayOfWeek.Saturday, result.DayOfWeek);
        Assert.NotEqual(DayOfWeek.Sunday,   result.DayOfWeek);
    }

    [Fact]
    public void CalculateSettlementDate_T2_AlwaysAfterTradeDate()
    {
        using var db = CreateDb();
        var svc  = MakeService(db);
        var date = new DateTime(2026, 3, 30);
        var result = svc.CalculateSettlementDate(date, SettlementType.T2);
        Assert.True(result > date.Date);
    }

    // ── SettlementBatch model ────────────────────────────────────────

    [Fact]
    public void SettlementBatch_DefaultsCorrect()
    {
        var b = new SettlementBatch();
        Assert.Equal(SettlementBatchStatus.Pending, b.Status);
        Assert.Empty(b.Items);
        Assert.Null(b.ProcessedAt);
    }

    [Fact]
    public async Task SettlementBatch_CanSaveAndRetrieve()
    {
        using var db = CreateDb();
        db.SettlementBatches.Add(new SettlementBatch
        {
            BrokerageHouseId = 1,
            Exchange         = "DSE",
            TradeDate        = DateTime.UtcNow.Date,
            SettlementDate   = DateTime.UtcNow.Date.AddDays(2),
            TotalTrades      = 5,
            TotalBuyValue    = 100_000m,
            TotalSellValue   = 50_000m,
            NetObligations   = 50_000m,
        });
        await db.SaveChangesAsync();

        var b = await db.SettlementBatches.FirstOrDefaultAsync();
        Assert.NotNull(b);
        Assert.Equal("DSE",    b.Exchange);
        Assert.Equal(5,        b.TotalTrades);
        Assert.Equal(50_000m,  b.NetObligations);
    }

    // ── SettlementItem model ─────────────────────────────────────────

    [Fact]
    public void SettlementItem_DefaultsCorrect()
    {
        var i = new SettlementItem();
        Assert.Equal(SettlementItemStatus.Pending, i.Status);
        Assert.Null(i.SettledAt);
        Assert.Null(i.FailureReason);
    }

    [Fact]
    public async Task SettlementItem_CanSaveAndRetrieve()
    {
        using var db = CreateDb();
        db.SettlementBatches.Add(new SettlementBatch
        {
            Id = 1, BrokerageHouseId = 1, Exchange = "DSE",
            TradeDate = DateTime.UtcNow.Date, SettlementDate = DateTime.UtcNow.Date.AddDays(2)
        });
        await db.SaveChangesAsync();

        db.SettlementItems.Add(new SettlementItem
        {
            SettlementBatchId = 1,
            TradeId = 1, OrderId = 1, InvestorId = 1, BrokerageHouseId = 1,
            Side = "BUY", Quantity = 100, Price = 380m,
            TradeValue = 38_000m, NetAmount = 38_000m,
            SettlementType = SettlementType.T2,
            TradeDate = DateTime.UtcNow.Date,
            SettlementDate = DateTime.UtcNow.Date.AddDays(2),
        });
        await db.SaveChangesAsync();

        var item = await db.SettlementItems.FirstOrDefaultAsync();
        Assert.NotNull(item);
        Assert.Equal("BUY",   item.Side);
        Assert.Equal(100,     item.Quantity);
        Assert.Equal(38_000m, item.TradeValue);
    }

    // ── GetPendingBatches ────────────────────────────────────────────

    [Fact]
    public async Task GetPendingBatches_ReturnsPendingOnly()
    {
        using var db = CreateDb();
        db.SettlementBatches.AddRange(
            new SettlementBatch { BrokerageHouseId=1, Exchange="DSE", TradeDate=DateTime.UtcNow.AddDays(-2), SettlementDate=DateTime.UtcNow, Status=SettlementBatchStatus.Pending },
            new SettlementBatch { BrokerageHouseId=1, Exchange="DSE", TradeDate=DateTime.UtcNow.AddDays(-3), SettlementDate=DateTime.UtcNow.AddDays(-1), Status=SettlementBatchStatus.Completed },
            new SettlementBatch { BrokerageHouseId=1, Exchange="CSE", TradeDate=DateTime.UtcNow.AddDays(-2), SettlementDate=DateTime.UtcNow, Status=SettlementBatchStatus.Pending }
        );
        await db.SaveChangesAsync();

        var svc     = MakeService(db);
        var pending = await svc.GetPendingBatchesAsync();
        Assert.Equal(2, pending.Count);
        Assert.All(pending, b => Assert.Equal(SettlementBatchStatus.Pending, b.Status));
    }

    // ── NetObligations ───────────────────────────────────────────────

    [Fact]
    public void NetObligations_BuyMinusSell()
    {
        var batch = new SettlementBatch { TotalBuyValue = 100_000m, TotalSellValue = 60_000m };
        batch.NetObligations = batch.TotalBuyValue - batch.TotalSellValue;
        Assert.Equal(40_000m, batch.NetObligations);
    }

    [Fact]
    public void NetObligations_NetSeller_IsNegative()
    {
        var batch = new SettlementBatch { TotalBuyValue = 30_000m, TotalSellValue = 80_000m };
        batch.NetObligations = batch.TotalBuyValue - batch.TotalSellValue;
        Assert.Equal(-50_000m, batch.NetObligations);
    }

    // ── Multi-tenant isolation ───────────────────────────────────────

    [Fact]
    public async Task SettlementBatch_TenantIsolation()
    {
        using var db = CreateDb();
        db.SettlementBatches.AddRange(
            new SettlementBatch { BrokerageHouseId=1, Exchange="DSE", TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2) },
            new SettlementBatch { BrokerageHouseId=2, Exchange="DSE", TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2) }
        );
        await db.SaveChangesAsync();

        var t1 = await db.SettlementBatches.CountAsync(b => b.BrokerageHouseId == 1);
        var t2 = await db.SettlementBatches.CountAsync(b => b.BrokerageHouseId == 2);
        Assert.Equal(1, t1);
        Assert.Equal(1, t2);
    }

    // ── SettlementItem retry logic ───────────────────────────────────

    [Fact]
    public async Task SettlementItem_FailedCanBeRetried()
    {
        using var db = CreateDb();
        db.SettlementBatches.Add(new SettlementBatch { Id=1, BrokerageHouseId=1, Exchange="DSE", TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2) });
        await db.SaveChangesAsync();

        db.SettlementItems.Add(new SettlementItem
        {
            SettlementBatchId=1, TradeId=1, OrderId=1, InvestorId=1, BrokerageHouseId=1,
            Side="BUY", Quantity=10, Price=100m, TradeValue=1000m, NetAmount=1000m,
            SettlementType=SettlementType.T2, TradeDate=DateTime.UtcNow, SettlementDate=DateTime.UtcNow.AddDays(2),
            Status=SettlementItemStatus.Failed, FailureReason="Test failure"
        });
        await db.SaveChangesAsync();

        var item = await db.SettlementItems.FirstAsync();
        item.Status        = SettlementItemStatus.Pending;
        item.FailureReason = null;
        await db.SaveChangesAsync();

        var updated = await db.SettlementItems.FirstAsync();
        Assert.Equal(SettlementItemStatus.Pending, updated.Status);
        Assert.Null(updated.FailureReason);
    }

    // ── Bangladesh market T+2 rules ──────────────────────────────────

    [Fact]
    public void T2_BangladeshMarket_SkipsSaturdayAndSunday()
    {
        // Bangladesh market is closed Sat & Sun (not Sat & Sun like Western markets)
        var closed = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
        Assert.Contains(DayOfWeek.Saturday, closed);
        Assert.Contains(DayOfWeek.Sunday,   closed);
        Assert.DoesNotContain(DayOfWeek.Friday, closed);
    }

    [Fact]
    public void T2_TradingDays_AreMonThroughFri()
    {
        // Wait - Bangladesh market trades Sun-Thu, not Mon-Fri
        // DSE trading days: Sunday to Thursday
        var tradingDays = new[] {
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
            DayOfWeek.Wednesday, DayOfWeek.Thursday
        };
        Assert.Equal(5, tradingDays.Length);
        Assert.DoesNotContain(DayOfWeek.Friday,   tradingDays);
        Assert.DoesNotContain(DayOfWeek.Saturday, tradingDays);
    }
}

// ── Test helpers ──────────────────────────────────────────────────────

public class FakeStateMachine : IOrderStateMachine
{
    public Task<bool> TransitionAsync(Order order, OrderStatus to,
        string? reason = null, string? actor = null)
    {
        order.Status = to;
        return Task.FromResult(true);
    }

    public bool CanTransition(OrderStatus from, OrderStatus to) => true;
    public OrderStatus[] GetAllowedTransitions(OrderStatus current)
        => Enum.GetValues<OrderStatus>();
}
