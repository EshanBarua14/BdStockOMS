using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class EDRResult
{
    public decimal TotalEquity     { get; set; }
    public decimal TotalDebt       { get; set; }
    public decimal EDRRatio        { get; set; }
    public decimal MarginUsed      { get; set; }
    public decimal MarginLimit     { get; set; }
    public decimal MarginUtilPct   { get; set; }
    public string  MarginTier      { get; set; } = "Safe";
    public List<string> Warnings   { get; set; } = new();
    public bool IsBreached         { get; set; }
}

public interface IEDRService
{
    Task<EDRResult> CalculateAsync(int investorId, int brokerageHouseId);
    Task<EDRSnapshot> SaveSnapshotAsync(int investorId, int brokerageHouseId);
}

public class EDRService : IEDRService
{
    private readonly AppDbContext _db;

    public EDRService(AppDbContext db) => _db = db;

    public async Task<EDRResult> CalculateAsync(int investorId, int brokerageHouseId)
    {
        var investor = await _db.Users.FindAsync(investorId);
        if (investor == null) return new EDRResult();

        var portfolio = await _db.Portfolios
            .Include(p => p.Stock)
            .Where(p => p.InvestorId == investorId && p.Quantity > 0)
            .ToListAsync();

        var totalEquity = portfolio.Sum(p =>
            p.Quantity * (p.Stock?.LastTradePrice ?? p.AverageBuyPrice));

        var cashBalance  = investor.CashBalance;
        var marginUsed   = investor.MarginUsed;
        var marginLimit  = investor.MarginLimit;
        var totalDebt    = marginUsed;

        var edrRatio     = totalDebt > 0
            ? Math.Round(totalEquity / totalDebt, 4)
            : 999m;

        var marginUtilPct = marginLimit > 0
            ? Math.Round((marginUsed / marginLimit) * 100m, 2)
            : 0m;

        var result = new EDRResult
        {
            TotalEquity   = totalEquity,
            TotalDebt     = totalDebt,
            EDRRatio      = edrRatio,
            MarginUsed    = marginUsed,
            MarginLimit   = marginLimit,
            MarginUtilPct = marginUtilPct,
        };

        // 3-tier margin thresholds
        if (marginUtilPct >= 90m)
        {
            result.MarginTier = "Critical";
            result.IsBreached = true;
            result.Warnings.Add($"CRITICAL: Margin utilization {marginUtilPct:N1}% exceeds 90% threshold. Trading may be suspended.");
        }
        else if (marginUtilPct >= 75m)
        {
            result.MarginTier = "Warning";
            result.Warnings.Add($"WARNING: Margin utilization {marginUtilPct:N1}% exceeds 75% threshold. Reduce exposure.");
        }
        else if (marginUtilPct >= 50m)
        {
            result.MarginTier = "Watch";
            result.Warnings.Add($"WATCH: Margin utilization {marginUtilPct:N1}% exceeds 50% threshold.");
        }
        else
        {
            result.MarginTier = "Safe";
        }

        // EDR threshold warnings
        if (edrRatio < 1.5m && totalDebt > 0)
        {
            result.IsBreached = true;
            result.Warnings.Add($"EDR ratio {edrRatio:N2} is below minimum threshold of 1.5.");
        }
        else if (edrRatio < 2.0m && totalDebt > 0)
        {
            result.Warnings.Add($"EDR ratio {edrRatio:N2} is approaching minimum threshold.");
        }

        return result;
    }

    public async Task<EDRSnapshot> SaveSnapshotAsync(int investorId, int brokerageHouseId)
    {
        var edr = await CalculateAsync(investorId, brokerageHouseId);
        var snap = new EDRSnapshot
        {
            InvestorId       = investorId,
            BrokerageHouseId = brokerageHouseId,
            TotalEquity      = edr.TotalEquity,
            TotalDebt        = edr.TotalDebt,
            EDRRatio         = edr.EDRRatio,
            MarginUsed       = edr.MarginUsed,
            MarginLimit      = edr.MarginLimit,
            MarginUtilPct    = edr.MarginUtilPct,
            CalculatedAt     = DateTime.UtcNow,
        };
        _db.EDRSnapshots.Add(snap);
        await _db.SaveChangesAsync();
        return snap;
    }
}
