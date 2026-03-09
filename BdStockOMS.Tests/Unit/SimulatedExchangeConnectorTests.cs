using BdStockOMS.API.Exchange;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class SimulatedExchangeConnectorTests
{
    private SimulatedExchangeConnector Build(string exchange = "DSE")
        => new(exchange, NullLogger<SimulatedExchangeConnector>.Instance);

    [Fact]
    public async Task ConnectAsync_SetsIsConnected_True()
    {
        var c = Build();
        var result = await c.ConnectAsync();
        Assert.True(result);
        Assert.True(c.IsConnected);
    }

    [Fact]
    public async Task DisconnectAsync_SetsIsConnected_False()
    {
        var c = Build();
        await c.ConnectAsync();
        await c.DisconnectAsync();
        Assert.False(c.IsConnected);
    }

    [Theory]
    [InlineData("DSE")]
    [InlineData("CSE")]
    public void ExchangeCode_ReturnsCorrectCode(string code)
    {
        var c = Build(code);
        Assert.Equal(code, c.ExchangeCode);
    }

    [Fact]
    public async Task GetLatestPriceAsync_ReturnsValidTick_ForKnownCode()
    {
        var c    = Build();
        var tick = await c.GetLatestPriceAsync("SQPHARMA");
        Assert.Equal("SQPHARMA", tick.TradingCode);
        Assert.True(tick.LastTradePrice > 0);
        Assert.True(tick.Volume > 0);
        Assert.True(tick.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task GetLatestPriceAsync_ReturnsDefaultPrice_ForUnknownCode()
    {
        var c    = Build();
        var tick = await c.GetLatestPriceAsync("UNKNOWN");
        Assert.True(tick.LastTradePrice > 0);
    }

    [Fact]
    public async Task GetMarketDepthAsync_ReturnsFiveBidsAndFiveAsks()
    {
        var c     = Build();
        var depth = await c.GetMarketDepthAsync("BEXIMCO");
        Assert.Equal(5, depth.Bids.Count);
        Assert.Equal(5, depth.Asks.Count);
    }

    [Fact]
    public async Task GetMarketDepthAsync_BidsLowerThanAsks()
    {
        var c     = Build();
        var depth = await c.GetMarketDepthAsync("BEXIMCO");
        Assert.True(depth.Bids.Max(b => b.Price) < depth.Asks.Min(a => a.Price));
    }

    [Fact]
    public async Task GetMarketDepthAsync_AllQuantitiesPositive()
    {
        var c     = Build();
        var depth = await c.GetMarketDepthAsync("GRAMEEN");
        Assert.All(depth.Bids, b => Assert.True(b.Quantity > 0));
        Assert.All(depth.Asks, a => Assert.True(a.Quantity > 0));
    }

    [Fact]
    public async Task GetHistoricalDataAsync_ExcludesWeekends()
    {
        var c    = Build();
        var from = new DateTime(2024, 1, 1);
        var to   = new DateTime(2024, 1, 31);
        var data = await c.GetHistoricalDataAsync("RENATA", from, to);
        Assert.All(data, d => Assert.NotEqual(DayOfWeek.Saturday, d.Date.DayOfWeek));
        Assert.All(data, d => Assert.NotEqual(DayOfWeek.Sunday,   d.Date.DayOfWeek));
    }

    [Fact]
    public async Task GetHistoricalDataAsync_HighAlwaysGreaterOrEqualToLow()
    {
        var c    = Build();
        var from = new DateTime(2024, 1, 1);
        var to   = new DateTime(2024, 3, 31);
        var data = await c.GetHistoricalDataAsync("BRAC", from, to);
        Assert.All(data, d => Assert.True(d.High >= d.Low));
    }

    [Fact]
    public async Task GetHistoricalDataAsync_ReturnsEmpty_WhenRangeIsWeekend()
    {
        var c    = Build();
        var from = new DateTime(2024, 1, 6); // Saturday
        var to   = new DateTime(2024, 1, 7); // Sunday
        var data = await c.GetHistoricalDataAsync("GRAMEEN", from, to);
        Assert.Empty(data);
    }

    [Fact]
    public async Task SendOrderAsync_ReturnsResult_WithMatchingOrderId()
    {
        var c   = Build();
        var req = new ExchangeOrderRequest("ORD-001", "SQPHARMA",
            "Buy", "Limit", 245.00m, 100, 1);
        var result = await c.SendOrderAsync(req);
        Assert.Equal("ORD-001", result.ExchangeOrderId);
        Assert.NotNull(result.Status);
    }

    [Fact]
    public async Task SendOrderAsync_SuccessResult_HasAcknowledgedStatus()
    {
        var c   = Build();
        var req = new ExchangeOrderRequest("ORD-002", "BEXIMCO",
            "Sell", "Market", 18.20m, 50, 1);
        ExchangeOrderResult? success = null;
        for (int i = 0; i < 50; i++)
        {
            var r = await c.SendOrderAsync(req);
            if (r.Success) { success = r; break; }
        }
        Assert.NotNull(success);
        Assert.Equal("Acknowledged", success!.Status);
    }

    [Fact]
    public async Task CancelOrderAsync_ReturnsSuccess_WithCancelledStatus()
    {
        var c      = Build();
        var result = await c.CancelOrderAsync("ORD-999");
        Assert.True(result.Success);
        Assert.Equal("Cancelled", result.Status);
        Assert.Equal("ORD-999",   result.ExchangeOrderId);
    }

    [Fact]
    public async Task GetOrderStatusAsync_ReturnsFilledStatus()
    {
        var c      = Build();
        var status = await c.GetOrderStatusAsync("ORD-123");
        Assert.Equal("ORD-123", status.ExchangeOrderId);
        Assert.Equal("Filled",  status.Status);
        Assert.True(status.FilledQuantity > 0);
        Assert.True(status.AveragePrice   > 0);
    }
}
