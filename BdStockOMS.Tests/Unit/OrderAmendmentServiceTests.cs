using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.OrderAmendment;
using BdStockOMS.API.DTOs.TraderReassignment;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class OrderAmendmentServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private async Task<(AppDbContext db, Role investorRole, Role traderRole, Role ccdRole,
        BrokerageHouse bh, User investor, User trader, User ccd, Stock stock)> SeedAsync()
    {
        var db = CreateDb();

        var investorRole = new Role { Id = 7, Name = "Investor" };
        var traderRole   = new Role { Id = 6, Name = "Trader" };
        var ccdRole      = new Role { Id = 4, Name = "CCD" };
        db.Roles.AddRange(investorRole, traderRole, ccdRole);

        var bh = new BrokerageHouse { Id = 1, Name = "Test Brokerage", LicenseNumber = "TB001", IsActive = true };
        db.BrokerageHouses.Add(bh);

        var stock = new Stock
        {
            Id = 1, TradingCode = "GP", CompanyName = "Grameenphone",
            Exchange = "DSE", Category = StockCategory.A, BoardLotSize = 1,
            LastTradePrice = 380m, CircuitBreakerHigh = 418m, CircuitBreakerLow = 342m,
            IsActive = true, LastUpdatedAt = DateTime.UtcNow
        };
        db.Stocks.Add(stock);

        var trader = new User
        {
            Id = 1, FullName = "Test Trader", Email = "trader@test.com",
            PasswordHash = "hash", RoleId = 6, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 0
        };
        var investor = new User
        {
            Id = 2, FullName = "Test Investor", Email = "investor@test.com",
            PasswordHash = "hash", RoleId = 7, BrokerageHouseId = 1,
            AssignedTraderId = 1, IsActive = true, CashBalance = 100000m
        };
        var ccd = new User
        {
            Id = 3, FullName = "CCD User", Email = "ccd@test.com",
            PasswordHash = "hash", RoleId = 4, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 0
        };
        db.Users.AddRange(trader, investor, ccd);
        await db.SaveChangesAsync();

        // Re-attach roles for navigation
        trader.Role = traderRole;
        investor.Role = investorRole;
        ccd.Role = ccdRole;

        return (db, investorRole, traderRole, ccdRole, bh, investor, trader, ccd, stock);
    }

    private Order CreatePendingLimitOrder(int investorId, int traderId, int stockId, int brokerageHouseId)
        => new Order
        {
            InvestorId = investorId, TraderId = traderId, StockId = stockId,
            BrokerageHouseId = brokerageHouseId, OrderType = OrderType.Buy,
            OrderCategory = OrderCategory.Limit, Quantity = 100,
            PriceAtOrder = 380m, LimitPrice = 375m,
            SettlementType = SettlementType.T2, PlacedBy = PlacedByRole.Trader,
            Status = OrderStatus.Pending, CreatedAt = DateTime.UtcNow
        };

    // ── ORDER AMENDMENT TESTS ──────────────────────

    [Fact]
    public async Task AmendOrder_ChangeQuantity_ReturnsSuccess()
    {
        var (db, _, _, _, _, investor, trader, _, stock) = await SeedAsync();
        var order = CreatePendingLimitOrder(investor.Id, trader.Id, stock.Id, 1);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var svc = new OrderAmendmentService(db);
        var result = await svc.AmendOrderAsync(order.Id, trader.Id, new AmendOrderDto
        {
            NewQuantity = 200, Reason = "Increasing position"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value!.OldQuantity);
        Assert.Equal(200, result.Value.NewQuantity);
    }

    [Fact]
    public async Task AmendOrder_ChangeLimitPrice_ReturnsSuccess()
    {
        var (db, _, _, _, _, investor, trader, _, stock) = await SeedAsync();
        var order = CreatePendingLimitOrder(investor.Id, trader.Id, stock.Id, 1);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var svc = new OrderAmendmentService(db);
        var result = await svc.AmendOrderAsync(order.Id, trader.Id, new AmendOrderDto
        {
            NewPrice = 370m, Reason = "Adjusting limit price"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(375m, result.Value!.OldPrice);
        Assert.Equal(370m, result.Value.NewPrice);
    }

    [Fact]
    public async Task AmendOrder_NonPendingOrder_ReturnsFail()
    {
        var (db, _, _, _, _, investor, trader, _, stock) = await SeedAsync();
        var order = CreatePendingLimitOrder(investor.Id, trader.Id, stock.Id, 1);
        order.Status = OrderStatus.Executed;
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var svc = new OrderAmendmentService(db);
        var result = await svc.AmendOrderAsync(order.Id, trader.Id, new AmendOrderDto
        {
            NewQuantity = 200
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("pending", result.Error);
    }

    [Fact]
    public async Task AmendOrder_NoFieldsProvided_ReturnsFail()
    {
        var (db, _, _, _, _, investor, trader, _, stock) = await SeedAsync();
        var order = CreatePendingLimitOrder(investor.Id, trader.Id, stock.Id, 1);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var svc = new OrderAmendmentService(db);
        var result = await svc.AmendOrderAsync(order.Id, trader.Id, new AmendOrderDto());

        Assert.False(result.IsSuccess);
        Assert.Contains("At least one", result.Error);
    }

    [Fact]
    public async Task AmendOrder_PriceOnMarketOrder_ReturnsFail()
    {
        var (db, _, _, _, _, investor, trader, _, stock) = await SeedAsync();
        var order = CreatePendingLimitOrder(investor.Id, trader.Id, stock.Id, 1);
        order.OrderCategory = OrderCategory.Market;
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var svc = new OrderAmendmentService(db);
        var result = await svc.AmendOrderAsync(order.Id, trader.Id, new AmendOrderDto
        {
            NewPrice = 370m
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Limit orders", result.Error);
    }

    [Fact]
    public async Task AmendOrder_OrderNotFound_ReturnsFail()
    {
        var (db, _, _, _, _, _, trader, _, _) = await SeedAsync();
        var svc = new OrderAmendmentService(db);

        var result = await svc.AmendOrderAsync(999, trader.Id, new AmendOrderDto
        {
            NewQuantity = 100
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task GetByOrder_ReturnsAmendmentHistory()
    {
        var (db, _, _, _, _, investor, trader, _, stock) = await SeedAsync();
        var order = CreatePendingLimitOrder(investor.Id, trader.Id, stock.Id, 1);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var svc = new OrderAmendmentService(db);
        await svc.AmendOrderAsync(order.Id, trader.Id, new AmendOrderDto { NewQuantity = 150 });

        var result = await svc.GetByOrderAsync(order.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }

    // ── TRADER REASSIGNMENT TESTS ──────────────────

    [Fact]
    public async Task ReassignTrader_ValidRequest_ReturnsSuccess()
    {
        var (db, investorRole, traderRole, _, bh, investor, trader, ccd, _) = await SeedAsync();

        var newTrader = new User
        {
            Id = 10, FullName = "New Trader", Email = "newtrader@test.com",
            PasswordHash = "hash", RoleId = 6, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 0, Role = traderRole
        };
        db.Users.Add(newTrader);
        await db.SaveChangesAsync();

        var svc = new TraderReassignmentService(db);
        investor.Role = investorRole;

        var result = await svc.ReassignTraderAsync(ccd.Id, new CreateTraderReassignmentDto
        {
            InvestorId = investor.Id,
            NewTraderId = newTrader.Id,
            Reason = "Trader on leave"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(newTrader.Id, result.Value!.NewTraderId);
        Assert.Equal(trader.Id, result.Value.OldTraderId);
    }

    [Fact]
    public async Task ReassignTrader_SameTrader_ReturnsFail()
    {
        var (db, investorRole, traderRole, _, _, investor, trader, ccd, _) = await SeedAsync();
        investor.Role = investorRole;
        trader.Role = traderRole;

        var svc = new TraderReassignmentService(db);
        var result = await svc.ReassignTraderAsync(ccd.Id, new CreateTraderReassignmentDto
        {
            InvestorId = investor.Id,
            NewTraderId = trader.Id,
            Reason = "No change"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("already assigned", result.Error);
    }

    [Fact]
    public async Task ReassignTrader_InvestorNotFound_ReturnsFail()
    {
        var (db, _, _, _, _, _, _, ccd, _) = await SeedAsync();
        var svc = new TraderReassignmentService(db);

        var result = await svc.ReassignTraderAsync(ccd.Id, new CreateTraderReassignmentDto
        {
            InvestorId = 999, NewTraderId = 1
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task ReassignTrader_DifferentBrokerageHouse_ReturnsFail()
    {
        var (db, investorRole, traderRole, _, _, investor, _, ccd, _) = await SeedAsync();

        var otherBh = new BrokerageHouse { Id = 2, Name = "Other BH", LicenseNumber = "OBH001", IsActive = true };
        db.BrokerageHouses.Add(otherBh);

        var otherTrader = new User
        {
            Id = 20, FullName = "Other Trader", Email = "other@test.com",
            PasswordHash = "hash", RoleId = 6, BrokerageHouseId = 2,
            IsActive = true, CashBalance = 0, Role = traderRole
        };
        db.Users.Add(otherTrader);
        await db.SaveChangesAsync();

        investor.Role = investorRole;
        var svc = new TraderReassignmentService(db);

        var result = await svc.ReassignTraderAsync(ccd.Id, new CreateTraderReassignmentDto
        {
            InvestorId = investor.Id, NewTraderId = otherTrader.Id
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("same brokerage", result.Error);
    }

    [Fact]
    public async Task GetByInvestor_ReturnsReassignmentHistory()
    {
        var (db, investorRole, traderRole, _, bh, investor, trader, ccd, _) = await SeedAsync();

        var newTrader = new User
        {
            Id = 10, FullName = "New Trader", Email = "newtrader@test.com",
            PasswordHash = "hash", RoleId = 6, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 0, Role = traderRole
        };
        db.Users.Add(newTrader);
        await db.SaveChangesAsync();

        investor.Role = investorRole;
        var svc = new TraderReassignmentService(db);

        await svc.ReassignTraderAsync(ccd.Id, new CreateTraderReassignmentDto
        {
            InvestorId = investor.Id, NewTraderId = newTrader.Id, Reason = "Test"
        });

        var result = await svc.GetByInvestorAsync(investor.Id);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
    }
}
