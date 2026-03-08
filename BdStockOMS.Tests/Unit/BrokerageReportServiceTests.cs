using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Reports;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class BrokerageReportServiceTests
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
        db.Roles.AddRange(investorRole, traderRole);

        var bh = new BrokerageHouse
        {
            Id = 1, Name = "Test Brokerage", LicenseNumber = "TB001",
            Email = "bh@test.com", Phone = "01000000000",
            Address = "Dhaka", IsActive = true
        };
        db.BrokerageHouses.Add(bh);

        var stock = new Stock
        {
            Id = 1, TradingCode = "GP", CompanyName = "Grameenphone",
            Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1,
            LastTradePrice = 400m, CircuitBreakerHigh = 440m,
            CircuitBreakerLow = 360m, IsActive = true, LastUpdatedAt = DateTime.UtcNow
        };
        db.Stocks.Add(stock);

        var trader = new User
        {
            Id = 1, FullName = "Trader One", Email = "trader@test.com",
            PasswordHash = "hash", RoleId = 6, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 0
        };
        var investor1 = new User
        {
            Id = 2, FullName = "Investor One", Email = "inv1@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 100000m
        };
        var investor2 = new User
        {
            Id = 3, FullName = "Investor Two", Email = "inv2@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 50000m
        };
        db.Users.AddRange(trader, investor1, investor2);

        // Orders this month
        var now = DateTime.UtcNow;
        db.Orders.AddRange(
            new Order { InvestorId = 2, TraderId = 1, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Buy,  OrderCategory = OrderCategory.Limit, Quantity = 100, PriceAtOrder = 400m, SettlementType = SettlementType.T2, PlacedBy = PlacedByRole.Trader, Status = OrderStatus.Executed,  CreatedAt = now },
            new Order { InvestorId = 2, TraderId = 1, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Sell, OrderCategory = OrderCategory.Limit, Quantity = 50,  PriceAtOrder = 410m, SettlementType = SettlementType.T2, PlacedBy = PlacedByRole.Trader, Status = OrderStatus.Executed,  CreatedAt = now },
            new Order { InvestorId = 3, TraderId = 1, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Buy,  OrderCategory = OrderCategory.Limit, Quantity = 200, PriceAtOrder = 400m, SettlementType = SettlementType.T2, PlacedBy = PlacedByRole.Trader, Status = OrderStatus.Pending,   CreatedAt = now },
            new Order { InvestorId = 3, TraderId = 1, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Buy,  OrderCategory = OrderCategory.Limit, Quantity = 100, PriceAtOrder = 400m, SettlementType = SettlementType.T2, PlacedBy = PlacedByRole.Trader, Status = OrderStatus.Cancelled, CreatedAt = now }
        );

        // Fund requests
        db.FundRequests.AddRange(
            new FundRequest { InvestorId = 2, BrokerageHouseId = 1, Amount = 50000m, PaymentMethod = PaymentMethod.Cash,   Status = FundRequestStatus.Completed, CreatedAt = now },
            new FundRequest { InvestorId = 3, BrokerageHouseId = 1, Amount = 30000m, PaymentMethod = PaymentMethod.BEFTN,  Status = FundRequestStatus.Completed, CreatedAt = now },
            new FundRequest { InvestorId = 2, BrokerageHouseId = 1, Amount = 10000m, PaymentMethod = PaymentMethod.Cheque, Status = FundRequestStatus.Pending,   CreatedAt = now }
        );

        await db.SaveChangesAsync();
        return db;
    }

    // ── ORDER SUMMARY TESTS ────────────────────────

    [Fact]
    public async Task GetOrderSummary_ValidBrokerage_ReturnsCorrectCounts()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetOrderSummaryAsync(1, new ReportQueryDto());

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.TotalOrders);
        Assert.Equal(3, result.Value.BuyOrders);
        Assert.Equal(1, result.Value.SellOrders);
    }

    [Fact]
    public async Task GetOrderSummary_CountsStatusesCorrectly()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetOrderSummaryAsync(1, new ReportQueryDto());

        Assert.Equal(2, result.Value!.ExecutedOrders);
        Assert.Equal(1, result.Value.PendingOrders);
        Assert.Equal(1, result.Value.CancelledOrders);
    }

    [Fact]
    public async Task GetOrderSummary_CalculatesTotalOrderValueFromExecutedOnly()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetOrderSummaryAsync(1, new ReportQueryDto());

        // Executed: 100×400 + 50×410 = 40000 + 20500 = 60500
        Assert.Equal(60500m, result.Value!.TotalOrderValue);
    }

    [Fact]
    public async Task GetOrderSummary_BrokerageNotFound_ReturnsFail()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetOrderSummaryAsync(999, new ReportQueryDto());

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task GetOrderSummary_DateRangeFilter_ReturnsFilteredOrders()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        // Filter to future dates — should return 0 orders
        var result = await svc.GetOrderSummaryAsync(1, new ReportQueryDto
        {
            FromDate = DateTime.UtcNow.AddDays(1),
            ToDate   = DateTime.UtcNow.AddDays(30)
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalOrders);
    }

    // ── TOP INVESTORS TESTS ────────────────────────

    [Fact]
    public async Task GetTopInvestors_ReturnsRankedByTradedValue()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetTopInvestorsAsync(1, new ReportQueryDto(), 10);

        Assert.True(result.IsSuccess);
        // Investor1 has more executed value (60500) than Investor2 (0)
        Assert.Equal(2, result.Value!.First().InvestorId);
    }

    [Fact]
    public async Task GetTopInvestors_RespectsTopLimit()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetTopInvestorsAsync(1, new ReportQueryDto(), 1);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    [Fact]
    public async Task GetTopInvestors_BrokerageNotFound_ReturnsFail()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetTopInvestorsAsync(999, new ReportQueryDto(), 10);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    // ── COMMISSION REPORT TESTS ────────────────────

    [Fact]
    public async Task GetCommissionReport_CalculatesEstimatedCommission()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetCommissionReportAsync(1, new ReportQueryDto());

        Assert.True(result.IsSuccess);
        // TotalTradedValue = 60500, Commission = 60500 × 0.005 = 302.5
        Assert.Equal(60500m, result.Value!.TotalTradedValue);
        Assert.Equal(302.50m, result.Value.EstimatedCommission);
    }

    [Fact]
    public async Task GetCommissionReport_BrokerageNotFound_ReturnsFail()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetCommissionReportAsync(999, new ReportQueryDto());

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    // ── FUND REQUEST REPORT TESTS ──────────────────

    [Fact]
    public async Task GetFundRequestReport_ReturnsCorrectCounts()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetFundRequestReportAsync(1, new ReportQueryDto());

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalRequests);
        Assert.Equal(2, result.Value.CompletedRequests);
        Assert.Equal(1, result.Value.PendingRequests);
    }

    [Fact]
    public async Task GetFundRequestReport_SumsCompletedAmount()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetFundRequestReportAsync(1, new ReportQueryDto());

        // Completed: 50000 + 30000 = 80000
        Assert.Equal(80000m, result.Value!.TotalCompletedAmount);
    }

    [Fact]
    public async Task GetFundRequestReport_BrokerageNotFound_ReturnsFail()
    {
        var db = await SeedAsync();
        var svc = new BrokerageReportService(db);

        var result = await svc.GetFundRequestReportAsync(999, new ReportQueryDto());

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }
}
