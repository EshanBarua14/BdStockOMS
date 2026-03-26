// SimulatedOrderFillService.cs
// Auto-fills Pending orders after a short delay to simulate exchange matching.
// Market orders fill in 2-4s, Limit orders fill in 5-15s.

using BdStockOMS.API.Data;
using BdStockOMS.API.Hubs;
using BdStockOMS.API.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.BackgroundServices;

public class SimulatedOrderFillService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<StockPriceHub> _hub;
    private readonly ILogger<SimulatedOrderFillService> _logger;
    private static readonly Random _rng = new();

    public SimulatedOrderFillService(
        IServiceScopeFactory scopeFactory,
        IHubContext<StockPriceHub> hub,
        ILogger<SimulatedOrderFillService> logger)
    {
        _scopeFactory = scopeFactory;
        _hub          = hub;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("SimulatedOrderFillService started");
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(2000, ct);
            try { await ProcessPendingOrders(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "SimulatedOrderFill error"); }
        }
    }

    private async Task ProcessPendingOrders(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var pending = await db.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Open)
            .Take(20)
            .ToListAsync(ct);

        foreach (var order in pending)
        {
            if (ct.IsCancellationRequested) break;

            // Determine delay based on order category
            var delayMs = order.OrderCategory == OrderCategory.Market
                ? _rng.Next(1000, 3000)
                : _rng.Next(4000, 12000);

            $1

            // First transition: Pending → Open after 500ms
            if (order.Status == OrderStatus.Pending && ageMs > 500)
            {
                order.Status = OrderStatus.Open;
                await db.SaveChangesAsync(ct);
                continue; // process fill on next cycle
            }
            if (order.Status != OrderStatus.Open) continue;

            var stock     = order.Stock;
            var fillPrice = stock?.LastTradePrice ?? order.LimitPrice ?? order.PriceAtOrder;

            // Limit order: only fill if price is within 3% of market
            if (order.OrderCategory == OrderCategory.Limit && order.LimitPrice.HasValue && stock != null)
            {
                var ltp = stock.LastTradePrice;
                if (order.OrderType == OrderType.Buy  && order.LimitPrice.Value < ltp * 0.97m) continue;
                if (order.OrderType == OrderType.Sell && order.LimitPrice.Value > ltp * 1.03m) continue;
                fillPrice = order.LimitPrice.Value;
            }

            // Fill with tiny slippage ±0.1%
            order.ExecutionPrice = Math.Round(fillPrice * (decimal)(1 + (_rng.NextDouble() - 0.5) * 0.002), 2);
            // Transition: Pending → Open → Filled
            order.Status = OrderStatus.Filled;
            order.ExecutedAt     = DateTime.UtcNow;

            // Update portfolio for BUY
            if (order.OrderType == OrderType.Buy)
            {
                var portfolio = await db.Portfolios.FirstOrDefaultAsync(
                    p => p.InvestorId == order.InvestorId && p.StockId == order.StockId, ct);

                if (portfolio == null)
                {
                    db.Portfolios.Add(new Portfolio {
                        InvestorId        = order.InvestorId,
                        StockId           = order.StockId,
                        BrokerageHouseId  = order.BrokerageHouseId,
                        Quantity          = order.Quantity,
                        AverageBuyPrice   = order.ExecutionPrice!.Value,
                        LastUpdatedAt     = DateTime.UtcNow,
                    });
                }
                else
                {
                    var totalQty  = portfolio.Quantity + order.Quantity;
                    var totalCost = (portfolio.Quantity * portfolio.AverageBuyPrice)
                                  + (order.Quantity * order.ExecutionPrice!.Value);
                    portfolio.AverageBuyPrice = totalQty > 0 ? totalCost / totalQty : 0;
                    portfolio.Quantity        = totalQty;
                    portfolio.LastUpdatedAt   = DateTime.UtcNow;
                }
            }

            // Update portfolio for SELL
            if (order.OrderType == OrderType.Sell)
            {
                var portfolio = await db.Portfolios.FirstOrDefaultAsync(
                    p => p.InvestorId == order.InvestorId && p.StockId == order.StockId, ct);

                if (portfolio != null)
                {
                    portfolio.Quantity      = Math.Max(0, portfolio.Quantity - order.Quantity);
                    portfolio.LastUpdatedAt = DateTime.UtcNow;
                    if (portfolio.Quantity == 0) db.Portfolios.Remove(portfolio);
                }

                // Credit cash for sell proceeds
                if (order.Investor != null)
                    order.Investor.CashBalance += order.Quantity * order.ExecutionPrice!.Value;
            }

            await db.SaveChangesAsync(ct);

            // Broadcast TradeExecuted via SignalR
            await _hub.Clients.All.SendAsync("TradeExecuted", new {
                orderId        = order.Id,
                tradingCode    = stock?.TradingCode,
                status         = order.Status.ToString(),
                filledQuantity = order.Quantity,
                executionPrice = order.ExecutionPrice,
                side           = order.OrderType.ToString(),
            }, ct);

            _logger.LogInformation(
                "Simulated fill: Order {Id} {Side} {Qty}×{Code} @ {Price} → {Status}",
                order.Id, order.OrderType, order.Quantity,
                stock?.TradingCode, order.ExecutionPrice, order.Status);
        }
    }
}
