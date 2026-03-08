using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class RMSValidationTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private RMSValidationService CreateService(AppDbContext db)
    {
        var auditMock = new Mock<IAuditService>();
        auditMock.Setup(x => x.LogAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>())).Returns(Task.CompletedTask);
        return new RMSValidationService(db, auditMock.Object);
    }

    private async Task SeedBaseDataAsync(AppDbContext db)
    {
        db.Roles.Add(new Role { Id = 1, Name = "Investor" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test Brokerage", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 1, FullName = "Test Investor", Email = "investor@test.com",
            PasswordHash = "hash", Phone = "01700000000",
            RoleId = 1, BrokerageHouseId = 1
        });
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "BRAC", CompanyName = "BRAC Bank",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 50m
        });
        await db.SaveChangesAsync();
    }

    // ── ORDER VALUE LIMIT ─────────────────────────────────────

    [Fact]
    public async Task CheckOrderValue_BelowLimit_IsAllowed()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CheckOrderValueLimitAsync(1, 1_000_000m, 1);
        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CheckOrderValue_ExceedsDefaultLimit_IsBlocked()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CheckOrderValueLimitAsync(1, 6_000_000m, 1);
        Assert.NotEmpty(result.Violations);
        Assert.NotEmpty(result.Violations);
    }

    [Fact]
    public async Task CheckOrderValue_Above80PctLimit_HasWarning()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        // 4.5M is above 80% of 5M default limit
        var result = await svc.CheckOrderValueLimitAsync(1, 4_500_000m, 1);
        Assert.NotEmpty(result.Warnings);
    }

    [Fact]
    public async Task CheckOrderValue_CustomRMSLimit_UsesCustomLimit()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);
        db.RMSLimits.Add(new RMSLimit
        {
            Level            = RMSLevel.Investor,
            EntityId         = 1,
            EntityType       = "User",
            BrokerageHouseId = 1,
            MaxOrderValue    = 2_000_000m,
            MaxDailyValue    = 10_000_000m,
            MaxExposure      = 20_000_000m,
            ConcentrationPct = 10m,
            IsActive         = true
        });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        // 3M exceeds custom 2M limit
        var result = await svc.CheckOrderValueLimitAsync(1, 3_000_000m, 1);
        Assert.NotEmpty(result.Violations);
    }

    // ── DAILY EXPOSURE ────────────────────────────────────────

    [Fact]
    public async Task CheckDailyExposure_NoOrdersToday_IsAllowed()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CheckDailyExposureAsync(1, 1_000_000m, 1);
        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CheckDailyExposure_ExceedsLimit_IsBlocked()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);

        // Add existing orders today
        db.Orders.Add(new Order
        {
            InvestorId       = 1, TraderId = 1, StockId = 1,
            BrokerageHouseId = 1, OrderType = OrderType.Buy,
            OrderCategory    = OrderCategory.Market, Quantity = 1000,
            PriceAtOrder     = 15_000m, Status = OrderStatus.Executed,
            SettlementType   = SettlementType.T2,
            PlacedBy         = PlacedByRole.Investor,
            CreatedAt        = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        // 15M existing + 6M new = 21M > 20M default limit
        var result = await svc.CheckDailyExposureAsync(1, 6_000_000m, 1);
        Assert.NotEmpty(result.Violations);
    }

    // ── CONCENTRATION ─────────────────────────────────────────

    [Fact]
    public async Task CheckConcentration_EmptyPortfolio_IsAllowed()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CheckConcentrationAsync(1, 1, 100_000m);
        Assert.True(result.IsAllowed);
        Assert.Empty(result.Violations);
    }

    [Fact]
    public async Task CheckConcentration_ExceedsLimit_IsBlocked()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);

        // Portfolio of 1M total, all in stock 1
        db.Portfolios.Add(new Portfolio
        {
            InvestorId = 1, StockId = 1, Quantity = 1000,
            AverageBuyPrice = 900m, BrokerageHouseId = 1
        });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        // Buying 200K more would push concentration way above 10%
        var result = await svc.CheckConcentrationAsync(1, 1, 200_000m);
        Assert.NotEmpty(result.Violations);
    }

    // ── FULL VALIDATION ───────────────────────────────────────

    [Fact]
    public async Task ValidateOrder_ValidOrder_IsAllowed()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.ValidateOrderAsync(1, 1, "DSE", 500_000m, "BUY", 1);
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task ValidateOrder_ExceedsOrderLimit_IsBlocked()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.ValidateOrderAsync(1, 1, "DSE", 10_000_000m, "BUY", 1);
        Assert.False(result.IsAllowed);
        Assert.Equal(RMSAction.Block, result.Action);
    }

    [Fact]
    public async Task ValidateOrder_SellOrder_SkipsConcentrationCheck()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        // Even large sell order should not trigger concentration check
        var result = await svc.ValidateOrderAsync(1, 1, "DSE", 1_000_000m, "SELL", 1);
        Assert.True(result.IsAllowed);
    }

    [Fact]
    public async Task RMSValidationResult_MultipleViolations_AllCaptured()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);
        db.RMSLimits.Add(new RMSLimit
        {
            Level = RMSLevel.Investor, EntityId = 1,
            EntityType = "User", BrokerageHouseId = 1,
            MaxOrderValue = 100_000m, MaxDailyValue = 200_000m,
            MaxExposure = 500_000m, ConcentrationPct = 5m,
            IsActive = true
        });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var result = await svc.ValidateOrderAsync(1, 1, "DSE", 500_000m, "BUY", 1);
        Assert.False(result.IsAllowed);
        Assert.True(result.Violations.Count >= 1);
    }
}
