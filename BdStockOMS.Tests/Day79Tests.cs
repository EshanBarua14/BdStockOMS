using BdStockOMS.API.Models;
using BdStockOMS.API.DTOs.Order;
using Xunit;

namespace BdStockOMS.Tests;

public class Day79Tests
{
    [Fact]
    public void OrderCategory_HasMarketAtBest()
        => Assert.True(Enum.IsDefined(typeof(OrderCategory), OrderCategory.MarketAtBest));

    [Fact]
    public void TimeInForce_HasDayIocFok()
    {
        Assert.True(Enum.IsDefined(typeof(TimeInForce), TimeInForce.Day));
        Assert.True(Enum.IsDefined(typeof(TimeInForce), TimeInForce.IOC));
        Assert.True(Enum.IsDefined(typeof(TimeInForce), TimeInForce.FOK));
    }

    [Fact]
    public void ExchangeId_HasDseAndCse()
    {
        Assert.True(Enum.IsDefined(typeof(ExchangeId), ExchangeId.DSE));
        Assert.True(Enum.IsDefined(typeof(ExchangeId), ExchangeId.CSE));
    }

    [Theory]
    [InlineData(Board.Public)]
    [InlineData(Board.SME)]
    [InlineData(Board.ATBPublic)]
    [InlineData(Board.Government)]
    [InlineData(Board.Debt)]
    [InlineData(Board.Block)]
    [InlineData(Board.BuyIn)]
    [InlineData(Board.SPublic)]
    public void Board_AllEightValuesExist(Board board)
        => Assert.True(Enum.IsDefined(typeof(Board), board));

    [Fact]
    public void ExecInstruction_HasAllValues()
    {
        Assert.True(Enum.IsDefined(typeof(ExecInstruction), ExecInstruction.None));
        Assert.True(Enum.IsDefined(typeof(ExecInstruction), ExecInstruction.Suspend));
        Assert.True(Enum.IsDefined(typeof(ExecInstruction), ExecInstruction.Release));
        Assert.True(Enum.IsDefined(typeof(ExecInstruction), ExecInstruction.WholeOrNone));
    }

    [Theory]
    [InlineData(OrderStatus.Queued)]
    [InlineData(OrderStatus.Submitted)]
    [InlineData(OrderStatus.Waiting)]
    [InlineData(OrderStatus.Deleted)]
    [InlineData(OrderStatus.CancelRequested)]
    [InlineData(OrderStatus.EditRequested)]
    [InlineData(OrderStatus.Replaced)]
    [InlineData(OrderStatus.Private)]
    public void OrderStatus_NewValuesExist(OrderStatus status)
        => Assert.True(Enum.IsDefined(typeof(OrderStatus), status));

    [Fact]
    public void OrderStatus_LegacyValuesUnchanged()
    {
        Assert.True(Enum.IsDefined(typeof(OrderStatus), OrderStatus.Pending));
        Assert.True(Enum.IsDefined(typeof(OrderStatus), OrderStatus.Open));
        Assert.True(Enum.IsDefined(typeof(OrderStatus), OrderStatus.PartiallyFilled));
        Assert.True(Enum.IsDefined(typeof(OrderStatus), OrderStatus.Filled));
        Assert.True(Enum.IsDefined(typeof(OrderStatus), OrderStatus.Completed));
        Assert.True(Enum.IsDefined(typeof(OrderStatus), OrderStatus.Cancelled));
        Assert.True(Enum.IsDefined(typeof(OrderStatus), OrderStatus.Rejected));
    }

    [Fact]
    public void OrderStatus_NeverHasExecuted()
        => Assert.DoesNotContain("Executed", Enum.GetNames<OrderStatus>());

    [Fact]
    public void AggressorSide_HasNoneBuySell()
    {
        Assert.True(Enum.IsDefined(typeof(AggressorSide), AggressorSide.None));
        Assert.True(Enum.IsDefined(typeof(AggressorSide), AggressorSide.Buy));
        Assert.True(Enum.IsDefined(typeof(AggressorSide), AggressorSide.Sell));
    }

    [Fact]
    public void Order_DefaultsAreCorrect()
    {
        var o = new Order();
        Assert.Equal(TimeInForce.Day,      o.TimeInForce);
        Assert.Equal(ExchangeId.DSE,       o.ExchangeId);
        Assert.Equal(Board.Public,         o.Board);
        Assert.Equal(ExecInstruction.None, o.ExecInstruction);
        Assert.Equal(OrderStatus.Pending,  o.Status);
        Assert.Equal(AggressorSide.None,   o.AggressorIndicator);
        Assert.False(o.IsPrivate);
        Assert.Equal(0, o.ExecutedQuantity);
    }

