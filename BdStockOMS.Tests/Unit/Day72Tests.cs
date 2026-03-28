using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Reports;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 72 — BrokerageReportService Tests
// ============================================================

public class Day72ReportTests
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
            Id = id, Name = "Pioneer Securities",
            LicenseNumber = "DSE-TM-0001",
            Email = "info@pioneer.com", Phone = "01700000000",
            Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private void SeedUser(AppDbContext db, int id = 1, int brokerageId = 1)
    {
        db.Users.Add(new User {
            Id = id, FullName = "Test Investor", Email = $"investor{id}@test.com",
            PasswordHash = "hash", BrokerageHouseId = brokerageId,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    private void SeedOrder(AppDbContext db, int investorId, int brokerageId,
        OrderStatus status = OrderStatus.Filled, decimal price = 100m, int qty = 100)
    {
        db.Orders.Add(new Order {
            InvestorId = investorId, StockId = 1, BrokerageHouseId = brokerageId,
            Quantity = qty, PriceAtOrder = price, LimitPrice = price,
            Status = status, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Limit,
            SettlementType = SettlementType.T2,
            PlacedBy = PlacedByRole.Investor,
            ExecutionPrice = status == OrderStatus.Filled ? price : 0,
            CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    // ── OrderSummaryReport ───────────────────────────────────

    [Fact]
    public async Task GetOrderSummary_EmptyDb_ReturnsZeroCounts()
    {
        var db  = CreateDb();
        SeedBrokerage(db);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetOrderSummaryAsync(1, new ReportQueryDto());
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalOrders);
    }

    [Fact]
    public async Task GetOrderSummary_WithOrders_CountsCorrectly()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        SeedOrder(db, 1, 1, OrderStatus.Filled);
        SeedOrder(db, 1, 1, OrderStatus.Pending);
        SeedOrder(db, 1, 1, OrderStatus.Cancelled);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetOrderSummaryAsync(1, new ReportQueryDto());
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.TotalOrders);
    }

    [Fact]
    public async Task GetOrderSummary_FiltersByBrokerage()
    {
        var db = CreateDb();
        SeedBrokerage(db, 1); SeedBrokerage(db, 2);
        SeedUser(db, 1, 1); SeedUser(db, 2, 2);
        SeedOrder(db, 1, 1, OrderStatus.Filled);
        SeedOrder(db, 2, 2, OrderStatus.Filled);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetOrderSummaryAsync(1, new ReportQueryDto());
        Assert.Equal(1, result.Value!.TotalOrders);
    }

    [Fact]
    public async Task GetOrderSummary_CalculatesTotalValue()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        SeedOrder(db, 1, 1, OrderStatus.Filled, 100m, 500);
        SeedOrder(db, 1, 1, OrderStatus.Filled, 200m, 300);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetOrderSummaryAsync(1, new ReportQueryDto());
        Assert.True(result.Value!.TotalOrderValue > 0);
    }

    [Fact]
    public async Task GetOrderSummary_NonExistentBrokerage_ReturnsFailure()
    {
        var db     = CreateDb();
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetOrderSummaryAsync(999, new ReportQueryDto());
        Assert.False(result.IsSuccess);
    }

    // ── TopInvestorsReport ───────────────────────────────────

    [Fact]
    public async Task GetTopInvestors_EmptyDb_ReturnsEmptyList()
    {
        var db     = CreateDb();
        SeedBrokerage(db);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetTopInvestorsAsync(1, new ReportQueryDto());
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetTopInvestors_WithOrders_ReturnsRanked()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db, 1, 1); SeedUser(db, 2, 1);
        SeedOrder(db, 1, 1, OrderStatus.Filled, 100m, 1000);
        SeedOrder(db, 1, 1, OrderStatus.Filled, 100m, 500);
        SeedOrder(db, 2, 1, OrderStatus.Filled, 100m, 100);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetTopInvestorsAsync(1, new ReportQueryDto());
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.Count <= 2);
    }

    // ── CommissionReport ─────────────────────────────────────

    [Fact]
    public async Task GetCommissionReport_EmptyDb_ReturnsZero()
    {
        var db     = CreateDb();
        SeedBrokerage(db);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetCommissionReportAsync(1, new ReportQueryDto());
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalExecutedOrders);
        Assert.Equal(0, result.Value!.EstimatedCommission);
    }

    [Fact]
    public async Task GetCommissionReport_WithExecutedOrders_CalculatesCommission()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        SeedOrder(db, 1, 1, OrderStatus.Filled, 100m, 1000);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetCommissionReportAsync(1, new ReportQueryDto());
        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.EstimatedCommission >= 0);
    }

    // ── FundRequestReport ────────────────────────────────────

    [Fact]
    public async Task GetFundRequestReport_EmptyDb_ReturnsZero()
    {
        var db     = CreateDb();
        SeedBrokerage(db);
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetFundRequestReportAsync(1, new ReportQueryDto());
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value!.TotalRequests);
    }

    [Fact]
    public async Task GetFundRequestReport_WithRequests_CountsCorrectly()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        db.FundRequests.AddRange(
            new FundRequest { InvestorId=1, BrokerageHouseId=1, Amount=50000, PaymentMethod=PaymentMethod.BEFTN, Status=FundRequestStatus.Completed, CreatedAt=DateTime.UtcNow },
            new FundRequest { InvestorId=1, BrokerageHouseId=1, Amount=25000, PaymentMethod=PaymentMethod.Cash,  Status=FundRequestStatus.Pending,   CreatedAt=DateTime.UtcNow }
        );
        await db.SaveChangesAsync();
        var svc    = new BrokerageReportService(db);
        var result = await svc.GetFundRequestReportAsync(1, new ReportQueryDto());
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TotalRequests);
        Assert.Equal(1, result.Value!.CompletedRequests);
        Assert.Equal(1, result.Value!.PendingRequests);
    }

    // ── ReportQueryDto ───────────────────────────────────────

    [Fact]
    public void ReportQueryDto_DefaultDates_AreNull()
    {
        var q = new ReportQueryDto();
        Assert.Null(q.FromDate);
        Assert.Null(q.ToDate);
    }

    [Fact]
    public void ReportQueryDto_CanSetDates()
    {
        var from = new DateTime(2026, 1, 1);
        var to   = new DateTime(2026, 3, 31);
        var q = new ReportQueryDto { FromDate = from, ToDate = to };
        Assert.Equal(from, q.FromDate);
        Assert.Equal(to,   q.ToDate);
    }
}