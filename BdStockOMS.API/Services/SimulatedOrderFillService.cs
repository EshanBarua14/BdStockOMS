using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BdStockOMS.API.Services
{
    public class SimulatedOrderFillService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SimulatedOrderFillService> _logger;

        public SimulatedOrderFillService(IServiceScopeFactory scopeFactory, ILogger<SimulatedOrderFillService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await ProcessPendingOrders(); }
                catch (Exception ex) { _logger.LogError(ex, "SimulatedFill error"); }
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        private async Task ProcessPendingOrders()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var fillable = new[]
            {
                OrderStatus.Queued, OrderStatus.Submitted,
                OrderStatus.Open,   OrderStatus.Waiting
            };

            var orders = await db.Orders
                .Include(o => o.Stock)
                .Where(o => fillable.Contains(o.Status))
                .ToListAsync();

            var rng = new Random();

            foreach (var order in orders)
            {
                if (order.Stock == null) continue;
                if (order.IsPrivate) continue;
                if (order.ExecInstruction == ExecInstruction.Suspend) continue;

                if (order.Status == OrderStatus.Queued)
                {
                    order.Status    = OrderStatus.Submitted;
                    order.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                if (order.Status == OrderStatus.Submitted)
                {
                    order.Status    = OrderStatus.Open;
                    order.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                var roll = rng.NextDouble();
                if (roll > order.FillProbability) continue;

                var slippage  = order.OrderType == OrderType.Buy ? 1 + order.SlippagePercent : 1 - order.SlippagePercent;
                var execPrice = order.Stock.LastTradePrice * (decimal)slippage;

                if (order.TimeInForce == TimeInForce.IOC || order.TimeInForce == TimeInForce.FOK)
                {
                    if (roll <= order.FillProbability) FillOrder(order, execPrice);
                    else { order.Status = OrderStatus.Cancelled; order.CancelledAt = DateTime.UtcNow; }
                    order.UpdatedAt = DateTime.UtcNow;
                    continue;
                }

                if (order.MinQty.HasValue)
                {
                    var partialQty = rng.Next(1, order.Quantity + 1);
                    if (partialQty < order.MinQty.Value) continue;
                }

                FillOrder(order, execPrice);
                order.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
        }

        private static void FillOrder(Order order, decimal execPrice)
        {
            order.ExecutedQuantity  = order.Quantity;
            order.ExecutionPrice    = execPrice;
            order.GrossTradeAmt     = execPrice * order.Quantity;
            order.TrdMatchID        = Guid.NewGuid().ToString("N")[..16].ToUpper();
            order.SettlDate         = DateTime.UtcNow.AddDays(2).ToString("yyyyMMdd");
            order.AggressorIndicator = order.OrderType == OrderType.Buy ? AggressorSide.Buy : AggressorSide.Sell;
            order.Status            = OrderStatus.Filled;
        }
    }
}