    [Fact]
    public void Order_NullableNewFieldsAreNull()
    {
        var o = new Order();
        Assert.Null(o.MinQty);
        Assert.Null(o.DisplayQty);
        Assert.Null(o.ClOrdID);
        Assert.Null(o.OrigClOrdID);
        Assert.Null(o.TrdMatchID);
        Assert.Null(o.SettlDate);
        Assert.Null(o.GrossTradeAmt);
    }

    [Fact]
    public void Order_PrivateOrderCanBeSet()
    {
        var o = new Order { IsPrivate = true, Status = OrderStatus.Private };
        Assert.True(o.IsPrivate);
        Assert.Equal(OrderStatus.Private, o.Status);
    }

    [Fact]
    public void Order_IcebergFieldsWork()
    {
        var o = new Order { Quantity = 1000, DisplayQty = 100, MinQty = 50 };
        Assert.Equal(100, o.DisplayQty);
        Assert.Equal(50,  o.MinQty);
    }

    [Fact]
    public void Order_FIXFieldsWork()
    {
        var o = new Order
        {
            ClOrdID    = "ORD001",
            OrigClOrdID = "ORD000",
            TrdMatchID = "MATCH1",
            SettlDate  = "20240103",
            GrossTradeAmt = 500000m
        };
        Assert.Equal("ORD001",    o.ClOrdID);
        Assert.Equal("ORD000",    o.OrigClOrdID);
        Assert.Equal("MATCH1",    o.TrdMatchID);
        Assert.Equal("20240103",  o.SettlDate);
        Assert.Equal(500000m,     o.GrossTradeAmt);
    }

    [Fact]
    public void Order_UpdatedAtExists()
    {
        var o = new Order();
        Assert.True(o.UpdatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public void PlaceOrderDto_NewFieldDefaults()
    {
        var dto = new PlaceOrderDto();
        Assert.Equal(TimeInForce.Day,      dto.TimeInForce);
        Assert.Equal(ExchangeId.DSE,       dto.ExchangeId);
        Assert.Equal(Board.Public,         dto.Board);
        Assert.Equal(ExecInstruction.None, dto.ExecInstruction);
        Assert.False(dto.IsPrivate);
    }

    [Fact]
    public void PlaceOrderDto_AcceptsMarketAtBest()
    {
        var dto = new PlaceOrderDto
        {
            OrderCategory = OrderCategory.MarketAtBest,
            TimeInForce   = TimeInForce.IOC,
            ExchangeId    = ExchangeId.CSE,
            Board         = Board.SME,
            Quantity      = 500
        };
        Assert.Equal(OrderCategory.MarketAtBest, dto.OrderCategory);
        Assert.Equal(TimeInForce.IOC,            dto.TimeInForce);
        Assert.Equal(ExchangeId.CSE,             dto.ExchangeId);
        Assert.Equal(Board.SME,                  dto.Board);
    }

    [Fact]
    public void OrderResponseDto_HasAllNewFields()
    {
        var dto = new OrderResponseDto
        {
            TimeInForce       = TimeInForce.FOK,
            ExchangeId        = ExchangeId.CSE,
            Board             = Board.Block,
            ExecInstruction   = ExecInstruction.Suspend,
            IsPrivate         = true,
            MinQty            = 100,
            DisplayQty        = 50,
            ExecutedQuantity  = 200,
            GrossTradeAmt     = 99000m,
            AggressorIndicator = AggressorSide.Buy,
            ClOrdID           = "CL001",
            OrigClOrdID       = "CL000",
            TrdMatchID        = "TM001",
            SettlDate         = "20240105"
        };
        Assert.Equal(TimeInForce.FOK,      dto.TimeInForce);
        Assert.Equal(Board.Block,          dto.Board);
        Assert.Equal(AggressorSide.Buy,    dto.AggressorIndicator);
        Assert.Equal("TM001",              dto.TrdMatchID);
        Assert.Equal(99000m,               dto.GrossTradeAmt);
    }

    [Fact]
    public void Board_BlockBuyInSPublicHaveCorrectOrdinals()
    {
        Assert.Equal(5, (int)Board.Block);
        Assert.Equal(6, (int)Board.BuyIn);
        Assert.Equal(7, (int)Board.SPublic);
    }
}
