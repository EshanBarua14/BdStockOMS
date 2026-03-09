using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class RMSValidationResult
{
    public bool IsAllowed          { get; set; }
    public List<string> Violations { get; set; } = new();
    public List<string> Warnings   { get; set; } = new();
    public RMSAction? Action       { get; set; }
}

public interface IRMSValidationService
{
    Task<RMSValidationResult> ValidateOrderAsync(
        int investorId, int stockId, string exchange,
        decimal orderValue, string orderSide, int brokerageHouseId);
    Task<RMSValidationResult> CheckOrderValueLimitAsync(
        int investorId, decimal orderValue, int brokerageHouseId);
    Task<RMSValidationResult> CheckDailyExposureAsync(
        int investorId, decimal newOrderValue, int brokerageHouseId);
    Task<RMSValidationResult> CheckConcentrationAsync(
        int investorId, int stockId, decimal orderValue);
    Task<RMSValidationResult> CheckSectorConcentrationAsync(
        int investorId, int stockId, decimal orderValue);
    Task<RMSValidationResult> CheckCashBalanceAsync(
        int investorId, decimal orderValue, int brokerageHouseId);
}

public class RMSValidationService : IRMSValidationService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private readonly IHubContext<NotificationHub> _hub;

    private const decimal DefaultMaxOrderValue       = 5_000_000m;
    private const decimal DefaultMaxDailyValue       = 20_000_000m;
    private const decimal DefaultMaxExposure         = 50_000_000m;
    private const decimal DefaultConcentrationPct    = 10m;
    private const decimal DefaultSectorConcentration = 25m;

    public RMSValidationService(AppDbContext db, IAuditService audit,
        IHubContext<NotificationHub> hub)
    {
        _db    = db;
        _audit = audit;
        _hub   = hub;
    }

    public async Task<RMSValidationResult> ValidateOrderAsync(
        int investorId, int stockId, string exchange,
        decimal orderValue, string orderSide, int brokerageHouseId)
    {
        var result = new RMSValidationResult { IsAllowed = true };

        // 1. Cash balance (buy orders only)
        if (orderSide.ToUpper() == "BUY")
        {
            var cashCheck = await CheckCashBalanceAsync(investorId, orderValue, brokerageHouseId);
            MergeResult(result, cashCheck);
        }

        // 2. Order value limit
        var orderCheck = await CheckOrderValueLimitAsync(investorId, orderValue, brokerageHouseId);
        MergeResult(result, orderCheck);

        // 3. Daily exposure
        var dailyCheck = await CheckDailyExposureAsync(investorId, orderValue, brokerageHouseId);
        MergeResult(result, dailyCheck);

        // 4. Stock + sector concentration (buy orders only)
        if (orderSide.ToUpper() == "BUY")
        {
            var concentrationCheck = await CheckConcentrationAsync(investorId, stockId, orderValue);
            MergeResult(result, concentrationCheck);

            var sectorCheck = await CheckSectorConcentrationAsync(investorId, stockId, orderValue);
            MergeResult(result, sectorCheck);
        }

        if (result.Violations.Any())
        {
            result.IsAllowed = false;
            result.Action    = RMSAction.Block;

            await CreateTradeAlertAsync(investorId, brokerageHouseId,
                TradeAlertSeverity.Breach, result.Violations);

            await _audit.LogAsync(investorId, "RMS_ORDER_BLOCKED", "Order",
                null, null, string.Join("; ", result.Violations), null);
        }
        else if (result.Warnings.Any())
        {
            result.Action = RMSAction.Warn;

            await CreateTradeAlertAsync(investorId, brokerageHouseId,
                TradeAlertSeverity.Warning, result.Warnings);
        }

        return result;
    }

    public async Task<RMSValidationResult> CheckCashBalanceAsync(
        int investorId, decimal orderValue, int brokerageHouseId)
    {
        var result = new RMSValidationResult { IsAllowed = true };

        var investor    = await _db.Users.FindAsync(investorId);
        var cashBalance = investor?.CashBalance ?? 0m;

        if (cashBalance < orderValue)
            result.Violations.Add(
                $"Insufficient cash balance. Available: {cashBalance:N2} BDT, Required: {orderValue:N2} BDT.");
        else if (cashBalance < orderValue * 1.1m)
            result.Warnings.Add(
                $"Cash balance {cashBalance:N2} BDT is low relative to order value {orderValue:N2} BDT.");
        if (result.Violations.Any()) result.IsAllowed = false;

        return result;
    }

    public async Task<RMSValidationResult> CheckOrderValueLimitAsync(
        int investorId, decimal orderValue, int brokerageHouseId)
    {
        var result = new RMSValidationResult { IsAllowed = true };

        var limit         = await GetRMSLimitAsync(investorId, brokerageHouseId, RMSLevel.Investor);
        var maxOrderValue = limit?.MaxOrderValue ?? DefaultMaxOrderValue;

        if (orderValue > maxOrderValue)
            result.Violations.Add(
                $"Order value {orderValue:N2} exceeds limit of {maxOrderValue:N2} BDT.");
        else if (orderValue > maxOrderValue * 0.8m)
            result.Warnings.Add(
                $"Order value {orderValue:N2} is above 80% of your limit.");

        if (result.Violations.Any()) result.IsAllowed = false;
        return result;
    }

    public async Task<RMSValidationResult> CheckDailyExposureAsync(
        int investorId, decimal newOrderValue, int brokerageHouseId)
    {
        var result = new RMSValidationResult { IsAllowed = true };

        var limit         = await GetRMSLimitAsync(investorId, brokerageHouseId, RMSLevel.Investor);
        var maxDailyValue = limit?.MaxDailyValue ?? DefaultMaxDailyValue;

        var today      = DateTime.UtcNow.Date;
        var todayTotal = await _db.Orders
            .Where(o => o.InvestorId == investorId &&
                        o.CreatedAt.Date == today &&
                        o.Status != OrderStatus.Cancelled &&
                        o.Status != OrderStatus.Rejected)
            .SumAsync(o => (decimal?)o.Quantity * o.PriceAtOrder) ?? 0m;

        var projectedTotal = todayTotal + newOrderValue;

        if (projectedTotal > maxDailyValue)
            result.Violations.Add(
                $"Daily exposure {projectedTotal:N2} would exceed limit of {maxDailyValue:N2} BDT.");
        else if (projectedTotal > maxDailyValue * 0.8m)
            result.Warnings.Add(
                $"Daily exposure {projectedTotal:N2} is above 80% of your daily limit.");

        if (result.Violations.Any()) result.IsAllowed = false;
        return result;
    }

    public async Task<RMSValidationResult> CheckConcentrationAsync(
        int investorId, int stockId, decimal orderValue)
    {
        var result = new RMSValidationResult { IsAllowed = true };

        var portfolio = await _db.Portfolios
            .Where(p => p.InvestorId == investorId)
            .ToListAsync();

        if (!portfolio.Any()) return result;

        var totalPortfolioValue = portfolio.Sum(p => p.Quantity * p.AverageBuyPrice);
        if (totalPortfolioValue <= 0) return result;

        var currentHolding   = portfolio
            .Where(p => p.StockId == stockId)
            .Sum(p => p.Quantity * p.AverageBuyPrice);

        var projectedHolding = currentHolding + orderValue;
        var projectedPct     = (projectedHolding / (totalPortfolioValue + orderValue)) * 100m;
        var maxConcentration = DefaultConcentrationPct;

        var stock = await _db.Stocks.FindAsync(stockId);
        if (stock != null)
        {
            var sectorLimit = await _db.SectorConfigs
                .Where(s => s.SectorName == stock.Category.ToString() && s.IsActive)
                .FirstOrDefaultAsync();
            if (sectorLimit != null)
                maxConcentration = sectorLimit.MaxConcentrationPct;
        }

        if (projectedPct > maxConcentration)
            result.Violations.Add(
                $"Stock concentration {projectedPct:N2}% would exceed limit of {maxConcentration}%.");
        else if (projectedPct > maxConcentration * 0.8m)
            result.Warnings.Add(
                $"Stock concentration {projectedPct:N2}% is approaching limit of {maxConcentration}%.");

        if (result.Violations.Any()) result.IsAllowed = false;
        return result;
    }

    public async Task<RMSValidationResult> CheckSectorConcentrationAsync(
        int investorId, int stockId, decimal orderValue)
    {
        var result = new RMSValidationResult { IsAllowed = true };

        var stock = await _db.Stocks.FindAsync(stockId);
        if (stock == null) return result;

        var portfolio = await _db.Portfolios
            .Include(p => p.Stock)
            .Where(p => p.InvestorId == investorId)
            .ToListAsync();

        if (!portfolio.Any()) return result;

        var totalValue = portfolio.Sum(p => p.Quantity * p.AverageBuyPrice);
        if (totalValue <= 0) return result;

        var sectorValue = portfolio
            .Where(p => p.Stock?.Category == stock.Category)
            .Sum(p => p.Quantity * p.AverageBuyPrice);

        var projectedSectorValue = sectorValue + orderValue;
        var projectedSectorPct   = (projectedSectorValue / (totalValue + orderValue)) * 100m;

        var sectorConfig = await _db.SectorConfigs
            .Where(s => s.SectorName == stock.Category.ToString() && s.IsActive)
            .FirstOrDefaultAsync();

        var maxSectorPct = sectorConfig?.MaxConcentrationPct ?? DefaultSectorConcentration;

        if (projectedSectorPct > maxSectorPct)
            result.Violations.Add(
                $"Sector concentration {projectedSectorPct:N2}% would exceed limit of {maxSectorPct}%.");

        return result;
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task CreateTradeAlertAsync(int investorId, int brokerageHouseId,
        TradeAlertSeverity severity, List<string> messages)
    {
        var alert = new TradeAlert
        {
            InvestorId       = investorId,
            BrokerageHouseId = brokerageHouseId,
            Severity         = severity,
            AlertType        = TradeAlertType.OrderValueLimit, // generic fallback
            Message          = string.Join(" | ", messages),
            CreatedAt        = DateTime.UtcNow,
        };

        _db.TradeAlerts.Add(alert);
        await _db.SaveChangesAsync();

        // SignalR — push to investor's group
        await _hub.Clients.Group($"user-{investorId}")
            .SendAsync("RmsAlert", new
            {
                severity = severity.ToString(),
                messages,
                timestamp = DateTime.UtcNow,
            });
    }

    private async Task<RMSLimit?> GetRMSLimitAsync(
        int entityId, int brokerageHouseId, RMSLevel level)
    {
        return await _db.RMSLimits
            .Where(r => r.Level == level &&
                        r.EntityId == entityId &&
                        r.BrokerageHouseId == brokerageHouseId &&
                        r.IsActive)
            .FirstOrDefaultAsync();
    }

    private static void MergeResult(RMSValidationResult target, RMSValidationResult source)
    {
        target.Violations.AddRange(source.Violations);
        target.Warnings.AddRange(source.Warnings);
        if (source.Violations.Any()) target.IsAllowed = false;
    }
}
