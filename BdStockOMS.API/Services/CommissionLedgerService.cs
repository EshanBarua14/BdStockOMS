using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
namespace BdStockOMS.API.Services;

public class CommissionLedgerService : ICommissionLedgerService
{
    private readonly AppDbContext _db;
    private readonly ICommissionCalculatorService _calculator;

    public CommissionLedgerService(AppDbContext db, ICommissionCalculatorService calculator)
    {
        _db         = db;
        _calculator = calculator;
    }

    public async Task<CommissionLedger> PostTradeCommissionAsync(Trade trade, string exchange)
    {
        var tradeValue = trade.Quantity * trade.Price;
        var side       = trade.Side.ToUpper();

        CommissionBreakdown breakdown;
        if (side == "BUY")
        {
            var result = await _calculator.CalculateBuyCommissionAsync(
                trade.InvestorId, trade.BrokerageHouseId, tradeValue, exchange);
            breakdown = result.IsSuccess ? result.Value! : await _calculator
                .CalculateFromRateAsync(tradeValue, 0.005m, exchange, side);
        }
        else
        {
            var result = await _calculator.CalculateSellCommissionAsync(
                trade.InvestorId, trade.BrokerageHouseId, tradeValue, exchange);
            breakdown = result.IsSuccess ? result.Value! : await _calculator
                .CalculateFromRateAsync(tradeValue, 0.005m, exchange, side);
        }

        var ledger = new CommissionLedger
        {
            TradeId          = trade.Id,
            OrderId          = trade.OrderId,
            InvestorId       = trade.InvestorId,
            BrokerageHouseId = trade.BrokerageHouseId,
            Exchange         = exchange.ToUpper(),
            Side             = side,
            TradeValue       = breakdown.TradeValue,
            BrokerCommission = breakdown.BrokerCommission,
            CDBLCharge       = breakdown.CDBLCharge,
            ExchangeFee      = breakdown.ExchangeFee,
            TotalCharges     = breakdown.TotalCharges,
            NetAmount        = breakdown.NetAmount,
            CommissionRate   = breakdown.CommissionRate,
            PostedAt         = DateTime.UtcNow,
        };

        _db.CommissionLedgers.Add(ledger);
        await _db.SaveChangesAsync();
        return ledger;
    }

    public async Task<List<CommissionLedger>> GetInvestorLedgerAsync(
        int investorId, DateTime? from, DateTime? to)
    {
        var query = _db.CommissionLedgers
            .Where(l => l.InvestorId == investorId);

        if (from.HasValue) query = query.Where(l => l.PostedAt >= from.Value);
        if (to.HasValue)   query = query.Where(l => l.PostedAt <= to.Value);

        return await query.OrderByDescending(l => l.PostedAt).ToListAsync();
    }

    public async Task<decimal> GetTotalCommissionAsync(
        int brokerageHouseId, DateTime? from, DateTime? to)
    {
        var query = _db.CommissionLedgers
            .Where(l => l.BrokerageHouseId == brokerageHouseId);

        if (from.HasValue) query = query.Where(l => l.PostedAt >= from.Value);
        if (to.HasValue)   query = query.Where(l => l.PostedAt <= to.Value);

        return await query.SumAsync(l => l.BrokerCommission);
    }
}
