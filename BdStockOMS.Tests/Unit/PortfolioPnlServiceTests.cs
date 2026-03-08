using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BdStockOMS.API.Models;

namespace BdStockOMS.Tests.Unit;

public class PortfolioPnlServiceTests
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

        var role = new Role { Id = 7, Name = "Investor" };
        db.Roles.Add(role);

        var bh = new BrokerageHouse
        {
            Id = 1, Name = "Test Brokerage", LicenseNumber = "TB001",
            Email = "bh@test.com", Phone = "0100000000",
            Address = "Dhaka", IsActive = true
        };
        db.BrokerageHouses.Add(bh);

        var investor = new User
        {
            Id = 1, FullName = "Test Investor", Email = "investor@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 50000m
        };
        db.Users.Add(investor);

        // Stock bought at 380, now trading at 400 → PROFIT
        var stockGP = new Stock
        {
            Id = 1, TradingCode = "GP", CompanyName = "Grameenphone",
            Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1,
            LastTradePrice = 400m, CircuitBreakerHigh = 440m,
            CircuitBreakerLow = 360m, IsActive = true, LastUpdatedAt = DateTime.UtcNow
        };

        // Stock bought at 100, now trading at 80 → LOSS
        var stockBRAC = new Stock
        {
            Id = 2, TradingCode = "BRACBANK", CompanyName = "BRAC Bank",
            Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1,
            LastTradePrice = 80m, CircuitBreakerHigh = 110m,
            CircuitBreakerLow = 72m, IsActive = true, LastUpdatedAt = DateTime.UtcNow
        };
        db.Stocks.AddRange(stockGP, stockBRAC);

        // Portfolio: 100 shares of GP bought at 380
        var portfolioGP = new Portfolio
        {
            Id = 1, InvestorId = 1, StockId = 1, BrokerageHouseId = 1,
            Quantity = 100, AverageBuyPrice = 380m, LastUpdatedAt = DateTime.UtcNow
        };

        // Portfolio: 200 shares of BRACBANK bought at 100
        var portfolioBRAC = new Portfolio
        {
            Id = 2, InvestorId = 1, StockId = 2, BrokerageHouseId = 1,
            Quantity = 200, AverageBuyPrice = 100m, LastUpdatedAt = DateTime.UtcNow
        };
        db.Portfolios.AddRange(portfolioGP, portfolioBRAC);

        await db.SaveChangesAsync();
        return db;
    }

    // ── PORTFOLIO SUMMARY TESTS ────────────────────

    [Fact]
    public async Task GetPortfolioSummary_ValidInvestor_ReturnsSuccess()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetPortfolioSummaryAsync(1);

        Assert.True(result.IsSuccess);
        Assert.Equal("Test Investor", result.Value!.InvestorName);
        Assert.Equal(2, result.Value.Holdings.Count);
    }

    [Fact]
    public async Task GetPortfolioSummary_CalculatesCostBasisCorrectly()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetPortfolioSummaryAsync(1);

        // GP: 100 × 380 = 38000, BRAC: 200 × 100 = 20000, Total = 58000
        Assert.Equal(58000m, result.Value!.TotalCostBasis);
    }

    [Fact]
    public async Task GetPortfolioSummary_CalculatesCurrentValueCorrectly()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetPortfolioSummaryAsync(1);

        // GP: 100 × 400 = 40000, BRAC: 200 × 80 = 16000, Total = 56000
        Assert.Equal(56000m, result.Value!.TotalCurrentValue);
    }

    [Fact]
    public async Task GetPortfolioSummary_CalculatesTotalPnLCorrectly()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetPortfolioSummaryAsync(1);

        // CurrentValue 56000 - CostBasis 58000 = -2000 (net loss)
        Assert.Equal(-2000m, result.Value!.TotalUnrealizedPnL);
    }

    [Fact]
    public async Task GetPortfolioSummary_IncludesCashInTotalPortfolioValue()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetPortfolioSummaryAsync(1);

        // TotalPortfolioValue = CashBalance 50000 + CurrentValue 56000 = 106000
        Assert.Equal(106000m, result.Value!.TotalPortfolioValue);
    }

    [Fact]
    public async Task GetPortfolioSummary_InvestorNotFound_ReturnsFail()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetPortfolioSummaryAsync(999);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    // ── INDIVIDUAL HOLDING TESTS ───────────────────

    [Fact]
    public async Task GetHolding_ProfitableStock_ReturnsPositivePnL()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        // GP bought at 380, now 400 → profit
        var result = await svc.GetHoldingAsync(1, 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(380m, result.Value!.AverageBuyPrice);
        Assert.Equal(400m, result.Value.CurrentPrice);
        Assert.Equal(2000m, result.Value.UnrealizedPnL);   // 100 × (400-380)
        Assert.True(result.Value.PnLPercent > 0);
    }

    [Fact]
    public async Task GetHolding_LosingStock_ReturnsNegativePnL()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        // BRACBANK bought at 100, now 80 → loss
        var result = await svc.GetHoldingAsync(1, 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(-4000m, result.Value!.UnrealizedPnL);  // 200 × (80-100)
        Assert.True(result.Value.PnLPercent < 0);
    }

    [Fact]
    public async Task GetHolding_NotFound_ReturnsFail()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetHoldingAsync(1, 999);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task GetHolding_CalculatesCostBasisCorrectly()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetHoldingAsync(1, 1);

        // 100 shares × 380 avg buy price = 38000
        Assert.Equal(38000m, result.Value!.CostBasis);
        Assert.Equal(40000m, result.Value.CurrentValue);
    }

    // ── PORTFOLIO HISTORY TESTS ────────────────────

    [Fact]
    public async Task GetPortfolioHistory_WithMarketData_ReturnsHistory()
    {
        var db = await SeedAsync();

        // Seed 3 days of market data for GP
        db.MarketData.AddRange(
            new MarketData { StockId = 1, Exchange = "DSE", Date = DateTime.UtcNow.Date.AddDays(-2), Open = 375m, High = 385m, Low = 370m, Close = 382m, Volume = 50000, CreatedAt = DateTime.UtcNow },
            new MarketData { StockId = 1, Exchange = "DSE", Date = DateTime.UtcNow.Date.AddDays(-1), Open = 382m, High = 395m, Low = 380m, Close = 390m, Volume = 60000, CreatedAt = DateTime.UtcNow },
            new MarketData { StockId = 1, Exchange = "DSE", Date = DateTime.UtcNow.Date,             Open = 390m, High = 405m, Low = 388m, Close = 400m, Volume = 70000, CreatedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var svc = new PortfolioPnlService(db);
        var result = await svc.GetPortfolioHistoryAsync(1, 30);

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Count);
    }

    [Fact]
    public async Task GetPortfolioHistory_InvestorNotFound_ReturnsFail()
    {
        var db = await SeedAsync();
        var svc = new PortfolioPnlService(db);

        var result = await svc.GetPortfolioHistoryAsync(999, 30);

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task GetPortfolioHistory_EmptyPortfolio_ReturnsEmptyList()
    {
        var db = CreateDb();
        var role = new Role { Id = 7, Name = "Investor" };
        var bh = new BrokerageHouse { Id = 1, Name = "BH", LicenseNumber = "BH001", Email = "bh@test.com", Phone = "01000", Address = "Dhaka", IsActive = true };
        var investor = new User { Id = 1, FullName = "Empty Investor", Email = "empty@test.com", PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1, IsActive = true, CashBalance = 0 };
        db.Roles.Add(role);
        db.BrokerageHouses.Add(bh);
        db.Users.Add(investor);
        await db.SaveChangesAsync();

        var svc = new PortfolioPnlService(db);
        var result = await svc.GetPortfolioHistoryAsync(1, 30);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }
}
