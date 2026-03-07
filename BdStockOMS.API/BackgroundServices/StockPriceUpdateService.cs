// BackgroundServices/StockPriceUpdateService.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.BackgroundServices;

// BackgroundService = runs continuously in the background
// while the API is running — like a daemon process
// This simulates DSE/CSE sending us live price updates
public class StockPriceUpdateService : BackgroundService
{
    private readonly IHubContext<StockPriceHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StockPriceUpdateService> _logger;

    // IHubContext = lets background service send messages to SignalR hub
    // IServiceScopeFactory = lets us create a DB scope (DbContext is scoped, not singleton)
    public StockPriceUpdateService(
        IHubContext<StockPriceHub> hubContext,
        IServiceScopeFactory scopeFactory,
        ILogger<StockPriceUpdateService> logger)
    {
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Stock Price Update Service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await BroadcastPriceUpdates();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error broadcasting price updates.");
            }

            // Wait 5 seconds before next update
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task BroadcastPriceUpdates()
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var stocks = await db.Stocks
            .Where(s => s.IsActive)
            .ToListAsync();

        var random = new Random();

        foreach (var stock in stocks)
        {
            // Simulate small price movement ±0.5%
            // In production this would come from DSE/CSE data feed
            decimal changePercent = (decimal)(random.NextDouble() * 1.0 - 0.5);
            decimal priceChange = stock.LastTradePrice * (changePercent / 100);
            decimal newPrice = stock.LastTradePrice + priceChange;

            // Respect circuit breaker limits
            newPrice = Math.Max(stock.CircuitBreakerLow,
                       Math.Min(stock.CircuitBreakerHigh, newPrice));
            newPrice = Math.Round(newPrice, 2);

            // Update in DB
            stock.LastTradePrice = newPrice;
            stock.Change = newPrice - stock.ClosePrice;
            stock.ChangePercent = stock.ClosePrice == 0 ? 0 :
                Math.Round((stock.Change / stock.ClosePrice) * 100, 2);
            stock.LastUpdatedAt = DateTime.UtcNow;

            // Broadcast to all clients subscribed to this stock's group
            await _hubContext.Clients.Group(stock.TradingCode)
                .SendAsync("PriceUpdate", new
                {
                    stockId = stock.Id,
                    tradingCode = stock.TradingCode,
                    companyName = stock.CompanyName,
                    exchange = stock.Exchange,
                    lastTradePrice = stock.LastTradePrice,
                    change = stock.Change,
                    changePercent = stock.ChangePercent,
                    updatedAt = stock.LastUpdatedAt
                });
        }

        // Also broadcast full market summary to all clients
        await _hubContext.Clients.All.SendAsync("MarketUpdate", new
        {
            totalStocks = stocks.Count,
            updatedAt = DateTime.UtcNow,
            stocks = stocks.Select(s => new
            {
                stockId = s.Id,
                tradingCode = s.TradingCode,
                exchange = s.Exchange,
                lastTradePrice = s.LastTradePrice,
                change = s.Change,
                changePercent = s.ChangePercent
            })
        });

        await db.SaveChangesAsync();
        _logger.LogInformation("Price update broadcasted for {Count} stocks.", stocks.Count);
    }
}
