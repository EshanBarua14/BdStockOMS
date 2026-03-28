// SimulatedOrderFillService.cs
// Realistic fill simulation: Market orders fill fast, Limit orders fill on price
// condition only, slippage applied, Buy/Sell side correctly propagated via SignalR.

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
        _logger.LogInformation("SimulatedOrderFillService started (realistic mode)");
        {
            await Task.Delay(3000, ct);
            try   { await ProcessPendingOrders(ct); }
            catch (Exception ex) { _logger.LogWarning(ex, "SimulatedOrderFill error"); }
        }
    }

    private async Task ProcessPendingOrders(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var orders = await db.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .Where(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Open)
            .Take(30)
            .ToListAsync(ct);

        foreach (var order in orders)
        {
            if (ct.IsCancellationRequested) break;

            var ageMs = (DateTime.UtcNow - order.CreatedAt).TotalMilliseconds;

            // Step 1: Pending -> Open after 500 ms
            if (order.Status == OrderStatus.Pending && ageMs > 500)
            {
                order.Status = OrderStatus.Open;
                await db.SaveChangesAsync(ct);
                continue;
            }
            if (order.Status != OrderStatus.Open) continue;

            var stock       = order.Stock;
            var marketPrice = stock?.LastTradePrice ?? order.LimitPrice ?? order.PriceAtOrder;

            // Step 2: Decide whether to fill this tick
            bool shouldFill = order.OrderCategory switch
            {
                // Market: high probability, fills quickly
                OrderCategory.Market => ageMs > 1500 && _rng.NextDouble() < 0.90,

                // Limit: price must cross, then random liquidity check
                OrderCategory.Limit  => IsLimitFillable(order, marketPrice)
                                        && ageMs > 3000
                                        && _rng.NextDouble() < 0.55,
                _ => false
            };


            // Step 3: Fill price with slippage for market orders
            decimal fillPrice = order.OrderCategory == OrderCategory.Market
                ? ApplySlippage(marketPrice, order.OrderType)
                : order.LimitPrice!.Value;

            order.ExecutionPrice = Math.Round(fillPrice, 2);
            order.Status         = OrderStatus.Filled;
            order.ExecutedAt     = DateTime.UtcNow;

            // Step 4: Update portfolio
            if (order.OrderType == OrderType.Buy)
            {
                var portfolio = await db.Portfolios.FirstOrDefaultAsync(
                    p => p.InvestorId == order.InvestorId && p.StockId == order.StockId, ct);

                if (portfolio == null)
                {
                    db.Portfolios.Add(new Portfolio
                    {
                        InvestorId       = order.InvestorId,
                        StockId          = order.StockId,
                        BrokerageHouseId = order.BrokerageHouseId,
                        Quantity         = order.Quantity,
                        AverageBuyPrice  = order.ExecutionPrice!.Value,
                        LastUpdatedAt    = DateTime.UtcNow,
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

                if (order.Investor != null)
                    order.Investor.CashBalance += order.Quantity * order.ExecutionPrice!.Value;
            }

            await db.SaveChangesAsync(ct);

            // Step 5: Broadcast — side = order.OrderType.ToString() = "Buy" or "Sell"
            await _hub.Clients.All.SendAsync("TradeExecuted", new
            {
                orderId        = order.Id,
                tradingCode    = stock?.TradingCode,
                status         = order.Status.ToString(),
                filledQuantity = order.Quantity,
                executionPrice = order.ExecutionPrice,
                side           = order.OrderType.ToString(),  // FIX: always Buy or Sell
            }, ct);

            _logger.LogInformation(
                "Filled: Order {Id} {Side} {Qty}x{Code} @ {Price:F2} (market {Market:F2})",
                order.Id, order.OrderType, order.Quantity,
                stock?.TradingCode, order.ExecutionPrice, marketPrice);
        }
    }

    // BUY  limit fills when market <= limit (getting it at or cheaper than limit)
    // SELL limit fills when market >= limit (getting it at or higher than limit)
    private static bool IsLimitFillable(Order order, decimal marketPrice)
    {
        return order.OrderType switch
        {
            OrderType.Buy  => marketPrice <= order.LimitPrice.Value,
            OrderType.Sell => marketPrice >= order.LimitPrice.Value,
            _              => false
        };
    }

    // Buyers pay slightly above market, sellers receive slightly below
    private static decimal ApplySlippage(decimal price, OrderType side)
    {
        var slip = (decimal)(_rng.NextDouble() * 0.002); // 0-0.2%
        return side == OrderType.Buy ? price * (1 + slip) : price * (1 - slip);
    }
}
