using BdStockOMS.API.Data;
using BdStockOMS.API.Hubs;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.BackgroundServices;

/// <summary>
/// Real market data service — scrapes DSE every 30s during market hours,
/// falls back to simulation when market is closed or scrape fails.
/// </summary>
public class RealMarketDataService : BackgroundService
{
    private readonly IHubContext<StockPriceHub> _hub;
    private readonly IServiceScopeFactory       _scope;
    private readonly ILogger<RealMarketDataService> _logger;
    private int _tick = 0;
    private DateTime _lastSuccessfulScrape = DateTime.MinValue;
    private int _consecutiveFailures = 0;

    public RealMarketDataService(
        IHubContext<StockPriceHub> hub,
        IServiceScopeFactory scope,
        ILogger<RealMarketDataService> logger)
    {
        _hub    = hub;
        _scope  = scope;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("RealMarketDataService started.");

        while (!ct.IsCancellationRequested)
        {
            try
            {
                _tick++;
                await FetchAndBroadcast(ct);
                if (_tick % 30 == 0) await BroadcastIndexData(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Market data tick error");
            }

            // 30s during market hours, 5min outside
            var interval = IsMarketHours() ? 30 : 300;
            await Task.Delay(TimeSpan.FromSeconds(interval), ct);
        }
    }

    private static bool IsMarketHours()
    {
        try
        {
            var tz  = TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Bangladesh Standard Time" : "Asia/Dhaka");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            if (now.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday) return false;
            return now.TimeOfDay >= new TimeSpan(10, 0, 0) &&
                   now.TimeOfDay <= new TimeSpan(14, 30, 0);
        }
        catch { return false; }
    }

    private async Task FetchAndBroadcast(CancellationToken ct)
    {
        using var scope   = _scope.CreateScope();
        var scraper       = scope.ServiceProvider.GetRequiredService<IDseScraperService>();
        var db            = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        List<DseStockTick> ticks = new();
        bool usingRealData = false;

        // Try real data first
        if (IsMarketHours() || _tick == 1)
        {
            ticks = await scraper.GetAllPricesAsync(ct);
            if (ticks.Count > 0)
            {
                usingRealData = true;
                _lastSuccessfulScrape = DateTime.UtcNow;
                _consecutiveFailures  = 0;
            }
            else
            {
                _consecutiveFailures++;
                _logger.LogWarning("DSE scrape returned 0 results (failure #{F})", _consecutiveFailures);
            }
        }

        var stocks = await db.Stocks.Where(s => s.IsActive).ToListAsync(ct);
        var updates = new List<object>();

        foreach (var stock in stocks)
        {
            decimal newPrice, change, changePct;

            if (usingRealData)
            {
                // Match by trading code
                var tick = ticks.FirstOrDefault(t =>
                    t.TradingCode.Equals(stock.TradingCode, StringComparison.OrdinalIgnoreCase));

                if (tick == null) continue; // skip stocks not in DSE data

                newPrice  = tick.LastTradePrice;
                change    = tick.Change;
                changePct = tick.ChangePercent;
            }
            else
            {
                // Keep existing price — no simulation overwrite
                newPrice  = stock.LastTradePrice;
                change    = stock.Change;
                changePct = stock.ChangePercent;
            }

            // Update DB
            stock.LastTradePrice = newPrice;
            stock.Change         = change;
            stock.ChangePercent  = changePct;
            stock.LastUpdatedAt  = DateTime.UtcNow;

            var update = new
            {
                stockId        = stock.Id,
                tradingCode    = stock.TradingCode,
                companyName    = stock.CompanyName,
                exchange       = stock.Exchange,
                category       = stock.Category.ToString(),
                lastTradePrice = newPrice,
                change,
                changePercent  = changePct,
                highPrice      = stock.HighPrice,
                lowPrice       = stock.LowPrice,
                closePrice     = stock.ClosePrice,
                volume         = stock.Volume,
                valueInMillionTaka = stock.ValueInMillionTaka,
                isRealData     = usingRealData,
                updatedAt      = DateTime.UtcNow,
            };

            updates.Add(update);
            await _hub.Clients.Group(stock.TradingCode)
                .SendAsync("PriceUpdate", update, ct);
        }

        if (updates.Count > 0)
        {
            await _hub.Clients.All.SendAsync("BulkPriceUpdate", updates, ct);
            await db.SaveChangesAsync(ct);
            _logger.LogInformation("Broadcast {Count} stocks ({Mode})",
                updates.Count, usingRealData ? "REAL" : "CACHED");
        }
    }

    private async Task BroadcastIndexData(CancellationToken ct)
    {
        using var scope = _scope.CreateScope();
        var scraper     = scope.ServiceProvider.GetRequiredService<IDseScraperService>();

        var idx = await scraper.GetIndexDataAsync(ct);
        if (idx == null) return;

        await _hub.Clients.All.SendAsync("IndexUpdate", new
        {
            dsex      = idx.DSEX,
            dses      = idx.DSES,
            ds30      = idx.DS30,
            cseAll    = 0m,
            cse30     = 0m,
            isRealData = true,
            updatedAt  = DateTime.UtcNow,
        }, ct);
    }
}
