// Tests/Unit/OrderServiceTests.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Order;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class OrderServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new AppDbContext(options);

        // Seed roles
        db.Roles.AddRange(
            new Role { Id = 6, Name = "Trader" },
            new Role { Id = 7, Name = "Investor" }
        );

        // Seed brokerage
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test Brokerage", LicenseNumber = "LIC001",
            Email = "test@b.com", IsActive = true, CreatedAt = DateTime.UtcNow
        });

        // Seed investor with active BO account and cash
        db.Users.Add(new User
        {
            Id = 1, FullName = "Test Investor", Email = "investor@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            IsBOAccountActive = true, AccountType = AccountType.Cash,
            CashBalance = 100000, MarginLimit = 0, MarginUsed = 0,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });

        // Seed margin investor
        db.Users.Add(new User
        {
            Id = 2, FullName = "Margin Investor", Email = "margin@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            IsBOAccountActive = true, AccountType = AccountType.Margin,
            CashBalance = 50000, MarginLimit = 50000, MarginUsed = 0,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });

        // Seed trader
        db.Users.Add(new User
        {
            Id = 3, FullName = "Test Trader", Email = "trader@test.com",
            PasswordHash = "hash", RoleId = 6, BrokerageHouseId = 1,
            IsBOAccountActive = false, IsActive = true, CreatedAt = DateTime.UtcNow
        });

        // Seed investor assigned to trader
        db.Users.Add(new User
        {
            Id = 4, FullName = "Assigned Investor", Email = "assigned@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            IsBOAccountActive = true, AccountType = AccountType.Cash,
            CashBalance = 100000, AssignedTraderId = 3,
            IsActive = true, CreatedAt = DateTime.UtcNow
        });

        // Seed normal stock (Category A)
        db.Stocks.Add(new Stock
        {
            Id = 1, TradingCode = "GP", CompanyName = "Grameenphone",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = 380, CircuitBreakerHigh = 418,
            CircuitBreakerLow = 342, BoardLotSize = 1, IsActive = true,
            LastUpdatedAt = DateTime.UtcNow
        });

        // Seed Z category stock
        db.Stocks.Add(new Stock
        {
            Id = 2, TradingCode = "ZSTOCK", CompanyName = "Z Company",
            Exchange = "DSE", Category = StockCategory.Z,
            LastTradePrice = 10, CircuitBreakerHigh = 11,
            CircuitBreakerLow = 9, BoardLotSize = 1, IsActive = true,
            LastUpdatedAt = DateTime.UtcNow
        });

        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task PlaceOrder_ValidBuyOrder_ReturnsOrder()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 1, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Market, Quantity = 10
        }, placedByUserId: 1, placedByRole: "Investor");

        Assert.Null(error);
        Assert.NotNull(order);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.Equal(SettlementType.T2, order.SettlementType);
    }

    [Fact]
    public async Task PlaceOrder_InactiveBOAccount_ReturnsError()
    {
        var db = CreateDb();
        // Deactivate investor BO account
        var investor = await db.Users.FindAsync(1);
        investor!.IsBOAccountActive = false;
        await db.SaveChangesAsync();

        var service = new OrderService(db);
        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 1, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Market, Quantity = 10
        }, 1, "Investor");

        Assert.Null(order);
        Assert.Contains("BO account", error);
    }

    [Fact]
    public async Task PlaceOrder_InsufficientCash_ReturnsError()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        // Try to buy 1000 shares at 380 = 380,000 but only has 100,000
        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 1, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Market, Quantity = 1000
        }, 1, "Investor");

        Assert.Null(order);
        Assert.Contains("Insufficient", error);
    }

    [Fact]
    public async Task PlaceOrder_ZCategoryWithMargin_ReturnsError()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        // Margin investor tries to buy Z stock — not allowed
        var investor = await db.Users.FindAsync(2);
        investor!.CashBalance = 0; // no cash, only margin
        await db.SaveChangesAsync();

        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 2, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Market, Quantity = 10
        }, 2, "Investor");

        Assert.Null(order);
        Assert.Contains("Z/Spot", error);
    }

    [Fact]
    public async Task PlaceOrder_SellWithoutShares_ReturnsError()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        // Investor has no GP shares in portfolio
        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 1, OrderType = OrderType.Sell,
            OrderCategory = OrderCategory.Market, Quantity = 10
        }, 1, "Investor");

        Assert.Null(order);
        Assert.Contains("Insufficient shares", error);
    }

    [Fact]
    public async Task PlaceOrder_PriceAboveCircuitBreaker_ReturnsError()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 1, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Limit,
            Quantity = 10, LimitPrice = 999 // above 418 circuit breaker high
        }, 1, "Investor");

        Assert.Null(order);
        Assert.Contains("circuit breaker", error);
    }

    [Fact]
    public async Task PlaceOrder_TraderForAssignedInvestor_Succeeds()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 1, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Market,
            Quantity = 5, InvestorId = 4
        }, placedByUserId: 3, placedByRole: "Trader");

        Assert.Null(error);
        Assert.NotNull(order);
        Assert.Equal(PlacedByRole.Trader, order.PlacedBy);
        Assert.Equal(4, order.InvestorId);
        Assert.Equal(3, order.TraderId);
    }

    [Fact]
    public async Task PlaceOrder_TraderForUnassignedInvestor_ReturnsError()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        // Trader 3 tries to place for Investor 1 (not assigned)
        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 1, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Market,
            Quantity = 5, InvestorId = 1
        }, placedByUserId: 3, placedByRole: "Trader");

        Assert.Null(order);
        Assert.Contains("not assigned", error);
    }

    [Fact]
    public async Task CancelOrder_PendingOrder_Succeeds()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        var (placed, _) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 1, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Market, Quantity = 10
        }, 1, "Investor");

        var (cancelled, error) = await service.CancelOrderAsync(
            placed!.Id, 1, "Investor", "Changed my mind");

        Assert.Null(error);
        Assert.Equal(OrderStatus.Cancelled, cancelled!.Status);
    }

    [Fact]
    public async Task PlaceOrder_ZCategory_SettlementIsT0()
    {
        var db = CreateDb();
        var service = new OrderService(db);

        var (order, error) = await service.PlaceOrderAsync(new PlaceOrderDto
        {
            StockId = 2, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Market, Quantity = 5
        }, 1, "Investor");

        Assert.Null(error);
        Assert.Equal(SettlementType.T0, order!.SettlementType);
    }
}
