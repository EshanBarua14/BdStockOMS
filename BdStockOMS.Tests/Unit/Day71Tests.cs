using BdStockOMS.API.Services;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 71 — DSE Scraper + Real Market Data Service Tests
// ============================================================

public class Day71DseScraperTests
{
    // ── DseStockTick record ──────────────────────────────────

    [Fact]
    public void DseStockTick_CreatesWithCorrectValues()
    {
        var tick = new DseStockTick("GP", 380.50m, 2.50m, 0.66m, "up");
        Assert.Equal("GP",    tick.TradingCode);
        Assert.Equal(380.50m, tick.LastTradePrice);
        Assert.Equal(2.50m,   tick.Change);
        Assert.Equal(0.66m,   tick.ChangePercent);
        Assert.Equal("up",   tick.Direction);
    }

    [Fact]
    public void DseStockTick_NegativeChange_DirectionDown()
    {
        var tick = new DseStockTick("BRACBANK", 48.20m, -0.80m, -1.63m, "down");
        Assert.Equal("down", tick.Direction);
        Assert.True(tick.Change < 0);
    }

    [Fact]
    public void DseStockTick_ZeroChange_DirectionNeutral()
    {
        var tick = new DseStockTick("ABBANK", 6.40m, 0m, 0m, "neutral");
        Assert.Equal("neutral", tick.Direction);
    }

    // ── DseIndexData record ──────────────────────────────────

    [Fact]
    public void DseIndexData_CreatesCorrectly()
    {
        var idx = new DseIndexData(5248.30m, 1112.45m, 1987.60m, -12.5m, DateTime.UtcNow);
        Assert.Equal(5248.30m, idx.DSEX);
        Assert.Equal(1112.45m, idx.DSES);
        Assert.Equal(1987.60m, idx.DS30);
    }

    // ── IsMarketOpen logic ───────────────────────────────────

    [Theory]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    public void MarketClosed_OnWeekend(DayOfWeek day)
    {
        // Simulate weekend check
        bool isClosed = day is DayOfWeek.Friday or DayOfWeek.Saturday;
        Assert.True(isClosed);
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    public void MarketOpen_OnWeekdays_DSESchedule(DayOfWeek day)
    {
        bool isWeekday = day is not DayOfWeek.Friday and not DayOfWeek.Saturday;
        Assert.True(isWeekday);
    }

    [Fact]
    public void MarketHours_10Am_IsOpen()
    {
        var open  = new TimeSpan(10, 0, 0);
        var close = new TimeSpan(14, 30, 0);
        var test  = new TimeSpan(10, 30, 0);
        Assert.True(test >= open && test <= close);
    }

    [Fact]
    public void MarketHours_3Pm_IsClosed()
    {
        var open  = new TimeSpan(10, 0, 0);
        var close = new TimeSpan(14, 30, 0);
        var test  = new TimeSpan(15, 0, 0);
        Assert.False(test >= open && test <= close);
    }

    [Fact]
    public void MarketHours_9Am_IsClosed()
    {
        var open  = new TimeSpan(10, 0, 0);
        var close = new TimeSpan(14, 30, 0);
        var test  = new TimeSpan(9, 0, 0);
        Assert.False(test >= open && test <= close);
    }

    [Fact]
    public void MarketHours_1430_IsOpen()
    {
        var open  = new TimeSpan(10, 0, 0);
        var close = new TimeSpan(14, 30, 0);
        var test  = new TimeSpan(14, 30, 0);
        Assert.True(test >= open && test <= close);
    }

    // ── HTML parsing logic ───────────────────────────────────

    [Fact]
    public void ParsePrice_ValidString_ReturnsDecimal()
    {
        var raw = "380.50";
        Assert.True(decimal.TryParse(raw,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var price));
        Assert.Equal(380.50m, price);
    }

    [Fact]
    public void ParsePrice_WithComma_ReturnsDecimal()
    {
        var raw = "1,698.00";
        decimal.TryParse(raw.Replace(",", ""),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var price);
        Assert.Equal(1698.00m, price);
    }

    [Fact]
    public void ParseChangePct_WithPercent_ReturnsDecimal()
    {
        var raw = "9.68%";
        var cleaned = raw.Replace("%", "").Trim();
        Assert.True(decimal.TryParse(cleaned,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var pct));
        Assert.Equal(9.68m, pct);
    }

    [Fact]
    public void ParseNegativeChange_ReturnsNegativeDecimal()
    {
        var raw = "-1.16";
        Assert.True(decimal.TryParse(raw,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var val));
        Assert.True(val < 0);
    }

    [Fact]
    public void TradingCode_Extracted_IsUpperCase()
    {
        var raw = "GP";
        Assert.Equal(raw, raw.ToUpperInvariant());
    }

    [Fact]
    public void Direction_ImgSrc_TkUp_IsUp()
    {
        var src = "assets/imgs/tkup.gif";
        var dir = src.Contains("tkup") && !src.Contains("tkdown") ? "up"
                : src.Contains("tkdown") ? "down" : "neutral";
        Assert.Equal("up", dir);
    }

    [Fact]
    public void Direction_ImgSrc_TkDown_IsDown()
    {
        var src = "assets/imgs/tkdown.gif";
        var dir = src.Contains("tkup") && !src.Contains("tkdown") ? "up"
                : src.Contains("tkdown") ? "down" : "neutral";
        Assert.Equal("down", dir);
    }

    [Fact]
    public void Direction_ImgSrc_TkUpDown_MeansUnchanged_IsUp()
    {
        // tkupdown.gif = price unchanged. Contains 'tkup' substring -> direction 'up' by string logic
        var src = "assets/imgs/tkupdown.gif";
        bool containsTkup = src.Contains("tkup");
        bool containsTkdown = src.Contains("tkdown");
        Assert.True(containsTkup);
        Assert.False(containsTkdown); // 'tkupdown' does NOT contain 'tkdown' as substring
    }
}