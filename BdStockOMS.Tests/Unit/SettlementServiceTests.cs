using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class SettlementServiceTests
{
    private static AppDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static SettlementService BuildService(AppDbContext db)
    {
        var smMock = new Mock<IOrderStateMachine>();
        smMock.Setup(s => s.TransitionAsync(
            It.IsAny<Order>(), It.IsAny<OrderStatus>(),
            It.IsAny<string?>(), It.IsAny<string?>()))
            .ReturnsAsync(true);
        return new SettlementService(db, smMock.Object);
    }

    private static Trade BuildTrade(int id, string side, decimal price = 100m, int qty = 100) => new()
    {
        Id               = id,
        OrderId          = id,
        InvestorId       = 1,
        BrokerageHouseId = 1,
        StockId          = 1,
        Side             = side,
        Quantity         = qty,
        Price            = price,
        TotalValue       = price * qty,
        Status           = TradeStatus.Filled,
        TradedAt         = DateTime.UtcNow,
    };

    // ── CalculateSettlementDate ───────────────────────────────────────────
    [Fact]
    public void CalculateSettlementDate_T0_ReturnsSameDay()
    {
        var svc  = BuildService(BuildDb());
        var date = new DateTime(2024, 1, 15); // Monday
        var result = svc.CalculateSettlementDate(date, SettlementType.T0);
        Assert.Equal(date.Date, result);
    }

    [Fact]
    public void CalculateSettlementDate_T2_SkipsWeekend_FromThursday()
    {
        var svc      = BuildService(BuildDb());
        var thursday = new DateTime(2024, 1, 11); // Thursday
        var result   = svc.CalculateSettlementDate(thursday, SettlementType.T2);
        // +1 = Friday, +2 = skip Sat/Sun → Monday Jan 15
        Assert.Equal(new DateTime(2024, 1, 15), result);
    }

    [Fact]
    public void CalculateSettlementDate_T2_FromMonday_IsWednesday()
    {
        var svc    = BuildService(BuildDb());
        var monday = new DateTime(2024, 1, 8); // Monday
        var result = svc.CalculateSettlementDate(monday, SettlementType.T2);
        Assert.Equal(new DateTime(2024, 1, 10), result); // Wednesday
    }

    [Fact]
    public void CalculateSettlementDate_T2_FromFriday_IsNextTuesday()
    {
        var svc    = BuildService(BuildDb());
        var friday = new DateTime(2024, 1, 12); // Friday
        var result = svc.CalculateSettlementDate(friday, SettlementType.T2);
        // +1 = skip Sat, +2 = skip Sun → Mon Jan 15, Tue Jan 16
        Assert.Equal(new DateTime(2024, 1, 16), result);
    }

    [Fact]
    public void CalculateSettlementDate_T2_NeverLandsOnWeekend()
    {
        var svc = BuildService(BuildDb());
        for (int i = 0; i < 14; i++)
        {
            var date   = new DateTime(2024, 1, 1).AddDays(i);
            var result = svc.CalculateSettlementDate(date, SettlementType.T2);
            Assert.NotEqual(DayOfWeek.Saturday, result.DayOfWeek);
            Assert.NotEqual(DayOfWeek.Sunday,   result.DayOfWeek);
        }
    }

    // ── CreateBatchAsync ──────────────────────────────────────────────────
    [Fact]
    public async Task CreateBatch_ReturnsEmptyBatch_WhenNoTrades()
    {
        var db  = BuildDb();
        var svc = BuildService(db);

        var batch = await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);

        Assert.NotNull(batch);
        Assert.Equal(0, batch.TotalTrades);
        Assert.Equal(0m, batch.TotalBuyValue);
        Assert.Equal(0m, batch.TotalSellValue);
    }

    [Fact]
    public async Task CreateBatch_CountsTrades_Correctly()
    {
        var db = BuildDb();
        db.Trades.Add(BuildTrade(1, "BUY"));
        db.Trades.Add(BuildTrade(2, "SELL"));
        await db.SaveChangesAsync();

        var svc   = BuildService(db);
        var batch = await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);

        Assert.Equal(2, batch.TotalTrades);
    }

    [Fact]
    public async Task CreateBatch_CalculatesBuyAndSellValues()
    {
        var db = BuildDb();
        db.Trades.Add(BuildTrade(1, "BUY",  price: 100m, qty: 100)); // 10_000
        db.Trades.Add(BuildTrade(2, "SELL", price: 200m, qty: 50));  //  10_000
        await db.SaveChangesAsync();

        var svc   = BuildService(db);
        var batch = await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);

        Assert.Equal(10_000m, batch.TotalBuyValue);
        Assert.Equal(10_000m, batch.TotalSellValue);
        Assert.Equal(0m,      batch.NetObligations);
    }

    [Fact]
    public async Task CreateBatch_CreatesSettlementItems_ForEachTrade()
    {
        var db = BuildDb();
        db.Trades.Add(BuildTrade(1, "BUY"));
        db.Trades.Add(BuildTrade(2, "BUY"));
        await db.SaveChangesAsync();

        var svc   = BuildService(db);
        var batch = await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);
        var items = await svc.GetBatchItemsAsync(batch.Id);

        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task CreateBatch_PersistsToDB()
    {
        var db  = BuildDb();
        var svc = BuildService(db);

        await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);

        Assert.Equal(1, await db.SettlementBatches.CountAsync());
    }

    [Fact]
    public async Task CreateBatch_Status_IsPending()
    {
        var db    = BuildDb();
        var svc   = BuildService(db);
        var batch = await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);

        Assert.Equal(SettlementBatchStatus.Pending, batch.Status);
    }

    // ── ProcessBatchAsync ─────────────────────────────────────────────────
    [Fact]
    public async Task ProcessBatch_EmptyBatch_CompletesSuccessfully()
    {
        var db  = BuildDb();
        var svc = BuildService(db);

        var batch  = await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);
        var result = await svc.ProcessBatchAsync(batch.Id);

        Assert.Equal(SettlementBatchStatus.Completed, result.Status);
        Assert.NotNull(result.ProcessedAt);
    }

    [Fact]
    public async Task ProcessBatch_SetsItemsToSettled()
    {
        var db = BuildDb();
        db.Trades.Add(BuildTrade(1, "BUY"));
        await db.SaveChangesAsync();

        var svc   = BuildService(db);
        var batch = await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);
        await svc.ProcessBatchAsync(batch.Id);

        var items = await svc.GetBatchItemsAsync(batch.Id);
        Assert.All(items, i => Assert.Equal(SettlementItemStatus.Settled, i.Status));
    }

    [Fact]
    public async Task ProcessBatch_ThrowsForUnknownBatch()
    {
        var db  = BuildDb();
        var svc = BuildService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ProcessBatchAsync(999));
    }

    // ── GetPendingBatchesAsync ────────────────────────────────────────────
    [Fact]
    public async Task GetPendingBatches_ReturnsOnlyPendingBatches()
    {
        var db  = BuildDb();
        var svc = BuildService(db);

        var b1 = await svc.CreateBatchAsync(1, "DSE", DateTime.UtcNow);
        var b2 = await svc.CreateBatchAsync(1, "CSE", DateTime.UtcNow);
        await svc.ProcessBatchAsync(b1.Id); // completes b1

        var pending = await svc.GetPendingBatchesAsync();

        Assert.Single(pending);
        Assert.Equal(b2.Id, pending[0].Id);
    }

    // ── SettlementBatch model ─────────────────────────────────────────────
    [Fact]
    public void SettlementBatch_DefaultStatus_IsPending()
    {
        var batch = new SettlementBatch();
        Assert.Equal(SettlementBatchStatus.Pending, batch.Status);
    }

    [Fact]
    public void SettlementItem_DefaultStatus_IsPending()
    {
        var item = new SettlementItem();
        Assert.Equal(SettlementItemStatus.Pending, item.Status);
    }
}
