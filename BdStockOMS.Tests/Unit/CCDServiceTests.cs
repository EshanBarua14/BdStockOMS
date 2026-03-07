// Tests/Unit/CCDServiceTests.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.CCD;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class CCDServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        db.Roles.AddRange(
            new Role { Id = 6, Name = "Trader" },
            new Role { Id = 7, Name = "Investor" }
        );
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test Brokerage", LicenseNumber = "LIC001",
            Email = "test@b.com", IsActive = true, CreatedAt = DateTime.UtcNow
        });
        // Investor without BO account
        db.Users.Add(new User
        {
            Id = 1, FullName = "Test Investor", Email = "investor@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            IsBOAccountActive = false, IsActive = true, CreatedAt = DateTime.UtcNow
        });
        // Investor with active BO account
        db.Users.Add(new User
        {
            Id = 2, FullName = "Active Investor", Email = "active@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            BONumber = "1201950099999999",
            AccountType = AccountType.Cash,
            CashBalance = 50000, MarginLimit = 0, MarginUsed = 0,
            IsBOAccountActive = true, IsActive = true, CreatedAt = DateTime.UtcNow
        });
        // Margin investor
        db.Users.Add(new User
        {
            Id = 3, FullName = "Margin Investor", Email = "margin@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            BONumber = "1201950088888888",
            AccountType = AccountType.Margin,
            CashBalance = 20000, MarginLimit = 50000, MarginUsed = 10000,
            IsBOAccountActive = true, IsActive = true, CreatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task OpenBOAccount_ValidInvestor_OpensAccount()
    {
        var db = CreateDb();
        var service = new CCDService(db);

        var (account, error) = await service.OpenBOAccountAsync(new OpenBOAccountDto
        {
            UserId = 1,
            BONumber = "1201950012345678",
            AccountType = AccountType.Cash,
            InitialCashBalance = 10000
        });

        Assert.Null(error);
        Assert.NotNull(account);
        Assert.True(account.IsBOAccountActive);
        Assert.Equal("1201950012345678", account.BONumber);
        Assert.Equal(10000, account.CashBalance);
    }

    [Fact]
    public async Task OpenBOAccount_AlreadyActive_ReturnsError()
    {
        var db = CreateDb();
        var service = new CCDService(db);

        var (account, error) = await service.OpenBOAccountAsync(new OpenBOAccountDto
        {
            UserId = 2, // already has active BO account
            BONumber = "1201950011111111",
            AccountType = AccountType.Cash
        });

        Assert.Null(account);
        Assert.Contains("already has", error);
    }

    [Fact]
    public async Task OpenBOAccount_DuplicateBONumber_ReturnsError()
    {
        var db = CreateDb();
        var service = new CCDService(db);

        var (account, error) = await service.OpenBOAccountAsync(new OpenBOAccountDto
        {
            UserId = 1,
            BONumber = "1201950099999999", // already used by investor 2
            AccountType = AccountType.Cash
        });

        Assert.Null(account);
        Assert.Contains("already in use", error);
    }

    [Fact]
    public async Task DepositCash_ValidAccount_IncreasesCashBalance()
    {
        var db = CreateDb();
        var service = new CCDService(db);

        var (account, error) = await service.DepositCashAsync(new DepositCashDto
        {
            UserId = 2,
            Amount = 25000
        });

        Assert.Null(error);
        Assert.Equal(75000, account!.CashBalance); // 50000 + 25000
    }

    [Fact]
    public async Task SetMarginLimit_ValidMarginAccount_UpdatesLimit()
    {
        var db = CreateDb();
        var service = new CCDService(db);

        var (account, error) = await service.SetMarginLimitAsync(new SetMarginLimitDto
        {
            UserId = 3,
            MarginLimit = 80000
        });

        Assert.Null(error);
        Assert.Equal(80000, account!.MarginLimit);
    }

    [Fact]
    public async Task SetMarginLimit_BelowUsedMargin_ReturnsError()
    {
        var db = CreateDb();
        var service = new CCDService(db);

        // Margin used is 10000, trying to set limit to 5000
        var (account, error) = await service.SetMarginLimitAsync(new SetMarginLimitDto
        {
            UserId = 3,
            MarginLimit = 5000
        });

        Assert.Null(account);
        Assert.Contains("margin in use", error);
    }

    [Fact]
    public async Task ToggleBOAccount_Deactivate_SetsInactive()
    {
        var db = CreateDb();
        var service = new CCDService(db);

        var (account, error) = await service.ToggleBOAccountAsync(2, false);

        Assert.Null(error);
        Assert.False(account!.IsBOAccountActive);
    }

    [Fact]
    public async Task SettleOrder_ExecutedBuyOrder_UpdatesPortfolio()
    {
        var db = CreateDb();

        // Add stock and executed order
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "GP", CompanyName = "Grameenphone",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 380, CircuitBreakerHigh = 418,
            CircuitBreakerLow = 342, BoardLotSize = 1,
            IsActive = true, LastUpdatedAt = DateTime.UtcNow
        });
        db.Orders.Add(new Order
        {
            Id = 1, InvestorId = 2, StockId = 1, BrokerageHouseId = 1,
            OrderType = OrderType.Buy, OrderCategory = OrderCategory.Market,
            Quantity = 10, PriceAtOrder = 380, ExecutionPrice = 380,
            Status = OrderStatus.Executed, SettlementType = SettlementType.T2,
            PlacedBy = PlacedByRole.Investor, CreatedAt = DateTime.UtcNow,
            ExecutedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = new CCDService(db);
        var (order, error) = await service.SettleOrderAsync(1);

        Assert.Null(error);
        Assert.Equal(OrderStatus.Completed, order!.Status);

        // Check portfolio was created
        var portfolio = await db.Portfolios
            .FirstOrDefaultAsync(p => p.InvestorId == 2 && p.StockId == 1);
        Assert.NotNull(portfolio);
        Assert.Equal(10, portfolio.Quantity);
        Assert.Equal(380, portfolio.AverageBuyPrice);
    }
}
