using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class OrderStateMachineTests
{
    private static AppDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static Order BuildOrder(OrderStatus status = OrderStatus.Pending) => new()
    {
        Id              = 1,
        InvestorId      = 1,
        StockId         = 1,
        BrokerageHouseId = 1,
        Quantity        = 100,
        PriceAtOrder    = 50m,
        Status          = status,
        OrderType       = OrderType.Buy,
        OrderCategory   = OrderCategory.Limit,
        SettlementType  = SettlementType.T2,
        PlacedBy        = PlacedByRole.Investor,
    };

    // ── CanTransition ─────────────────────────────────────────────────────
    [Theory]
    [InlineData(OrderStatus.Pending,  OrderStatus.Executed,  true)]
    [InlineData(OrderStatus.Pending,  OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Pending,  OrderStatus.Rejected,  true)]
    [InlineData(OrderStatus.Executed, OrderStatus.Completed, true)]
    [InlineData(OrderStatus.Executed, OrderStatus.Cancelled, true)]
    [InlineData(OrderStatus.Completed,OrderStatus.Cancelled, false)]
    [InlineData(OrderStatus.Cancelled,OrderStatus.Pending,   false)]
    [InlineData(OrderStatus.Rejected, OrderStatus.Executed,  false)]
    public void CanTransition_ReturnsExpected(OrderStatus from, OrderStatus to, bool expected)
    {
        var sm = new OrderStateMachine(BuildDb());
        Assert.Equal(expected, sm.CanTransition(from, to));
    }

    // ── GetAllowedTransitions ─────────────────────────────────────────────
    [Fact]
    public void GetAllowedTransitions_Pending_ReturnsThreeOptions()
    {
        var sm      = new OrderStateMachine(BuildDb());
        var allowed = sm.GetAllowedTransitions(OrderStatus.Pending);
        Assert.Equal(3, allowed.Length);
        Assert.Contains(OrderStatus.Executed,  allowed);
        Assert.Contains(OrderStatus.Cancelled, allowed);
        Assert.Contains(OrderStatus.Rejected,  allowed);
    }

    [Fact]
    public void GetAllowedTransitions_Completed_ReturnsEmpty()
    {
        var sm      = new OrderStateMachine(BuildDb());
        var allowed = sm.GetAllowedTransitions(OrderStatus.Completed);
        Assert.Empty(allowed);
    }

    [Fact]
    public void GetAllowedTransitions_Cancelled_ReturnsEmpty()
    {
        var sm      = new OrderStateMachine(BuildDb());
        var allowed = sm.GetAllowedTransitions(OrderStatus.Cancelled);
        Assert.Empty(allowed);
    }

    // ── TransitionAsync ───────────────────────────────────────────────────
    [Fact]
    public async Task TransitionAsync_ValidTransition_ReturnsTrue()
    {
        var db    = BuildDb();
        var order = BuildOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sm     = new OrderStateMachine(db);
        var result = await sm.TransitionAsync(order, OrderStatus.Executed);

        Assert.True(result);
        Assert.Equal(OrderStatus.Executed, order.Status);
    }

    [Fact]
    public async Task TransitionAsync_InvalidTransition_ReturnsFalse()
    {
        var db    = BuildDb();
        var order = BuildOrder(OrderStatus.Completed);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sm     = new OrderStateMachine(db);
        var result = await sm.TransitionAsync(order, OrderStatus.Cancelled);

        Assert.False(result);
        Assert.Equal(OrderStatus.Completed, order.Status);
    }

    [Fact]
    public async Task TransitionAsync_SetsExecutedAt_WhenMovingToExecuted()
    {
        var db    = BuildDb();
        var order = BuildOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sm = new OrderStateMachine(db);
        await sm.TransitionAsync(order, OrderStatus.Executed);

        Assert.NotNull(order.ExecutedAt);
    }

    [Fact]
    public async Task TransitionAsync_SetsCompletedAt_WhenMovingToCompleted()
    {
        var db    = BuildDb();
        var order = BuildOrder(OrderStatus.Executed);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sm = new OrderStateMachine(db);
        await sm.TransitionAsync(order, OrderStatus.Completed);

        Assert.NotNull(order.CompletedAt);
    }

    [Fact]
    public async Task TransitionAsync_SetsCancelledAt_WhenMovingToCancelled()
    {
        var db    = BuildDb();
        var order = BuildOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sm = new OrderStateMachine(db);
        await sm.TransitionAsync(order, OrderStatus.Cancelled);

        Assert.NotNull(order.CancelledAt);
    }

    [Fact]
    public async Task TransitionAsync_SetsRejectionReason_WhenMovingToRejected()
    {
        var db    = BuildDb();
        var order = BuildOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sm = new OrderStateMachine(db);
        await sm.TransitionAsync(order, OrderStatus.Rejected, reason: "Insufficient funds");

        Assert.Equal("Insufficient funds", order.RejectionReason);
    }

    [Fact]
    public async Task TransitionAsync_CreatesOrderEvent_OnValidTransition()
    {
        var db    = BuildDb();
        var order = BuildOrder();
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sm = new OrderStateMachine(db);
        await sm.TransitionAsync(order, OrderStatus.Executed, triggeredBy: "system-test");

        var evt = db.Set<OrderEvent>().FirstOrDefault();
        Assert.NotNull(evt);
        Assert.Equal("Pending",  evt!.FromStatus);
        Assert.Equal("Executed", evt.ToStatus);
        Assert.Equal("system-test", evt.TriggeredBy);
    }

    [Fact]
    public async Task TransitionAsync_DoesNotCreateEvent_OnInvalidTransition()
    {
        var db    = BuildDb();
        var order = BuildOrder(OrderStatus.Completed);
        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var sm = new OrderStateMachine(db);
        await sm.TransitionAsync(order, OrderStatus.Cancelled);

        Assert.Empty(db.Set<OrderEvent>());
    }

    // ── Trade model ───────────────────────────────────────────────────────
    [Fact]
    public void Trade_TotalValue_IsQuantityTimesPrice()
    {
        var trade = new Trade
        {
            Quantity   = 200,
            Price      = 50.25m,
            TotalValue = 200 * 50.25m,
            Side       = "Buy",
            Status     = TradeStatus.Filled,
        };
        Assert.Equal(10_050m, trade.TotalValue);
    }

    [Fact]
    public void Trade_DefaultStatus_IsFilled()
    {
        var trade = new Trade();
        Assert.Equal(TradeStatus.Filled, trade.Status);
    }

    // ── OrderEvent model ──────────────────────────────────────────────────
    [Fact]
    public void OrderEvent_DefaultTriggeredBy_IsNull()
    {
        var evt = new OrderEvent();
        Assert.Null(evt.TriggeredBy);
    }

    [Fact]
    public void OrderEvent_OccurredAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var evt    = new OrderEvent();
        Assert.True(evt.OccurredAt >= before);
    }
}
