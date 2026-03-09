using BdStockOMS.API.Data;
using BdStockOMS.API.Exchange;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

public class MarketDepthServiceTests
{
    private static AppDbContext BuildDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(opts);
    }

    private static Stock BuildStock(int id = 1, string code = "SQPHARMA",
        string exchange = "DSE") => new()
    {
        Id             = id,
        TradingCode    = code,
        CompanyName    = $"{code} Ltd",
        Exchange       = exchange,
        Category       = StockCategory.A,
        LastTradePrice = 100m,
    };

    private static MarketDepthDto BuildDepthDto(string code = "SQPHARMA",
        decimal basePrice = 100m) => new(
        code,
        Bids: Enumerable.Range(1, 5)
            .Select(i => new DepthLevelDto(basePrice - i, 1000 * i))
            .ToList(),
        Asks: Enumerable.Range(1, 5)
            .Select(i => new DepthLevelDto(basePrice + i, 1000 * i))
            .ToList());

    private static (MarketDepthService svc, Mock<IExchangeConnectorFactory> factoryMock)
        BuildService(AppDbContext db, MarketDepthDto? depthDto = null)
    {
        var connectorMock = new Mock<IExchangeConnector>();
        connectorMock.Setup(c => c.GetMarketDepthAsync(It.IsAny<string>()))
            .ReturnsAsync(depthDto ?? BuildDepthDto());

        var factoryMock = new Mock<IExchangeConnectorFactory>();
        factoryMock.Setup(f => f.GetConnector(It.IsAny<string>()))
            .Returns(connectorMock.Object);

        return (new MarketDepthService(db, factoryMock.Object), factoryMock);
    }

    // ── UpsertDepthAsync ──────────────────────────────────────────────────
    [Fact]
    public async Task UpsertDepth_CreatesNewRecord_WhenNotExists()
    {
        var db  = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        var dto      = BuildDepthDto();

        await svc.UpsertDepthAsync(1, dto);

        Assert.Equal(1, await db.MarketDepths.CountAsync());
    }

    [Fact]
    public async Task UpsertDepth_UpdatesExistingRecord_WhenExists()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        var dto      = BuildDepthDto(basePrice: 100m);

        await svc.UpsertDepthAsync(1, dto);
        await svc.UpsertDepthAsync(1, BuildDepthDto(basePrice: 110m));

        Assert.Equal(1, await db.MarketDepths.CountAsync());
    }

    [Fact]
    public async Task UpsertDepth_MapsBidLevels_Correctly()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        var dto      = BuildDepthDto(basePrice: 100m);

        var depth = await svc.UpsertDepthAsync(1, dto);

        Assert.Equal(99m, depth.Bid1Price);
        Assert.Equal(98m, depth.Bid2Price);
        Assert.Equal(1000, depth.Bid1Qty);
        Assert.Equal(2000, depth.Bid2Qty);
    }

    [Fact]
    public async Task UpsertDepth_MapsAskLevels_Correctly()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        var dto      = BuildDepthDto(basePrice: 100m);

        var depth = await svc.UpsertDepthAsync(1, dto);

        Assert.Equal(101m, depth.Ask1Price);
        Assert.Equal(102m, depth.Ask2Price);
        Assert.Equal(1000, depth.Ask1Qty);
        Assert.Equal(2000, depth.Ask2Qty);
    }

    [Fact]
    public async Task UpsertDepth_SetsExchange_FromStock()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock(exchange: "CSE"));
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        var depth    = await svc.UpsertDepthAsync(1, BuildDepthDto());

        Assert.Equal("CSE", depth.Exchange);
    }

    [Fact]
    public async Task UpsertDepth_UpdatesTimestamp()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var before   = DateTime.UtcNow.AddSeconds(-1);
        var (svc, _) = BuildService(db);
        var depth    = await svc.UpsertDepthAsync(1, BuildDepthDto());

        Assert.True(depth.UpdatedAt >= before);
    }

    // ── GetDepthAsync ─────────────────────────────────────────────────────
    [Fact]
    public async Task GetDepth_ReturnsNull_WhenNoRecord()
    {
        var db       = BuildDb();
        var (svc, _) = BuildService(db);

        var result = await svc.GetDepthAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetDepth_ReturnsSnapshot_WhenExists()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        await svc.UpsertDepthAsync(1, BuildDepthDto());

        var snapshot = await svc.GetDepthAsync(1);

        Assert.NotNull(snapshot);
        Assert.Equal(1, snapshot!.StockId);
        Assert.Equal(5, snapshot.Bids.Count);
        Assert.Equal(5, snapshot.Asks.Count);
    }

    [Fact]
    public async Task GetDepth_Snapshot_HasCorrectExchange()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock(exchange: "DSE"));
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        await svc.UpsertDepthAsync(1, BuildDepthDto());

        var snapshot = await svc.GetDepthAsync(1);

        Assert.Equal("DSE", snapshot!.Exchange);
    }

    [Fact]
    public async Task GetDepth_Bids_AreInDescendingPriceOrder()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        await svc.UpsertDepthAsync(1, BuildDepthDto(basePrice: 100m));

        var snapshot = await svc.GetDepthAsync(1);
        var prices   = snapshot!.Bids.Select(b => b.Price).ToList();

        Assert.True(prices[0] > prices[1]);
        Assert.True(prices[1] > prices[2]);
    }

    [Fact]
    public async Task GetDepth_Asks_AreInAscendingPriceOrder()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        await svc.UpsertDepthAsync(1, BuildDepthDto(basePrice: 100m));

        var snapshot = await svc.GetDepthAsync(1);
        var prices   = snapshot!.Asks.Select(a => a.Price).ToList();

        Assert.True(prices[0] < prices[1]);
        Assert.True(prices[1] < prices[2]);
    }

    // ── RefreshDepthAsync ─────────────────────────────────────────────────
    [Fact]
    public async Task RefreshDepth_CallsExchangeConnector()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, factoryMock) = BuildService(db);
        await svc.RefreshDepthAsync(1);

        factoryMock.Verify(f => f.GetConnector("DSE"), Times.Once);
    }

    [Fact]
    public async Task RefreshDepth_ThrowsForUnknownStock()
    {
        var db       = BuildDb();
        var (svc, _) = BuildService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RefreshDepthAsync(999));
    }

    [Fact]
    public async Task RefreshDepth_PersistsDepthToDatabase()
    {
        var db = BuildDb();
        db.Stocks.Add(BuildStock());
        await db.SaveChangesAsync();

        var (svc, _) = BuildService(db);
        await svc.RefreshDepthAsync(1);

        Assert.Equal(1, await db.MarketDepths.CountAsync());
    }

    // ── MarketDepth model ─────────────────────────────────────────────────
    [Fact]
    public void MarketDepth_UpdatedAt_DefaultsToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var depth  = new MarketDepth();
        Assert.True(depth.UpdatedAt >= before);
    }

    [Fact]
    public void MarketDepthSnapshot_HasFiveBidsAndFiveAsks()
    {
        var snapshot = new MarketDepthSnapshot(1, "TEST", "DSE",
            Enumerable.Range(1, 5).Select(i => new DepthLevelDto(100m - i, 1000)).ToList(),
            Enumerable.Range(1, 5).Select(i => new DepthLevelDto(100m + i, 1000)).ToList(),
            DateTime.UtcNow);

        Assert.Equal(5, snapshot.Bids.Count);
        Assert.Equal(5, snapshot.Asks.Count);
    }
}
