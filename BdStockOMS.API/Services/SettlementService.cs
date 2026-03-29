using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
namespace BdStockOMS.API.Services;

public class SettlementService : ISettlementService
{
    private readonly AppDbContext _db;
    private readonly IOrderStateMachine _stateMachine;

    public SettlementService(AppDbContext db, IOrderStateMachine stateMachine)
    {
        _db           = db;
        _stateMachine = stateMachine;
    }

    public DateTime CalculateSettlementDate(DateTime tradeDate, SettlementType type)
    {
        if (type == SettlementType.T0)
            return tradeDate.Date;

        // T+2 — skip weekends (Bangladesh market: Sat/Sun off)
        var date  = tradeDate.Date;
        var days  = 0;
        while (days < 2)
        {
            date = date.AddDays(1);
            if (date.DayOfWeek != DayOfWeek.Saturday &&
                date.DayOfWeek != DayOfWeek.Sunday)
                days++;
        }
        return date;
    }

    public async Task<SettlementBatch> CreateBatchAsync(
        int brokerageHouseId, string exchange, DateTime tradeDate)
    {
        // Find all executed trades for this broker/exchange on tradeDate
        var tradeDateStart = tradeDate.Date;
        var tradeDateEnd   = tradeDateStart.AddDays(1);
        var trades = await _db.Trades
            .Where(t => t.BrokerageHouseId == brokerageHouseId &&
                        t.TradedAt >= tradeDateStart &&
                        t.TradedAt <  tradeDateEnd &&
                        t.Status == TradeStatus.Filled)
            .ToListAsync();
        // Load orders separately to avoid InMemory Include issues
        foreach (var t in trades)
            await _db.Entry(t).Reference(x => x.Order).LoadAsync();

        var settlementDate = CalculateSettlementDate(tradeDate, SettlementType.T2);

        var batch = new SettlementBatch
        {
            BrokerageHouseId = brokerageHouseId,
            Exchange         = exchange.ToUpper(),
            TradeDate        = tradeDate.Date,
            SettlementDate   = settlementDate,
            Status           = SettlementBatchStatus.Pending,
            TotalTrades      = trades.Count,
            TotalBuyValue    = trades.Where(t => t.Side == "BUY").Sum(t => t.TotalValue),
            TotalSellValue   = trades.Where(t => t.Side == "SELL").Sum(t => t.TotalValue),
            CreatedAt        = DateTime.UtcNow,
        };
        batch.NetObligations = batch.TotalBuyValue - batch.TotalSellValue;

        _db.SettlementBatches.Add(batch);
        await _db.SaveChangesAsync();

        // Create settlement items
        foreach (var trade in trades)
        {
            var item = new SettlementItem
            {
                SettlementBatchId = batch.Id,
                TradeId           = trade.Id,
                OrderId           = trade.OrderId,
                InvestorId        = trade.InvestorId,
                BrokerageHouseId  = trade.BrokerageHouseId,
                Side              = trade.Side,
                Quantity          = trade.Quantity,
                Price             = trade.Price,
                TradeValue        = trade.TotalValue,
                NetAmount         = trade.TotalValue,
                SettlementType    = trade.Order?.SettlementType ?? SettlementType.T2,
                TradeDate         = tradeDate.Date,
                SettlementDate    = settlementDate,
                Status            = SettlementItemStatus.Pending,
            };
            _db.SettlementItems.Add(item);
        }

        await _db.SaveChangesAsync();
        return batch;
    }

    public async Task<SettlementBatch> ProcessBatchAsync(int batchId)
    {
        var batch = await _db.SettlementBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId)
            ?? throw new InvalidOperationException($"Batch {batchId} not found");

        batch.Status = SettlementBatchStatus.Processing;
        await _db.SaveChangesAsync();

        var failCount = 0;
        foreach (var item in batch.Items)
        {
            try
            {
                item.Status     = SettlementItemStatus.Settled;
                item.SettledAt  = DateTime.UtcNow;

                // Mark the order as completed
                var order = await _db.Orders.FindAsync(item.OrderId);
                if (order != null)
                    await _stateMachine.TransitionAsync(order, OrderStatus.Completed,
                        "Settlement completed", "SettlementService");
            }
            catch (Exception ex)
            {
                item.Status        = SettlementItemStatus.Failed;
                item.FailureReason = ex.Message;
                failCount++;
            }
        }

        batch.Status      = failCount == 0
            ? SettlementBatchStatus.Completed
            : SettlementBatchStatus.Failed;
        batch.ProcessedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return batch;
    }

    public async Task<List<SettlementBatch>> GetPendingBatchesAsync()
        => await _db.SettlementBatches
            .Where(b => b.Status == SettlementBatchStatus.Pending)
            .OrderBy(b => b.SettlementDate)
            .ToListAsync();

    public async Task<List<SettlementItem>> GetBatchItemsAsync(int batchId)
        => await _db.SettlementItems
            .Where(i => i.SettlementBatchId == batchId)
            .ToListAsync();
    public async Task<SettlementBatch?> GetBatchByIdAsync(int batchId, int brokerageHouseId)
        => await _db.SettlementBatches
            .Include(b => b.Items)
            .FirstOrDefaultAsync(b => b.Id == batchId && b.BrokerageHouseId == brokerageHouseId);

    public async Task<List<SettlementItem>> GetInvestorSettlementsAsync(
        int investorId, int brokerageHouseId)
        => await _db.SettlementItems
            .Where(i => i.InvestorId == investorId && i.BrokerageHouseId == brokerageHouseId)
            .OrderByDescending(i => i.SettlementDate)
            .ToListAsync();

    public async Task<int> AutoCreateBatchesForTodayAsync(int brokerageHouseId)
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var existing  = await _db.SettlementBatches
            .AnyAsync(b => b.BrokerageHouseId == brokerageHouseId
                        && b.TradeDate.Date == yesterday);

        if (existing) return 0;

        var batch = await CreateBatchAsync(brokerageHouseId, "DSE", yesterday);
        return batch.TotalTrades;
    }

}