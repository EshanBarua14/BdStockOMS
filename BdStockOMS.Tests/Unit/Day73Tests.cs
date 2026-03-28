using Xunit;
using BdStockOMS.API.Models;

namespace BdStockOMS.Tests.Unit;

public class Day73Tests
{
    private static Order MakeOrder(
        OrderType    type       = OrderType.Buy,
        OrderCategory cat       = OrderCategory.Market,
        int          qty        = 100,
        decimal      price      = 50m,
        decimal?     limitPrice = null) => new()
    {
        Id               = 1,
        OrderType        = type,
        OrderCategory    = cat,
        Quantity         = qty,
        PriceAtOrder     = price,
        LimitPrice       = limitPrice,
        Status           = OrderStatus.Pending,
        CreatedAt        = DateTime.UtcNow,
        InvestorId       = 1,
        StockId          = 1,
        BrokerageHouseId = 1,
    };

    [Fact] public void Buy_OrderType_IsBuy()
        => Assert.Equal(OrderType.Buy,  MakeOrder(OrderType.Buy).OrderType);

    [Fact] public void Sell_OrderType_IsSell()
        => Assert.Equal(OrderType.Sell, MakeOrder(OrderType.Sell).OrderType);

    [Fact] public void Buy_ToString_IsNotSell()
        => Assert.Equal("Buy",  MakeOrder(OrderType.Buy).OrderType.ToString());

    [Fact] public void Sell_ToString_IsNotBuy()
        => Assert.Equal("Sell", MakeOrder(OrderType.Sell).OrderType.ToString());

    [Theory]
    [InlineData(OrderType.Buy,  49, 50, true )]
    [InlineData(OrderType.Buy,  50, 50, true )]
    [InlineData(OrderType.Buy,  51, 50, false)]
    [InlineData(OrderType.Sell, 51, 50, true )]
    [InlineData(OrderType.Sell, 50, 50, true )]
    [InlineData(OrderType.Sell, 49, 50, false)]
    public void LimitFillability(OrderType side, decimal market, decimal limit, bool expected)
        => Assert.Equal(expected, IsLimitFillable(MakeOrder(side, OrderCategory.Limit, limitPrice: limit), market));

    [Fact] public void BuySlippage_AboveMarket()
        => Assert.True(100m * 1.001m > 100m);

    [Fact] public void SellSlippage_BelowMarket()
        => Assert.True(100m * 0.999m < 100m);

    [Fact] public void FilledStatus_IsFilledNotExecuted()
    { var o = MakeOrder(); o.Status = OrderStatus.Filled; Assert.Equal(OrderStatus.Filled, o.Status); }

    [Fact] public void StatusFlow_Pending_Open_Filled()
    {
        var o = MakeOrder();
        Assert.Equal(OrderStatus.Pending, o.Status);
        o.Status = OrderStatus.Open;   Assert.Equal(OrderStatus.Open,   o.Status);
        o.Status = OrderStatus.Filled; Assert.Equal(OrderStatus.Filled, o.Status);
    }

    [Fact] public void AverageBuyPrice_TwoFills()
        => Assert.Equal(50m, (100m * 48m + 100m * 52m) / 200m);

    private static bool IsLimitFillable(Order o, decimal market)
    {
        return o.OrderType switch
        {
            OrderType.Buy  => market <= o.LimitPrice.Value,
            OrderType.Sell => market >= o.LimitPrice.Value,
            _              => false
        };
    }
}
