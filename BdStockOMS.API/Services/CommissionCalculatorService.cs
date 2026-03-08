using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class CommissionBreakdown
{
    public decimal TradeValue       { get; set; }
    public decimal BrokerCommission { get; set; }
    public decimal CDBLCharge      { get; set; }
    public decimal ExchangeFee      { get; set; }
    public decimal TotalCharges     { get; set; }
    public decimal NetAmount        { get; set; } // TradeValue + charges (buy) or TradeValue - charges (sell)
    public decimal CommissionRate   { get; set; }
    public string Exchange          { get; set; } = string.Empty;
    public string OrderType        { get; set; } = string.Empty;

    // Rate details
    public decimal CDBLRate        { get; set; } = 0.00015m; // 0.015%
    public decimal ExchangeFeeRate { get; set; } = 0.0005m;  // 0.05%
}

public interface ICommissionCalculatorService
{
    Task<Result<CommissionBreakdown>> CalculateBuyCommissionAsync(
        int investorId, int brokerageHouseId, decimal tradeValue, string exchange);
    Task<Result<CommissionBreakdown>> CalculateSellCommissionAsync(
        int investorId, int brokerageHouseId, decimal tradeValue, string exchange);
    Task<decimal> GetEffectiveBuyRateAsync(int investorId, int brokerageHouseId);
    Task<decimal> GetEffectiveSellRateAsync(int investorId, int brokerageHouseId);
    Task<CommissionBreakdown> CalculateFromRateAsync(
        decimal tradeValue, decimal brokerRate, string exchange, string orderType);
}

public class CommissionCalculatorService : ICommissionCalculatorService
{
    private readonly AppDbContext _db;

    // Fixed BD market charges
    private const decimal CDBLRate        = 0.00015m; // 0.015% of trade value
    private const decimal DSEFeeRate      = 0.0005m;  // 0.05% of trade value
    private const decimal CSEFeeRate      = 0.0005m;  // 0.05% of trade value
    private const decimal DefaultBuyRate  = 0.005m;   // 0.50% default
    private const decimal DefaultSellRate = 0.005m;   // 0.50% default

    public CommissionCalculatorService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<CommissionBreakdown>> CalculateBuyCommissionAsync(
        int investorId, int brokerageHouseId, decimal tradeValue, string exchange)
    {
        if (tradeValue <= 0)
            return Result<CommissionBreakdown>.Failure(
                "Trade value must be greater than zero.", "INVALID_TRADE_VALUE");

        var rate = await GetEffectiveBuyRateAsync(investorId, brokerageHouseId);
        var breakdown = await CalculateFromRateAsync(tradeValue, rate, exchange, "BUY");
        return Result<CommissionBreakdown>.Success(breakdown);
    }

    public async Task<Result<CommissionBreakdown>> CalculateSellCommissionAsync(
        int investorId, int brokerageHouseId, decimal tradeValue, string exchange)
    {
        if (tradeValue <= 0)
            return Result<CommissionBreakdown>.Failure(
                "Trade value must be greater than zero.", "INVALID_TRADE_VALUE");

        var rate = await GetEffectiveSellRateAsync(investorId, brokerageHouseId);
        var breakdown = await CalculateFromRateAsync(tradeValue, rate, exchange, "SELL");
        return Result<CommissionBreakdown>.Success(breakdown);
    }

    public async Task<decimal> GetEffectiveBuyRateAsync(int investorId, int brokerageHouseId)
    {
        // 1. Check investor-specific rate first
        var investorRate = await _db.InvestorCommissionRates
            .Where(r => r.InvestorId == investorId &&
                        r.BrokerageHouseId == brokerageHouseId &&
                        r.IsApproved &&
                        r.EffectiveFrom <= DateTime.UtcNow &&
                        (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (investorRate != null) return investorRate.BuyRate / 100m;

        // 2. Check brokerage-level rate
        var brokerageRate = await _db.BrokerageCommissionRates
            .Where(r => r.BrokerageHouseId == brokerageHouseId &&
                        r.IsActive &&
                        r.EffectiveFrom <= DateTime.UtcNow &&
                        (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (brokerageRate != null) return brokerageRate.BuyRate / 100m;

        // 3. Check system-wide rate
        var systemRate = await _db.CommissionRates
            .Where(r => r.IsActive &&
                        r.EffectiveFrom <= DateTime.UtcNow &&
                        (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (systemRate != null) return systemRate.BuyRate / 100m;

        return DefaultBuyRate;
    }

    public async Task<decimal> GetEffectiveSellRateAsync(int investorId, int brokerageHouseId)
    {
        var investorRate = await _db.InvestorCommissionRates
            .Where(r => r.InvestorId == investorId &&
                        r.BrokerageHouseId == brokerageHouseId &&
                        r.IsApproved &&
                        r.EffectiveFrom <= DateTime.UtcNow &&
                        (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (investorRate != null) return investorRate.SellRate / 100m;

        var brokerageRate = await _db.BrokerageCommissionRates
            .Where(r => r.BrokerageHouseId == brokerageHouseId &&
                        r.IsActive &&
                        r.EffectiveFrom <= DateTime.UtcNow &&
                        (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (brokerageRate != null) return brokerageRate.SellRate / 100m;

        var systemRate = await _db.CommissionRates
            .Where(r => r.IsActive &&
                        r.EffectiveFrom <= DateTime.UtcNow &&
                        (r.EffectiveTo == null || r.EffectiveTo >= DateTime.UtcNow))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (systemRate != null) return systemRate.SellRate / 100m;

        return DefaultSellRate;
    }

    public Task<CommissionBreakdown> CalculateFromRateAsync(
        decimal tradeValue, decimal brokerRate, string exchange, string orderType)
    {
        var exchangeFeeRate = exchange.ToUpper() == "CSE" ? CSEFeeRate : DSEFeeRate;

        var brokerCommission = Math.Round(tradeValue * brokerRate, 2);
        var cdblCharge       = Math.Round(tradeValue * CDBLRate, 2);
        var exchangeFee      = Math.Round(tradeValue * exchangeFeeRate, 2);
        var totalCharges     = brokerCommission + cdblCharge + exchangeFee;

        var netAmount = orderType.ToUpper() == "BUY"
            ? tradeValue + totalCharges
            : tradeValue - totalCharges;

        var breakdown = new CommissionBreakdown
        {
            TradeValue       = tradeValue,
            BrokerCommission = brokerCommission,
            CDBLCharge       = cdblCharge,
            ExchangeFee      = exchangeFee,
            TotalCharges     = totalCharges,
            NetAmount        = netAmount,
            CommissionRate   = brokerRate,
            Exchange         = exchange.ToUpper(),
            OrderType        = orderType.ToUpper(),
            CDBLRate         = CDBLRate,
            ExchangeFeeRate  = exchangeFeeRate
        };

        return Task.FromResult(breakdown);
    }
}
