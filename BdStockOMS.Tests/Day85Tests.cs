using BdStockOMS.API.FIX;
using BdStockOMS.API.Models;
using Microsoft.Extensions.DependencyInjection;
using BdStockOMS.API.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests;

public class Day85Tests
{
    private static SimulatedFIXConnector MakeConnector()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(opts);
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<SimulatedFIXConnector>.Instance;
        return new SimulatedFIXConnector(new FakeScopeFactory2(db), logger, "DSE");
    }

    private static FIXOrderRequest MakeReq(string id, OrderType side, OrderCategory cat,
        TimeInForce tif, int qty, decimal? price = null) => new()
    {
        ClOrdID = id, Symbol = "TEST", StockId = 1,
        OrderType = side, Category = cat, TimeInForce = tif,
        Quantity = qty, Price = price,
        Exchange = ExchangeId.DSE, Board = Board.Public, BrokerageHouseId = 1
    };

    // ── FIXCertScenario enum ─────────────────────────────────────────

    [Fact]
    public void FIXCertScenario_HasTwelveScenarios()
        => Assert.Equal(12, Enum.GetValues<FIXCertScenario>().Length);

    [Theory]
    [InlineData(FIXCertScenario.S1_MarketOrder,             1)]
    [InlineData(FIXCertScenario.S2_LimitOrder,              2)]
    [InlineData(FIXCertScenario.S3_MarketAtBest,            3)]
    [InlineData(FIXCertScenario.S4_IOC_Limit,               4)]
    [InlineData(FIXCertScenario.S5_FOK_Limit,               5)]
    [InlineData(FIXCertScenario.S6_PrivateOrder,            6)]
    [InlineData(FIXCertScenario.S7_IcebergOrder,            7)]
    [InlineData(FIXCertScenario.S8_MinQtyOrder,             8)]
    [InlineData(FIXCertScenario.S9_CancelPendingOrder,      9)]
    [InlineData(FIXCertScenario.S10_AmendPendingOrder,     10)]
    [InlineData(FIXCertScenario.S11_PartialFillThenCancel, 11)]
    [InlineData(FIXCertScenario.S12_RejectInvalidOrder,    12)]
    public void FIXCertScenario_OrdinalsCorrect(FIXCertScenario s, int expected)
        => Assert.Equal(expected, (int)s);

    // ── FIXOrderTypeValidator ────────────────────────────────────────

    [Fact]
    public void Validator_ValidMarketOrder_Passes()
    {
        var req = MakeReq("T1", OrderType.Buy, OrderCategory.Market, TimeInForce.Day, 100);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.True(r.IsValid);
        Assert.Equal("1", r.FIXOrdType);
        Assert.Equal("1", r.FIXSide);
        Assert.Equal("0", r.FIXTimeInForce);
    }

    [Fact]
    public void Validator_ValidLimitOrder_Passes()
    {
        var req = MakeReq("T2", OrderType.Sell, OrderCategory.Limit, TimeInForce.Day, 50, 380.50m);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.True(r.IsValid);
        Assert.Equal("2", r.FIXOrdType);
        Assert.Equal("2", r.FIXSide);
    }

    [Fact]
    public void Validator_LimitWithoutPrice_Fails()
    {
        var req = MakeReq("T3", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 100);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("Price"));
    }

    [Fact]
    public void Validator_ZeroQuantity_Fails()
    {
        var req = MakeReq("T4", OrderType.Buy, OrderCategory.Market, TimeInForce.Day, 0);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("Quantity"));
    }

    [Fact]
    public void Validator_FOK_Market_Fails()
    {
        var req = MakeReq("T5", OrderType.Buy, OrderCategory.Market, TimeInForce.FOK, 100);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("FOK"));
    }

    [Fact]
    public void Validator_IOC_Limit_Passes()
    {
        var req = MakeReq("T6", OrderType.Buy, OrderCategory.Limit, TimeInForce.IOC, 100, 50.00m);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.True(r.IsValid);
        Assert.Equal("3", r.FIXTimeInForce);
    }

    [Fact]
    public void Validator_FOK_Limit_Passes()
    {
        var req = MakeReq("T7", OrderType.Buy, OrderCategory.Limit, TimeInForce.FOK, 100, 50.00m);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.True(r.IsValid);
        Assert.Equal("4", r.FIXTimeInForce);
    }

    [Fact]
    public void Validator_MarketAtBest_HasTagP()
    {
        var req = MakeReq("T8", OrderType.Buy, OrderCategory.MarketAtBest, TimeInForce.Day, 200);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.True(r.IsValid);
        Assert.Equal("P", r.FIXOrdType);
    }

    [Fact]
    public void Validator_IcebergDisplayQtyExceedsQty_Fails()
    {
        var req = MakeReq("T9", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 100, 50m);
        req.DisplayQty = 200;
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("DisplayQty"));
    }

    [Fact]
    public void Validator_ValidIceberg_Passes()
    {
        var req = MakeReq("T10", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 1000, 100m);
        req.DisplayQty = 100;
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.True(r.IsValid);
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void Validator_MinQtyExceedsQty_Fails()
    {
        var req = MakeReq("T11", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 100, 50m);
        req.MinQty = 200;
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("MinQty"));
    }

    [Fact]
    public void Validator_ValidMinQty_Passes()
    {
        var req = MakeReq("T12", OrderType.Buy, OrderCategory.Limit, TimeInForce.Day, 1000, 100m);
        req.MinQty = 500;
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.True(r.IsValid);
    }

    [Fact]
    public void Validator_PrivateMarketOrder_Fails()
    {
        var req = MakeReq("T13", OrderType.Buy, OrderCategory.Market, TimeInForce.Day, 100);
        req.IsPrivate = true;
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("Private"));
    }

    [Fact]
    public void Validator_BlockBoardMarket_Fails()
    {
        var req = MakeReq("T14", OrderType.Buy, OrderCategory.Market, TimeInForce.Day, 100);
        req.Board = Board.Block;
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.False(r.IsValid);
        Assert.Contains(r.Errors, e => e.Contains("Block"));
    }

    [Fact]
    public void Validator_MarketAtBestWithPrice_Warns()
    {
        var req = MakeReq("T15", OrderType.Buy, OrderCategory.MarketAtBest, TimeInForce.Day, 100, 50m);
        var r = FIXOrderTypeValidator.Validate(req);
        Assert.True(r.IsValid);
        Assert.NotEmpty(r.Warnings);
    }

    // ── FIX tag mapping ───────────────────────────────────────────────

    [Theory]
    [InlineData(OrderCategory.Market,       "1")]
    [InlineData(OrderCategory.Limit,        "2")]
    [InlineData(OrderCategory.MarketAtBest, "P")]
    public void GetOrdType_ReturnsCorrectTag(OrderCategory cat, string expected)
        => Assert.Equal(expected, FIXOrderTypeValidator.GetOrdType(cat));

    [Theory]
    [InlineData(TimeInForce.Day, "0")]
    [InlineData(TimeInForce.IOC, "3")]
    [InlineData(TimeInForce.FOK, "4")]
    public void GetTIF_ReturnsCorrectTag(TimeInForce tif, string expected)
        => Assert.Equal(expected, FIXOrderTypeValidator.GetTIF(tif));

    [Theory]
    [InlineData(OrderType.Buy,  "1")]
    [InlineData(OrderType.Sell, "2")]
    public void GetSide_ReturnsCorrectTag(OrderType side, string expected)
        => Assert.Equal(expected, FIXOrderTypeValidator.GetSide(side));

    // ── Cert scenario runs ────────────────────────────────────────────

    [Fact]
    public async Task S1_MarketOrder_Passes()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S1_MarketOrder, c);
        Assert.True(r.Passed);
        Assert.Equal(FIXCertScenario.S1_MarketOrder, r.Scenario);
        Assert.NotEmpty(r.Steps);
    }

    [Fact]
    public async Task S2_LimitOrder_Passes()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S2_LimitOrder, c);
        Assert.True(r.Passed);
        Assert.Contains("44=380.50", r.RawFIXMessage ?? "");
    }

    [Fact]
    public async Task S3_MarketAtBest_Passes()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S3_MarketAtBest, c);
        Assert.True(r.Passed);
        Assert.Contains("40=P", r.RawFIXMessage ?? "");
    }

    [Fact]
    public async Task S4_IOC_Passes()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S4_IOC_Limit, c);
        Assert.True(r.Passed);
        Assert.Contains("59=3", r.RawFIXMessage ?? "");
    }

    [Fact]
    public async Task S5_FOK_Passes()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S5_FOK_Limit, c);
        Assert.True(r.Passed);
        Assert.Contains("59=4", r.RawFIXMessage ?? "");
    }

    [Fact]
    public async Task S6_Private_Passes()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S6_PrivateOrder, c);
        Assert.True(r.Passed);
    }

    [Fact]
    public async Task S7_Iceberg_HasDisplayQtyTag()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S7_IcebergOrder, c);
        Assert.True(r.Passed);
        Assert.Contains("1138=100", r.RawFIXMessage ?? "");
    }

    [Fact]
    public async Task S8_MinQty_HasMinQtyTag()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S8_MinQtyOrder, c);
        Assert.True(r.Passed);
        Assert.Contains("110=500", r.RawFIXMessage ?? "");
    }

    [Fact]
    public async Task S9_Cancel_HasCancelMsgType()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S9_CancelPendingOrder, c);
        Assert.True(r.Passed);
        Assert.Contains("35=F", r.RawFIXMessage ?? "");
    }

    [Fact]
    public async Task S10_Amend_HasReplaceMsgType()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S10_AmendPendingOrder, c);
        Assert.True(r.Passed);
        Assert.Contains("35=G", r.RawFIXMessage ?? "");
    }

    [Fact]
    public async Task S11_PartialFill_Passes()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S11_PartialFillThenCancel, c);
        Assert.True(r.Passed);
    }

    [Fact]
    public async Task S12_Reject_ZeroQty_Detected()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var r = await FIXCertScenarioRunner.RunAsync(FIXCertScenario.S12_RejectInvalidOrder, c);
        Assert.True(r.Passed);
        Assert.Contains(r.Steps, s => s.Contains("rejected") || s.Contains("Validation"));
    }

    [Fact]
    public async Task RunAll_Returns12Results()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var results = await FIXCertScenarioRunner.RunAllAsync(c);
        Assert.Equal(12, results.Count);
    }

    [Fact]
    public async Task RunAll_AllPass()
    {
        var c = MakeConnector();
        await c.ConnectAsync();
        var results = await FIXCertScenarioRunner.RunAllAsync(c);
        var failed = results.Where(r => !r.Passed).ToList();
        Assert.Empty(failed);
    }
}

public class FakeScopeFactory2 : IServiceScopeFactory
{
    private readonly AppDbContext _db;
    public FakeScopeFactory2(AppDbContext db) => _db = db;
    public IServiceScope CreateScope() => new FakeScope2(_db);
}
public class FakeScope2 : IServiceScope
{
    public IServiceProvider ServiceProvider { get; }
    public FakeScope2(AppDbContext db) => ServiceProvider = new FakeServiceProvider2(db);
    public void Dispose() { }
}
public class FakeServiceProvider2 : IServiceProvider
{
    private readonly AppDbContext _db;
    public FakeServiceProvider2(AppDbContext db) => _db = db;
    public object? GetService(Type t) => t == typeof(AppDbContext) ? _db : null;
}
