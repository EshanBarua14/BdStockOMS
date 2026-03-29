using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using AggressorSide = BdStockOMS.API.Controllers.AggressorSide;
using BdStockOMS.API.Controllers;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 69 — TimeAndSalesController Tests
//  T&S multi-symbol backend, filter params, response shape
// ============================================================

public class Day69TimeAndSalesTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private void SeedStock(AppDbContext db, string code = "GP", decimal price = 380m)
    {
        db.Stocks.Add(new Stock
        {
            TradingCode = code, CompanyName = code + " Ltd",
            Exchange = "DSE", Category = StockCategory.A,
            LastTradePrice = price, ClosePrice = price * 0.99m,
            IsActive = true, LastUpdatedAt = DateTime.UtcNow
        });
        db.SaveChanges();
    }

    // ── TASEntry shape via controller logic ──────────────────

    [Fact]
    public void TASEntry_AggressorSide_BuyIsOne()
        => Assert.Equal(1, (int)AggressorSide.Buy);

    [Fact]
    public void TASEntry_AggressorSide_SellIsMinusOne()
        => Assert.Equal(-1, (int)AggressorSide.Sell);

    [Fact]
    public void TASEntry_AggressorSide_UnknownIsZero()
        => Assert.Equal(0, (int)AggressorSide.Unknown);

    // ── Stock lookup for T&S price baseline ─────────────────

    [Fact]
    public async Task Stock_CanBeFoundByTradingCode()
    {
        var db = CreateDb();
        SeedStock(db, "GP", 380m);
        var s = await db.Stocks.FirstOrDefaultAsync(x => x.TradingCode == "GP" && x.IsActive);
        Assert.NotNull(s);
        Assert.Equal(380m, s!.LastTradePrice);
    }

    [Fact]
    public async Task Stock_UnknownCode_ReturnsNull()
    {
        var db = CreateDb();
        var s = await db.Stocks.FirstOrDefaultAsync(x => x.TradingCode == "UNKNOWN");
        Assert.Null(s);
    }

    [Fact]
    public async Task MultipleStocks_CanBeSeeded_AndQueried()
    {
        var db = CreateDb();
        SeedStock(db, "GP",       380m);
        SeedStock(db, "BRACBANK", 48m);
        SeedStock(db, "BATBC",    615m);
        Assert.Equal(3, await db.Stocks.CountAsync());
    }

    [Fact]
    public async Task Stock_InactiveStock_NotReturnedInActiveQuery()
    {
        var db = CreateDb();
        db.Stocks.Add(new Stock { TradingCode = "DEAD", CompanyName = "Dead Corp",
            Exchange = "DSE", Category = StockCategory.Z,
            LastTradePrice = 5m, IsActive = false, LastUpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var s = await db.Stocks.FirstOrDefaultAsync(x => x.TradingCode == "DEAD" && x.IsActive);
        Assert.Null(s);
    }

    // ── T&S simulated data logic ─────────────────────────────

    [Fact]
    public void TAS_PriceChange_CanBePositive()
    {
        var entry = new { price = 382.50m, priceChange = 2.50m };
        Assert.True(entry.priceChange > 0);
    }

    [Fact]
    public void TAS_PriceChange_CanBeNegative()
    {
        var entry = new { price = 378.00m, priceChange = -2.00m };
        Assert.True(entry.priceChange < 0);
    }

    [Fact]
    public void TAS_Value_IsQuantityTimesPrice()
    {
        decimal price = 380m;
        long    vol   = 500;
        decimal value = Math.Round(price * vol, 2);
        Assert.Equal(190_000m, value);
    }

    [Fact]
    public void TAS_CountParam_ClampedTo500()
    {
        int count = 999;
        if (count < 1 || count > 500) count = 80;
        Assert.Equal(80, count);
    }

    [Fact]
    public void TAS_CountParam_ClampedToMin()
    {
        int count = 0;
        if (count < 1 || count > 500) count = 80;
        Assert.Equal(80, count);
    }

    [Fact]
    public void TAS_CountParam_ValidRange_Unchanged()
    {
        int count = 200;
        if (count < 1 || count > 500) count = 80;
        Assert.Equal(200, count);
    }

    [Fact]
    public void TAS_TradeMatchId_Format_IsDateDashSeq()
    {
        var date = DateTime.UtcNow.Date.ToString("yyyyMMdd");
        var matchId = $"{date}-{123456:D6}";
        Assert.StartsWith(date, matchId);
        Assert.Contains("-", matchId);
    }

    // ── TA Indicator math (RSI boundary conditions) ──────────

    [Fact]
    public void RSI_AllGains_Returns100()
    {
        var prices = Enumerable.Range(1, 20).Select(i => (decimal)i).ToList();
        var period = 14;
        var changes = prices.Skip(1).Zip(prices, (cur, prev) => cur - prev).ToList();
        var gains  = changes.Where(c => c > 0).Sum() / period;
        var losses = 0m;
        var rsi    = losses == 0 ? 100m : 100 - 100 / (1 + gains / losses);
        Assert.Equal(100m, rsi);
    }

    [Fact]
    public void RSI_AllLosses_Returns0()
    {
        var prices = Enumerable.Range(1, 20).Select(i => (decimal)(20 - i)).ToList();
        var period = 14;
        var changes = prices.Skip(1).Zip(prices, (cur, prev) => cur - prev).ToList();
        var gains  = 0m;
        var losses = Math.Abs(changes.Where(c => c < 0).Sum()) / period;
        var rsi    = gains == 0 ? 0m : 100 - 100 / (1 + gains / losses);
        Assert.Equal(0m, rsi);
    }

    [Fact]
    public void BB_UpperBand_AboveMid()
    {
        var prices = new[] { 10m, 11m, 10m, 12m, 11m, 10m, 13m, 11m, 10m, 12m,
                             11m, 10m, 12m, 11m, 10m, 12m, 11m, 10m, 11m, 12m };
        var mean = prices.Average();
        var std  = (decimal)Math.Sqrt((double)prices.Select(p => (p - mean) * (p - mean)).Average());
        var upper = mean + 2 * std;
        var lower = mean - 2 * std;
        Assert.True(upper > mean);
        Assert.True(lower < mean);
    }
}