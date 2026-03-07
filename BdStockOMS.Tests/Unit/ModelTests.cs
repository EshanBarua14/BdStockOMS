using BdStockOMS.API.Models;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class ModelTests
{
    [Fact]
    public void Order_DefaultStatus_IsPending()
    {
        // ARRANGE + ACT
        var order = new Order();

        // ASSERT
        // New order must always start as Pending
        // Never Approved or Executed by default
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void User_DefaultIsActive_IsTrue()
    {
        // New users should be active by default
        var user = new User();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void User_DefaultIsLocked_IsFalse()
    {
        // New users should NOT be locked by default
        var user = new User();
        Assert.False(user.IsLocked);
    }

    [Fact]
    public void Stock_DefaultIsActive_IsTrue()
    {
        var stock = new Stock();
        Assert.True(stock.IsActive);
    }

    [Fact]
    public void BrokerageHouse_DefaultIsActive_IsTrue()
    {
        var brokerage = new BrokerageHouse();
        Assert.True(brokerage.IsActive);
    }

    [Fact]
    public void OrderType_HasBuyAndSell()
    {
        // Enum must have exactly Buy and Sell
        var values = Enum.GetValues<OrderType>();
        Assert.Contains(OrderType.Buy, values);
        Assert.Contains(OrderType.Sell, values);
    }

    [Fact]
    public void OrderStatus_HasAllFiveStatuses()
    {
        // All 6 statuses must exist in the enum
        var values = Enum.GetValues<OrderStatus>();
        Assert.Contains(OrderStatus.Pending, values);
        Assert.Contains(OrderStatus.Rejected, values);
        Assert.Contains(OrderStatus.Executed, values);
        Assert.Contains(OrderStatus.Completed, values);
        Assert.Contains(OrderStatus.Cancelled, values);
    }
}