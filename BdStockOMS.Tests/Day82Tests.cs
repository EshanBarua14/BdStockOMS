using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Xunit;

namespace BdStockOMS.Tests;

public class Day82Tests
{
    // ── ExchangeOrderRouter ──────────────────────────────────────────

    [Theory]
    [InlineData(Board.Block,   OrderCategory.Market,      false)]
    [InlineData(Board.Block,   OrderCategory.MarketAtBest,false)]
    [InlineData(Board.Block,   OrderCategory.Limit,       true)]
    [InlineData(Board.BuyIn,   OrderCategory.Market,      false)]
    [InlineData(Board.BuyIn,   OrderCategory.Limit,       true)]
    [InlineData(Board.SPublic, OrderCategory.Market,      false)]
    [InlineData(Board.SPublic, OrderCategory.Limit,       true)]
    public void Router_BlockBuyInSPublic_OnlyAcceptsLimit(Board board, OrderCategory cat, bool expected)
    {
        var result = ExchangeOrderRouter.Route(ExchangeId.DSE, board, cat);
        Assert.Equal(expected, result.IsAccepted);
    }

    [Theory]
    [InlineData(Board.Government, ExchangeId.DSE, true)]
    [InlineData(Board.Government, ExchangeId.CSE, false)]
    [InlineData(Board.Debt,       ExchangeId.DSE, true)]
    [InlineData(Board.Debt,       ExchangeId.CSE, false)]
    [InlineData(Board.SME,        ExchangeId.DSE, true)]
    [InlineData(Board.SME,        ExchangeId.CSE, false)]
    public void Router_GovDebtSme_DseOnly(Board board, ExchangeId exchange, bool expected)
    {
        var result = ExchangeOrderRouter.Route(exchange, board, OrderCategory.Limit);
        Assert.Equal(expected, result.IsAccepted);
    }

    [Theory]
    [InlineData(Board.Public,    ExchangeId.DSE, OrderCategory.Market,      true)]
    [InlineData(Board.Public,    ExchangeId.CSE, OrderCategory.Market,      true)]
    [InlineData(Board.ATBPublic, ExchangeId.DSE, OrderCategory.Limit,       true)]
    [InlineData(Board.ATBPublic, ExchangeId.CSE, OrderCategory.MarketAtBest,true)]
    public void Router_PublicATB_AllowsAllExchangesAndTypes(Board board, ExchangeId ex, OrderCategory cat, bool expected)
    {
        var result = ExchangeOrderRouter.Route(ex, board, cat);
        Assert.Equal(expected, result.IsAccepted);
    }

    [Fact]
    public void Router_Accepted_HasExchange()
    {
        var result = ExchangeOrderRouter.Route(ExchangeId.DSE, Board.Public, OrderCategory.Market);
        Assert.True(result.IsAccepted);
        Assert.Equal(ExchangeId.DSE, result.Exchange);
        Assert.NotEmpty(result.Message);
    }

    [Fact]
    public void Router_Rejected_HasNullExchange()
    {
        var result = ExchangeOrderRouter.Route(ExchangeId.CSE, Board.SME, OrderCategory.Limit);
        Assert.False(result.IsAccepted);
        Assert.Null(result.Exchange);
        Assert.NotEmpty(result.Message);
    }

    // ── IsValidForExchange ────────────────────────────────────────────

    [Theory]
    [InlineData("DSE", ExchangeId.DSE, true)]
    [InlineData("DSE", ExchangeId.CSE, false)]
    [InlineData("CSE", ExchangeId.CSE, true)]
    [InlineData("CSE", ExchangeId.DSE, false)]
    [InlineData("",    ExchangeId.DSE, true)]
    public void IsValidForExchange_CorrectRouting(string stockExchange, ExchangeId orderExchange, bool expected)
    {
        Assert.Equal(expected, ExchangeOrderRouter.IsValidForExchange(stockExchange, orderExchange));
    }

    // ── ExchangeId enum ───────────────────────────────────────────────

    [Fact]
    public void ExchangeId_HasDseAndCse()
    {
        Assert.True(Enum.IsDefined(typeof(ExchangeId), ExchangeId.DSE));
        Assert.True(Enum.IsDefined(typeof(ExchangeId), ExchangeId.CSE));
    }

    // ── Board enum routing rules ──────────────────────────────────────

    [Fact]
    public void Board_AllEightValuesExist()
    {
        var expected = new[] { "Public","SME","ATBPublic","Government","Debt","Block","BuyIn","SPublic" };
        var actual = Enum.GetNames<Board>();
        foreach (var name in expected)
            Assert.Contains(name, actual);
    }

    [Fact]
    public void Board_BlockOrdinalIs5()     => Assert.Equal(5, (int)Board.Block);
    [Fact]
    public void Board_BuyInOrdinalIs6()     => Assert.Equal(6, (int)Board.BuyIn);
    [Fact]
    public void Board_SPublicOrdinalIs7()   => Assert.Equal(7, (int)Board.SPublic);

