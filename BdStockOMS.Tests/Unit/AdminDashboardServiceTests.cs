using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class AdminDashboardServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private async Task<AppDbContext> SeedAsync()
    {
        var db = CreateDb();

        var investorRole = new Role { Id = 7, Name = "Investor" };
        var traderRole   = new Role { Id = 6, Name = "Trader" };
        var ccdRole      = new Role { Id = 4, Name = "CCD" };
        var adminRole    = new Role { Id = 3, Name = "Admin" };
        db.Roles.AddRange(investorRole, traderRole, ccdRole, adminRole);

        var bh1 = new BrokerageHouse { Id = 1, Name = "Active BH",   LicenseNumber = "BH001", Email = "bh1@test.com", Phone = "01000000001", Address = "Dhaka", IsActive = true };
        var bh2 = new BrokerageHouse { Id = 2, Name = "Inactive BH", LicenseNumber = "BH002", Email = "bh2@test.com", Phone = "01000000002", Address = "Dhaka", IsActive = false };
        db.BrokerageHouses.AddRange(bh1, bh2);

        var stock = new Stock
        {
            Id = 1, TradingCode = "GP", CompanyName = "Grameenphone",
            Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1,
            LastTradePrice = 400m, CircuitBreakerHigh = 440m,
            CircuitBreakerLow = 360m, IsActive = true, LastUpdatedAt = DateTime.UtcNow
        };
        db.Stocks.Add(stock);

        var trader    = new User { Id = 1, FullName = "Trader",    Email = "trader@test.com",    PasswordHash = "hash", RoleId = 6, BrokerageHouseId = 1, IsActive = true,  CashBalance = 0 };
        var investor1 = new User { Id = 2, FullName = "Investor1", Email = "investor1@test.com", PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1, IsActive = true,  CashBalance = 100000m };
        var investor2 = new User { Id = 3, FullName = "Investor2", Email = "investor2@test.com", PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1, IsActive = false, CashBalance = 50000m, IsLocked = true };
        var ccd       = new User { Id = 4, FullName = "CCD User",  Email = "ccd@test.com",       PasswordHash = "hash", RoleId = 4, BrokerageHouseId = 1, IsActive = true,  CashBalance = 0 };
        db.Users.AddRange(trader, investor1, investor2, ccd);

        var now = DateTime.UtcNow;

        // Orders — mix of today and older
        db.Orders.AddRange(
            new Order { InvestorId = 2, TraderId = 1, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Buy,  OrderCategory = OrderCategory.Limit, Quantity = 100, PriceAtOrder = 400m, SettlementType = SettlementType.T2, PlacedBy = PlacedByRole.Trader, Status = OrderStatus.Executed,  CreatedAt = now },
            new Order { InvestorId = 2, TraderId = 1, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Sell, OrderCategory = OrderCategory.Limit, Quantity = 50,  PriceAtOrder = 410m, SettlementType = SettlementType.T2, PlacedBy = PlacedByRole.Trader, Status = OrderStatus.Pending,   CreatedAt = now },
            new Order { InvestorId = 3, TraderId = 1, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Buy,  OrderCategory = OrderCategory.Limit, Quantity = 200, PriceAtOrder = 400m, SettlementType = SettlementType.T2, PlacedBy = PlacedByRole.Trader, Status = OrderStatus.Executed,  CreatedAt = now.AddDays(-5) }
        );

        // Fund requests
        db.FundRequests.AddRange(
            new FundRequest { InvestorId = 2, BrokerageHouseId = 1, Amount = 50000m, PaymentMethod = PaymentMethod.Cash,  Status = FundRequestStatus.Completed, CreatedAt = now },
            new FundRequest { InvestorId = 3, BrokerageHouseId = 1, Amount = 30000m, PaymentMethod = PaymentMethod.BEFTN, Status = FundRequestStatus.Pending,   CreatedAt = now }
        );

        await db.SaveChangesAsync();
        return db;
    }

    // ── USER STATS TESTS ───────────────────────────

    [Fact]
    public async Task GetUserStats_ReturnsCorrectTotals()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetUserStatsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.TotalUsers);
        Assert.Equal(2, result.Value.TotalInvestors);
        Assert.Equal(1, result.Value.TotalTraders);
        Assert.Equal(1, result.Value.TotalCCDs);
    }

    [Fact]
    public async Task GetUserStats_CountsActiveAndLockedCorrectly()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetUserStatsAsync();

        Assert.Equal(3, result.Value!.ActiveUsers);
        Assert.Equal(1, result.Value.LockedUsers);
    }

    // ── ORDER STATS TESTS ──────────────────────────

    [Fact]
    public async Task GetOrderStats_CountsTodayOrdersOnly()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetOrderStatsAsync();

        Assert.True(result.IsSuccess);
        // Only 2 orders today, 1 is from 5 days ago
        Assert.Equal(2, result.Value!.TotalOrdersToday);
    }

    [Fact]
    public async Task GetOrderStats_CountsAllMonthOrders()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetOrderStatsAsync();

        Assert.Equal(3, result.Value!.TotalOrdersThisMonth);
    }

    [Fact]
    public async Task GetOrderStats_CountsPendingOrders()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetOrderStatsAsync();

        Assert.Equal(1, result.Value!.PendingOrders);
    }

    [Fact]
    public async Task GetOrderStats_CalculatesTodayTradedValue()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetOrderStatsAsync();

        // Only today's executed order: 100 × 400 = 40000
        Assert.Equal(40000m, result.Value!.TotalTradedValueToday);
    }

    // ── FUND REQUEST STATS TESTS ───────────────────

    [Fact]
    public async Task GetFundRequestStats_CountsPendingCorrectly()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetFundRequestStatsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.PendingApproval);
    }

    [Fact]
    public async Task GetFundRequestStats_SumsCompletedToday()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetFundRequestStatsAsync();

        Assert.Equal(50000m, result.Value!.TotalDepositedToday);
    }

    // ── SYSTEM STATS TESTS ─────────────────────────

    [Fact]
    public async Task GetSystemStats_CountsBrokerageHousesCorrectly()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetSystemStatsAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalBrokerageHouses);
        Assert.Equal(1, result.Value.ActiveBrokerageHouses);
    }

    [Fact]
    public async Task GetSystemStats_CountsStocksCorrectly()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetSystemStatsAsync();

        Assert.Equal(1, result.Value!.TotalStocks);
        Assert.Equal(1, result.Value.ActiveStocks);
    }

    // ── FULL DASHBOARD TESTS ───────────────────────

    [Fact]
    public async Task GetDashboard_ReturnsAllSections()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetDashboardAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value!.Users);
        Assert.NotNull(result.Value.Orders);
        Assert.NotNull(result.Value.FundRequests);
        Assert.NotNull(result.Value.System);
        Assert.NotNull(result.Value.RecentActivity);
    }

    [Fact]
    public async Task GetRecentActivity_ReturnsOrdersAndFundRequests()
    {
        var db = await SeedAsync();
        var svc = new AdminDashboardService(db);

        var result = await svc.GetRecentActivityAsync(10);

        Assert.True(result.IsSuccess);
        Assert.Contains(result.Value!, a => a.Type == "Order");
        Assert.Contains(result.Value!, a => a.Type == "FundRequest");
    }
}
