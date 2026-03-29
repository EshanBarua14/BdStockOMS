using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class CascadeCheckResult
{
    public bool IsAllowed          { get; set; } = true;
    public List<string> Violations { get; set; } = new();
    public List<string> Warnings   { get; set; } = new();
    public string BreachedLevel    { get; set; } = string.Empty;
    public RMSLimitType? BreachedType { get; set; }
}

public interface IRMSCascadeService
{
    Task<CascadeCheckResult> CheckCascadeAsync(
        int investorId, int brokerageHouseId, int? basketId,
        decimal orderValue, string orderSide);
    Task<List<RMSLimitV2>> GetCascadeLimitsAsync(int investorId, int brokerageHouseId);
    Task SetLimitAsync(RMSLimitV2 limit);
}

public class RMSCascadeService : IRMSCascadeService
{
    private readonly AppDbContext _db;
    private readonly IEDRService _edr;

    private static readonly Dictionary<RMSLevelV2, decimal[]> DefaultLimits = new()
    {
        [RMSLevelV2.Client]  = new[] { 5_000_000m,  20_000_000m, 50_000_000m },
        [RMSLevelV2.User]    = new[] { 3_000_000m,  15_000_000m, 40_000_000m },
        [RMSLevelV2.BOGroup] = new[] { 50_000_000m, 200_000_000m,500_000_000m },
        [RMSLevelV2.Basket]  = new[] { 10_000_000m, 50_000_000m, 100_000_000m},
        [RMSLevelV2.Branch]  = new[] { 500_000_000m,2_000_000_000m,5_000_000_000m},
        [RMSLevelV2.Broker]  = new[] { 5_000_000_000m,20_000_000_000m,50_000_000_000m},
    };

    public RMSCascadeService(AppDbContext db, IEDRService edr)
    {
        _db  = db;
        _edr = edr;
    }

    public async Task<CascadeCheckResult> CheckCascadeAsync(
        int investorId, int brokerageHouseId, int? basketId,
        decimal orderValue, string orderSide)
    {
        var result = new CascadeCheckResult();
        var isBuy  = orderSide.ToUpperInvariant() == "BUY";

        // Level 1: Client (investor-level)
        await CheckLevel(result, RMSLevelV2.Client, investorId,
            brokerageHouseId, orderValue, isBuy, "Client");
        if (!result.IsAllowed) return result;

        // Level 2: User (same as client for now — extensible)
        await CheckLevel(result, RMSLevelV2.User, investorId,
            brokerageHouseId, orderValue, isBuy, "User");
        if (!result.IsAllowed) return result;

        // Level 3: BOGroup
        var boGroupId = await GetBOGroupIdAsync(investorId, brokerageHouseId);
        if (boGroupId.HasValue)
        {
            await CheckLevel(result, RMSLevelV2.BOGroup, boGroupId.Value,
                brokerageHouseId, orderValue, isBuy, "BOGroup");
            if (!result.IsAllowed) return result;
        }

        // Level 4: Basket
        if (basketId.HasValue)
        {
            await CheckLevel(result, RMSLevelV2.Basket, basketId.Value,
                brokerageHouseId, orderValue, isBuy, "Basket");
            if (!result.IsAllowed) return result;
        }

        // Level 5: Branch (use BrokerageHouseId as branch proxy)
        await CheckLevel(result, RMSLevelV2.Branch, brokerageHouseId,
            brokerageHouseId, orderValue, isBuy, "Branch");
        if (!result.IsAllowed) return result;

        // Level 6: Broker
        await CheckLevel(result, RMSLevelV2.Broker, brokerageHouseId,
            brokerageHouseId, orderValue, isBuy, "Broker");
        if (!result.IsAllowed) return result;

        // EDR check
        if (isBuy)
        {
            var edr = await _edr.CalculateAsync(investorId, brokerageHouseId);
            result.Warnings.AddRange(edr.Warnings);
            if (edr.IsBreached)
            {
                result.IsAllowed    = false;
                result.BreachedLevel = "EDR";
                result.BreachedType  = RMSLimitType.EDRThreshold;
                result.Violations.AddRange(edr.Warnings.Where(w => w.StartsWith("CRITICAL") || w.StartsWith("EDR")));
            }
        }

        return result;
    }

    private async Task CheckLevel(CascadeCheckResult result, RMSLevelV2 level,
        int entityId, int brokerageHouseId, decimal orderValue, bool isBuy, string levelName)
    {
        var limits = await _db.RMSLimitsV2
            .Where(l => l.Level == level && l.EntityId == entityId
                     && l.BrokerageHouseId == brokerageHouseId && l.IsActive)
            .OrderBy(l => l.Priority)
            .ToListAsync();

        var defaults = DefaultLimits.GetValueOrDefault(level, new[] { decimal.MaxValue, decimal.MaxValue, decimal.MaxValue });

        var maxOrder   = limits.FirstOrDefault(l => l.LimitType == RMSLimitType.MaxOrderValue)?.LimitValue ?? defaults[0];
        var maxDaily   = limits.FirstOrDefault(l => l.LimitType == RMSLimitType.DayBuyValue)?.LimitValue   ?? defaults[1];
        var maxExposure= limits.FirstOrDefault(l => l.LimitType == RMSLimitType.MaxExposure)?.LimitValue   ?? defaults[2];

        if (orderValue > maxOrder)
        {
            result.IsAllowed     = false;
            result.BreachedLevel = levelName;
            result.BreachedType  = RMSLimitType.MaxOrderValue;
            result.Violations.Add($"[{levelName}] Order value {orderValue:N0} exceeds limit {maxOrder:N0}.");
            return;
        }

        if (orderValue > maxOrder * 0.8m)
            result.Warnings.Add($"[{levelName}] Order value {orderValue:N0} is above 80% of {levelName} limit.");
    }

    private async Task<int?> GetBOGroupIdAsync(int investorId, int brokerageHouseId)
    {
        var member = await _db.BOGroupMembers
            .Include(m => m.BOGroup)
            .FirstOrDefaultAsync(m => m.UserId == investorId
                && m.BOGroup.BrokerageHouseId == brokerageHouseId
                && m.BOGroup.IsActive);
        return member?.BOGroupId;
    }

    public async Task<List<RMSLimitV2>> GetCascadeLimitsAsync(int investorId, int brokerageHouseId)
    {
        return await _db.RMSLimitsV2
            .Where(l => (l.EntityId == investorId || l.EntityId == brokerageHouseId)
                     && l.BrokerageHouseId == brokerageHouseId && l.IsActive)
            .OrderBy(l => l.Level)
            .ThenBy(l => l.LimitType)
            .ToListAsync();
    }

    public async Task SetLimitAsync(RMSLimitV2 limit)
    {
        var existing = await _db.RMSLimitsV2
            .FirstOrDefaultAsync(l => l.Level == limit.Level
                && l.LimitType == limit.LimitType
                && l.EntityId == limit.EntityId
                && l.BrokerageHouseId == limit.BrokerageHouseId);

        if (existing != null)
        {
            existing.LimitValue     = limit.LimitValue;
            existing.WarnAt         = limit.WarnAt;
            existing.ActionOnBreach = limit.ActionOnBreach;
            existing.UpdatedAt      = DateTime.UtcNow;
            existing.Notes          = limit.Notes;
        }
        else
        {
            limit.CreatedAt = DateTime.UtcNow;
            limit.UpdatedAt = DateTime.UtcNow;
            _db.RMSLimitsV2.Add(limit);
        }
        await _db.SaveChangesAsync();
    }
}