    // ── CseStockTick record ───────────────────────────────────────────

    [Fact]
    public void CseStockTick_CanBeCreated()
    {
        var tick = new CseStockTick("BRACBANK", 45.50m, 1.20m, 2.71m, "up");
        Assert.Equal("BRACBANK", tick.TradingCode);
        Assert.Equal(45.50m,     tick.LastTradePrice);
        Assert.Equal(1.20m,      tick.Change);
        Assert.Equal(2.71m,      tick.ChangePercent);
        Assert.Equal("up",       tick.Direction);
    }

    [Fact]
    public void CseIndexData_CanBeCreated()
    {
        var idx = new CseIndexData(12500.50m, 8200.25m, -45.30m, DateTime.UtcNow);
        Assert.Equal(12500.50m, idx.CASPI);
        Assert.Equal(8200.25m,  idx.CSE30);
        Assert.Equal(-45.30m,   idx.CASPIChange);
    }

    // ── DseStockTick record ───────────────────────────────────────────

    [Fact]
    public void DseStockTick_CanBeCreated()
    {
        var tick = new DseStockTick("GP", 380.00m, 2.50m, 0.66m, "up");
        Assert.Equal("GP",     tick.TradingCode);
        Assert.Equal(380.00m,  tick.LastTradePrice);
        Assert.Equal("up",     tick.Direction);
    }

    // ── Market hours (IsMarketOpen logic) ────────────────────────────

    [Fact]
    public void MarketHours_SundayToThursdayAreWeekdays()
    {
        var weekdays = new[] {
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday,
            DayOfWeek.Wednesday, DayOfWeek.Thursday
        };
        foreach (var day in weekdays)
            Assert.DoesNotContain(day, new[] { DayOfWeek.Friday, DayOfWeek.Saturday });
    }

    [Fact]
    public void MarketHours_FridaySaturdayAreClosed()
    {
        var closed = new[] { DayOfWeek.Friday, DayOfWeek.Saturday };
        Assert.Contains(DayOfWeek.Friday, closed);
        Assert.Contains(DayOfWeek.Saturday, closed);
    }

    // ── ExchangeRouteResult ───────────────────────────────────────────

    [Fact]
    public void ExchangeRouteResult_AcceptedHasCorrectProperties()
    {
        var r = ExchangeRouteResult.Accepted(ExchangeId.CSE, "OK");
        Assert.True(r.IsAccepted);
        Assert.Equal(ExchangeId.CSE, r.Exchange);
        Assert.Equal("OK", r.Message);
    }

    [Fact]
    public void ExchangeRouteResult_RejectedHasCorrectProperties()
    {
        var r = ExchangeRouteResult.Rejected("Not allowed");
        Assert.False(r.IsAccepted);
        Assert.Null(r.Exchange);
        Assert.Equal("Not allowed", r.Message);
    }

    // ── Full routing matrix ───────────────────────────────────────────

    [Theory]
    [InlineData(ExchangeId.DSE, Board.Public,    OrderCategory.Market,       true)]
    [InlineData(ExchangeId.DSE, Board.Public,    OrderCategory.Limit,        true)]
    [InlineData(ExchangeId.DSE, Board.Public,    OrderCategory.MarketAtBest, true)]
    [InlineData(ExchangeId.CSE, Board.Public,    OrderCategory.Market,       true)]
    [InlineData(ExchangeId.CSE, Board.Public,    OrderCategory.Limit,        true)]
    [InlineData(ExchangeId.DSE, Board.Block,     OrderCategory.Limit,        true)]
    [InlineData(ExchangeId.DSE, Board.Block,     OrderCategory.Market,       false)]
    [InlineData(ExchangeId.DSE, Board.BuyIn,     OrderCategory.Limit,        true)]
    [InlineData(ExchangeId.DSE, Board.BuyIn,     OrderCategory.Market,       false)]
    [InlineData(ExchangeId.DSE, Board.SME,       OrderCategory.Limit,        true)]
    [InlineData(ExchangeId.CSE, Board.SME,       OrderCategory.Limit,        false)]
    [InlineData(ExchangeId.DSE, Board.Government,OrderCategory.Limit,        true)]
    [InlineData(ExchangeId.CSE, Board.Government,OrderCategory.Limit,        false)]
    [InlineData(ExchangeId.DSE, Board.Debt,      OrderCategory.Limit,        true)]
    [InlineData(ExchangeId.CSE, Board.Debt,      OrderCategory.Limit,        false)]
    public void FullRoutingMatrix(ExchangeId ex, Board board, OrderCategory cat, bool expected)
    {
        var result = ExchangeOrderRouter.Route(ex, board, cat);
        Assert.Equal(expected, result.IsAccepted);
    }
}
