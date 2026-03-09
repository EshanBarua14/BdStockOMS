using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using BdStockOMS.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class RMSEngineTests
{
    private static AppDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static RMSValidationService BuildService(AppDbContext db)
    {
        var auditMock = new Mock<IAuditService>();
        auditMock.Setup(a => a.LogAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>()))
            .Returns(Task.CompletedTask);

        var clientsMock = new Mock<IHubClients>();
        var clientProxyMock = new Mock<IClientProxy>();
        clientsMock.Setup(c => c.Group(It.IsAny<string>())).Returns(clientProxyMock.Object);

        var hubMock = new Mock<IHubContext<NotificationHub>>();
        hubMock.Setup(h => h.Clients).Returns(clientsMock.Object);

        return new RMSValidationService(db, auditMock.Object, hubMock.Object);
    }

    private static User BuildInvestor(int id, decimal cashBalance = 100_000m) => new()
    {
        Id               = id,
        FullName         = $"Investor {id}",
        Email            = $"investor{id}@test.com",
        PasswordHash     = "hash",
        BrokerageHouseId = 1,
        RoleId           = 1,
        CashBalance      = cashBalance,
    };

    // ── CheckCashBalanceAsync ─────────────────────────────────────────────
    [Fact]
    public async Task CheckCashBalance_Passes_WhenSufficientBalance()
    {
        var db = BuildDb();
        db.Users.Add(BuildInvestor(1, cashBalance: 50_000m));
        await db.SaveChangesAsync();

        var svc    = BuildService(db);
        var result = await svc.CheckCashBalanceAsync(1, 30_000m, 1);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CheckCashBalance_Blocks_WhenInsufficientBalance()
    {
        var db = BuildDb();
        db.Users.Add(BuildInvestor(1, cashBalance: 10_000m));
        await db.SaveChangesAsync();

        var svc    = BuildService(db);
        var result = await svc.CheckCashBalanceAsync(1, 50_000m, 1);

        Assert.False(result.IsAllowed);
        Assert.Single(result.Violations);
        Assert.Contains("Insufficient cash balance", result.Violations[0]);
    }

    [Fact]
    public async Task CheckCashBalance_Warns_WhenBalanceIsLow()
    {
        var db = BuildDb();
        db.Users.Add(BuildInvestor(1, cashBalance: 10_500m));
        await db.SaveChangesAsync();

        var svc    = BuildService(db);
        // order value = 10_000, balance = 10_500 (< 10_000 * 1.1 = 11_000) → warning
        var result = await svc.CheckCashBalanceAsync(1, 10_000m, 1);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
        Assert.Single(result.Warnings);
    }

    [Fact]
    public async Task CheckCashBalance_ReturnsZeroBalance_WhenUserNotFound()
    {
        var db     = BuildDb();
        var svc    = BuildService(db);
        var result = await svc.CheckCashBalanceAsync(999, 1_000m, 1);

        Assert.False(result.IsAllowed);
        Assert.Contains("Insufficient cash balance", result.Violations[0]);
    }

    // ── CheckOrderValueLimitAsync ─────────────────────────────────────────
    [Fact]
    public async Task CheckOrderValueLimit_Passes_WhenUnderLimit()
    {
        var db     = BuildDb();
        var svc    = BuildService(db);
        var result = await svc.CheckOrderValueLimitAsync(1, 1_000_000m, 1);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CheckOrderValueLimit_Blocks_WhenOverLimit()
    {
        var db     = BuildDb();
        var svc    = BuildService(db);
        var result = await svc.CheckOrderValueLimitAsync(1, 6_000_000m, 1);

        Assert.False(result.IsAllowed);
        Assert.Single(result.Violations);
        Assert.Contains("exceeds limit", result.Violations[0]);
    }

    [Fact]
    public async Task CheckOrderValueLimit_Warns_WhenAbove80Pct()
    {
        var db     = BuildDb();
        var svc    = BuildService(db);
        // Default limit = 5_000_000, 80% = 4_000_000
        var result = await svc.CheckOrderValueLimitAsync(1, 4_500_000m, 1);

        Assert.True(result.IsAllowed);
        Assert.Single(result.Warnings);
    }

    // ── CheckDailyExposureAsync ───────────────────────────────────────────
    [Fact]
    public async Task CheckDailyExposure_Passes_WhenNoPriorOrders()
    {
        var db     = BuildDb();
        var svc    = BuildService(db);
        var result = await svc.CheckDailyExposureAsync(1, 1_000_000m, 1);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CheckDailyExposure_Blocks_WhenExceedsLimit()
    {
        var db = BuildDb();
        // Add today's orders totalling 19_000_000
        db.Orders.Add(new Order
        {
            InvestorId       = 1, StockId = 1, BrokerageHouseId = 1,
            Quantity         = 1, PriceAtOrder = 19_000_000m,
            Status           = OrderStatus.Pending,
            OrderType        = OrderType.Buy,
            OrderCategory    = OrderCategory.Limit,
            SettlementType   = SettlementType.T2,
            PlacedBy         = PlacedByRole.Investor,
            CreatedAt        = DateTime.UtcNow,
        });
        await db.SaveChangesAsync();

        var svc    = BuildService(db);
        var result = await svc.CheckDailyExposureAsync(1, 2_000_000m, 1);

        Assert.False(result.IsAllowed);
        Assert.Contains("Daily exposure", result.Violations[0]);
    }

    // ── CheckConcentrationAsync ───────────────────────────────────────────
    [Fact]
    public async Task CheckConcentration_Passes_WhenNoPortfolio()
    {
        var db     = BuildDb();
        var svc    = BuildService(db);
        var result = await svc.CheckConcentrationAsync(1, 1, 10_000m);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CheckConcentration_Blocks_WhenExceedsLimit()
    {
        var db = BuildDb();
        // Portfolio: 1 stock worth 90_000, adding 20_000 to same stock
        db.Portfolios.Add(new Portfolio
        {
            InvestorId = 1, StockId = 2, BrokerageHouseId = 1,
            Quantity = 1, AverageBuyPrice = 90_000m,
        });
        await db.SaveChangesAsync();

        var svc    = BuildService(db);
        // projectedHolding = 20_000, totalPortfolio = 90_000+20_000=110_000
        // pct = 20_000/110_000 = 18.18% > 10% default
        var result = await svc.CheckConcentrationAsync(1, 1, 20_000m);

        Assert.False(result.IsAllowed);
        Assert.Contains("concentration", result.Violations[0]);
    }

    // ── CheckSectorConcentrationAsync ─────────────────────────────────────
    [Fact]
    public async Task CheckSectorConcentration_Passes_WhenNoPortfolio()
    {
        var db = BuildDb();
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "TEST", CompanyName = "Test Co",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 100m,
        });
        await db.SaveChangesAsync();

        var svc    = BuildService(db);
        var result = await svc.CheckSectorConcentrationAsync(1, 1, 10_000m);

        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    // ── ValidateOrderAsync ────────────────────────────────────────────────
    [Fact]
    public async Task ValidateOrder_Blocks_WhenInsufficientCash_BuyOrder()
    {
        var db = BuildDb();
        db.Users.Add(BuildInvestor(1, cashBalance: 100m));
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "TEST", CompanyName = "Test",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 100m,
        });
        await db.SaveChangesAsync();

        var svc    = BuildService(db);
        var result = await svc.ValidateOrderAsync(1, 1, "DSE", 50_000m, "BUY", 1);

        Assert.False(result.IsAllowed);
        Assert.Equal(RMSAction.Block, result.Action);
    }

    [Fact]
    public async Task ValidateOrder_Passes_WhenAllChecksPass_SellOrder()
    {
        var db = BuildDb();
        db.Users.Add(BuildInvestor(1, cashBalance: 0m));
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "TEST", CompanyName = "Test",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 100m,
        });
        await db.SaveChangesAsync();

        var svc    = BuildService(db);
        // Sell order — cash check skipped
        var result = await svc.ValidateOrderAsync(1, 1, "DSE", 10_000m, "SELL", 1);

        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ValidateOrder_CreatesTradeAlert_WhenBlocked()
    {
        var db = BuildDb();
        db.Users.Add(BuildInvestor(1, cashBalance: 0m));
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "TEST", CompanyName = "Test",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 100m,
        });
        await db.SaveChangesAsync();

        var svc = BuildService(db);
        await svc.ValidateOrderAsync(1, 1, "DSE", 50_000m, "BUY", 1);

        var alert = db.TradeAlerts.FirstOrDefault();
        Assert.NotNull(alert);
        Assert.Equal(TradeAlertSeverity.Breach, alert!.Severity);
    }

    // ── TradeAlert model ──────────────────────────────────────────────────
    [Fact]
    public void TradeAlert_DefaultIsAcknowledged_IsFalse()
    {
        var alert = new TradeAlert();
        Assert.False(alert.IsAcknowledged);
        Assert.Null(alert.AcknowledgedAt);
    }

    [Fact]
    public void TradeAlert_CreatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var alert  = new TradeAlert();
        Assert.True(alert.CreatedAt >= before);
    }
}
