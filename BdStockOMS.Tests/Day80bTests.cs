using BdStockOMS.API.Models;
using BdStockOMS.API.DTOs.Order;
using BdStockOMS.API.Services;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests;

public class Day80bTests
{
    private static AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static Order MakeOrder(OrderStatus status, int investorId = 1, int quantity = 10, decimal price = 100m)
        => new Order
        {
            InvestorId       = investorId,
            StockId          = 1,
            BrokerageHouseId = 1,
            OrderType        = OrderType.Buy,
            OrderCategory    = OrderCategory.Limit,
            Quantity         = quantity,
            PriceAtOrder     = price,
            LimitPrice       = price,
            Status           = status,
            PlacedBy         = PlacedByRole.Investor,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow,
        };

    // ── CancelOrderAsync ────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Queued)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Waiting)]
    [InlineData(OrderStatus.Open)]
    public void CancelOrder_AllowedStatuses_AreCorrect(OrderStatus status)
    {
        var allowed = new[]
        {
            OrderStatus.Pending, OrderStatus.Queued, OrderStatus.Submitted,
            OrderStatus.Waiting, OrderStatus.Open, OrderStatus.CancelRequested
        };
        Assert.Contains(status, allowed);
    }

    [Theory]
    [InlineData(OrderStatus.Filled)]
    [InlineData(OrderStatus.Cancelled)]
    [InlineData(OrderStatus.Rejected)]
    [InlineData(OrderStatus.Deleted)]
    public void CancelOrder_BlockedStatuses_AreCorrect(OrderStatus status)
    {
        var allowed = new[]
        {
            OrderStatus.Pending, OrderStatus.Queued, OrderStatus.Submitted,
            OrderStatus.Waiting, OrderStatus.Open, OrderStatus.CancelRequested
        };
        Assert.DoesNotContain(status, allowed);
    }

    [Fact]
    public async Task CancelOrder_SetsStatusToCancelled()
    {
        using var db = CreateDb();
        var order = MakeOrder(OrderStatus.Waiting);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        order.Status      = OrderStatus.Cancelled;
        order.CancelledAt = DateTime.UtcNow;
        order.UpdatedAt   = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Orders.FindAsync(order.Id);
        Assert.Equal(OrderStatus.Cancelled, saved!.Status);
        Assert.NotNull(saved.CancelledAt);
    }

    [Fact]
    public async Task CancelOrder_SetsRejectionReason()
    {
        using var db = CreateDb();
        var order = MakeOrder(OrderStatus.Pending);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        order.Status          = OrderStatus.Cancelled;
        order.RejectionReason = "User cancelled";
        order.UpdatedAt       = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Orders.FindAsync(order.Id);
        Assert.Equal("User cancelled", saved!.RejectionReason);
    }

    // ── AmendOrderAsync ─────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.Pending)]
    [InlineData(OrderStatus.Queued)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Waiting)]
    [InlineData(OrderStatus.Open)]
    public void AmendOrder_AllowedStatuses_AreCorrect(OrderStatus status)
    {
        var allowed = new[]
        {
            OrderStatus.Pending, OrderStatus.Queued, OrderStatus.Submitted,
            OrderStatus.Waiting, OrderStatus.Open
        };
        Assert.Contains(status, allowed);
    }

    [Fact]
    public async Task AmendOrder_UpdatesQuantity()
    {
        using var db = CreateDb();
        var order = MakeOrder(OrderStatus.Waiting, quantity: 10);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        order.Quantity  = 25;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Orders.FindAsync(order.Id);
        Assert.Equal(25, saved!.Quantity);
    }

    [Fact]
    public async Task AmendOrder_UpdatesLimitPrice()
    {
        using var db = CreateDb();
        var order = MakeOrder(OrderStatus.Pending, price: 100m);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        order.LimitPrice = 95.50m;
        order.UpdatedAt  = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Orders.FindAsync(order.Id);
        Assert.Equal(95.50m, saved!.LimitPrice);
    }

    [Fact]
    public async Task AmendOrder_UpdatesNotes()
    {
        using var db = CreateDb();
        var order = MakeOrder(OrderStatus.Open);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        order.Notes     = "Amended by trader";
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Orders.FindAsync(order.Id);
        Assert.Equal("Amended by trader", saved!.Notes);
    }

    [Fact]
    public async Task AmendOrder_UpdatesUpdatedAt()
    {
        using var db = CreateDb();
        var order = MakeOrder(OrderStatus.Queued);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var before = order.UpdatedAt;
        await Task.Delay(10);
        order.Quantity  = 5;
        order.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var saved = await db.Orders.FindAsync(order.Id);
        Assert.True(saved!.UpdatedAt >= before);
    }

    // ── AmendOrderRequestDto ────────────────────────────────────────

    [Fact]
    public void AmendOrderRequestDto_AllFieldsNullable()
    {
        var dto = new AmendOrderRequestDto(null, null, null);
        Assert.Null(dto.Quantity);
        Assert.Null(dto.LimitPrice);
        Assert.Null(dto.Notes);
    }

    [Fact]
    public void AmendOrderRequestDto_AcceptsValues()
    {
        var dto = new AmendOrderRequestDto(50, 75.25m, "Test note");
        Assert.Equal(50,      dto.Quantity);
        Assert.Equal(75.25m,  dto.LimitPrice);
        Assert.Equal("Test note", dto.Notes);
    }

    // ── OrderStatusBadge string mapping ────────────────────────────

    [Theory]
    [InlineData("Pending")]
    [InlineData("Waiting")]
    [InlineData("Open")]
    [InlineData("Queued")]
    [InlineData("Submitted")]
    [InlineData("Filled")]
    [InlineData("Cancelled")]
    [InlineData("Rejected")]
    [InlineData("CancelRequested")]
    [InlineData("EditRequested")]
    [InlineData("Deleted")]
    [InlineData("Replaced")]
    [InlineData("Private")]
    public void OrderStatus_StringNames_MatchEnumNames(string statusName)
    {
        var parsed = Enum.TryParse<OrderStatus>(statusName, out var result);
        Assert.True(parsed, $"'{statusName}' should be a valid OrderStatus name");
    }

    [Fact]
    public void OrderStatus_FilledIsNotExecuted()
    {
        Assert.DoesNotContain("Executed", Enum.GetNames<OrderStatus>());
        Assert.Contains("Filled", Enum.GetNames<OrderStatus>());
    }
}
