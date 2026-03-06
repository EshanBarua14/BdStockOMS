using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class DbContextTests
{
    // Creates a fresh in-memory database for each test
    // WHY in-memory?
    // → No real SQL Server needed for tests
    // → Tests run fast
    // → Each test starts with clean empty database
    // → Tests work on any machine
    private AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                databaseName: Guid.NewGuid().ToString()
            // Guid.NewGuid() = unique random name
            // so each test gets its own fresh DB
            )
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task CanAddAndRetrieveRole()
    {
        // ARRANGE
        using var context = CreateInMemoryContext();

        var role = new Role { Id = 1, Name = "Admin" };

        // ACT
        // AddAsync = stage record for insertion
        await context.Roles.AddAsync(role);
        // SaveChangesAsync = actually write to DB
        await context.SaveChangesAsync();

        // ASSERT
        // FindAsync = find by primary key
        var retrieved = await context.Roles.FindAsync(1);
        Assert.NotNull(retrieved);
        Assert.Equal("Admin", retrieved.Name);
    }

    [Fact]
    public async Task CanAddAndRetrieveStock()
    {
        // ARRANGE
        using var context = CreateInMemoryContext();

        var stock = new Stock
        {
            Id = 1,
            TradingCode = "GP",
            CompanyName = "Grameenphone Ltd",
            Exchange = "DSE",
            LastTradePrice = 380.50m,
            IsActive = true,
            LastUpdatedAt = DateTime.UtcNow
        };

        // ACT
        await context.Stocks.AddAsync(stock);
        await context.SaveChangesAsync();

        // ASSERT
        var retrieved = await context.Stocks.FindAsync(1);
        Assert.NotNull(retrieved);
        Assert.Equal("GP", retrieved.TradingCode);
        Assert.Equal("DSE", retrieved.Exchange);
        Assert.Equal(380.50m, retrieved.LastTradePrice);
    }

    [Fact]
    public async Task CanAddMultipleStocks()
    {
        // ARRANGE
        using var context = CreateInMemoryContext();

        var stocks = new List<Stock>
        {
            new Stock
            {
                Id = 1, TradingCode = "GP",
                CompanyName = "Grameenphone",
                Exchange = "DSE",
                LastTradePrice = 380m,
                IsActive = true,
                LastUpdatedAt = DateTime.UtcNow
            },
            new Stock
            {
                Id = 2, TradingCode = "BRACBANK",
                CompanyName = "BRAC Bank",
                Exchange = "DSE",
                LastTradePrice = 52m,
                IsActive = true,
                LastUpdatedAt = DateTime.UtcNow
            },
            new Stock
            {
                Id = 3, TradingCode = "GP",
                CompanyName = "Grameenphone",
                Exchange = "CSE",
                LastTradePrice = 379m,
                IsActive = true,
                LastUpdatedAt = DateTime.UtcNow
            }
        };

        // ACT
        // AddRangeAsync = add multiple records at once
        await context.Stocks.AddRangeAsync(stocks);
        await context.SaveChangesAsync();

        // ASSERT
        // ToListAsync = get all records as a list
        var allStocks = await context.Stocks.ToListAsync();
        Assert.Equal(3, allStocks.Count);
    }

    [Fact]
    public async Task Order_DefaultStatus_IsPending_InDatabase()
    {
        // ARRANGE
        using var context = CreateInMemoryContext();

        // Order has foreign keys to BrokerageHouse,
        // Role, User and Stock — we must create those
        // first before creating the Order
        var brokerage = new BrokerageHouse
        {
            Id = 1,
            Name = "Test Brokerage",
            LicenseNumber = "LIC001",
            Email = "test@brokerage.com"
        };
        var role = new Role { Id = 6, Name = "Investor" };

        await context.BrokerageHouses.AddAsync(brokerage);
        await context.Roles.AddAsync(role);
        await context.SaveChangesAsync();

        var investor = new User
        {
            Id = 1,
            FullName = "Test Investor",
            Email = "investor@test.com",
            PasswordHash = "hashedpassword",
            RoleId = 6,
            BrokerageHouseId = 1
        };
        var stock = new Stock
        {
            Id = 1,
            TradingCode = "GP",
            CompanyName = "Grameenphone",
            Exchange = "DSE",
            LastTradePrice = 380m,
            IsActive = true,
            LastUpdatedAt = DateTime.UtcNow
        };

        await context.Users.AddAsync(investor);
        await context.Stocks.AddAsync(stock);
        await context.SaveChangesAsync();

        var order = new Order
        {
            InvestorId = 1,
            StockId = 1,
            BrokerageHouseId = 1,
            OrderType = OrderType.Buy,
            Quantity = 100,
            PriceAtOrder = 380m
            // Status not set — must default to Pending
        };

        // ACT
        await context.Orders.AddAsync(order);
        await context.SaveChangesAsync();

        // ASSERT
        var saved = await context.Orders.FindAsync(order.Id);
        Assert.NotNull(saved);
        Assert.Equal(OrderStatus.Pending, saved.Status);
    }
}