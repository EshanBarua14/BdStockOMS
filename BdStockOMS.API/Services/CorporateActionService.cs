using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.CorporateAction;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class CorporateActionService : ICorporateActionService
{
    private readonly AppDbContext _context;

    public CorporateActionService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<CorporateActionResponseDto>>> GetAllAsync(int? stockId, bool? isProcessed)
    {
        var q = _context.CorporateActions.Include(c => c.Stock).AsQueryable();
        if (stockId.HasValue)     q = q.Where(c => c.StockId == stockId.Value);
        if (isProcessed.HasValue) q = q.Where(c => c.IsProcessed == isProcessed.Value);
        var items = await q.OrderByDescending(c => c.RecordDate).Select(c => ToDto(c)).ToListAsync();
        return Result<List<CorporateActionResponseDto>>.Success(items);
    }

    public async Task<Result<CorporateActionResponseDto>> GetByIdAsync(int id)
    {
        var entity = await _context.CorporateActions.Include(c => c.Stock).FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null) return Result<CorporateActionResponseDto>.Failure("Corporate action not found.");
        return Result<CorporateActionResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<List<CorporateActionResponseDto>>> GetByStockAsync(int stockId)
    {
        var items = await _context.CorporateActions
            .Include(c => c.Stock)
            .Where(c => c.StockId == stockId)
            .OrderByDescending(c => c.RecordDate)
            .Select(c => ToDto(c))
            .ToListAsync();
        return Result<List<CorporateActionResponseDto>>.Success(items);
    }

    public async Task<Result<CorporateActionResponseDto>> CreateAsync(CreateCorporateActionDto dto)
    {
        var stock = await _context.Stocks.FindAsync(dto.StockId);
        if (stock == null) return Result<CorporateActionResponseDto>.Failure("Stock not found.");

        if (!Enum.TryParse<CorporateActionType>(dto.Type, true, out var actionType))
            return Result<CorporateActionResponseDto>.Failure("Invalid corporate action type: " + dto.Type);

        if (dto.Value <= 0)
            return Result<CorporateActionResponseDto>.Failure("Value must be greater than zero.");

        var entity = new CorporateAction
        {
            StockId = dto.StockId, Type = actionType, Value = dto.Value,
            RecordDate = dto.RecordDate, PaymentDate = dto.PaymentDate,
            Description = dto.Description, IsProcessed = false, CreatedAt = DateTime.UtcNow
        };

        _context.CorporateActions.Add(entity);
        await _context.SaveChangesAsync();
        entity.Stock = stock;
        return Result<CorporateActionResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<CorporateActionResponseDto>> UpdateAsync(int id, UpdateCorporateActionDto dto)
    {
        var entity = await _context.CorporateActions.Include(c => c.Stock).FirstOrDefaultAsync(c => c.Id == id);
        if (entity == null)    return Result<CorporateActionResponseDto>.Failure("Corporate action not found.");
        if (entity.IsProcessed) return Result<CorporateActionResponseDto>.Failure("Cannot update an already processed corporate action.");
        if (dto.Value <= 0)    return Result<CorporateActionResponseDto>.Failure("Value must be greater than zero.");

        entity.Value = dto.Value; entity.RecordDate = dto.RecordDate;
        entity.PaymentDate = dto.PaymentDate; entity.Description = dto.Description;
        entity.IsProcessed = dto.IsProcessed;
        await _context.SaveChangesAsync();
        return Result<CorporateActionResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<bool>> MarkProcessedAsync(int id)
    {
        var entity = await _context.CorporateActions.FindAsync(id);
        if (entity == null)    return Result<bool>.Failure("Corporate action not found.");
        if (entity.IsProcessed) return Result<bool>.Failure("Corporate action is already processed.");
        entity.IsProcessed = true;
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var entity = await _context.CorporateActions.FindAsync(id);
        if (entity == null)    return Result<bool>.Failure("Corporate action not found.");
        if (entity.IsProcessed) return Result<bool>.Failure("Cannot delete a processed corporate action.");
        _context.CorporateActions.Remove(entity);
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    // ── Portfolio adjustment engine ──────────────────────────────────────

    public async Task<Result<ProcessCorporateActionResultDto>> ProcessAsync(int id)
    {
        var action = await _context.CorporateActions.Include(c => c.Stock).FirstOrDefaultAsync(c => c.Id == id);
        if (action == null)    return Result<ProcessCorporateActionResultDto>.Failure("Corporate action not found.");
        if (action.IsProcessed) return Result<ProcessCorporateActionResultDto>.Failure("Corporate action already processed.");

        var holdings = await _context.Portfolios
            .Where(p => p.StockId == action.StockId)
            .ToListAsync();

        if (!holdings.Any())
            return Result<ProcessCorporateActionResultDto>.Failure("No holdings found for this stock.");

        var entries   = new List<CorporateActionLedger>();
        var resultDtos = new List<CorporateActionLedgerEntryDto>();
        decimal totalCash   = 0;
        int     totalShares = 0;

        foreach (var holding in holdings)
        {
            var entry = action.Type switch
            {
                CorporateActionType.Dividend    => ApplyDividend(action, holding),
                CorporateActionType.BonusShare  => ApplyBonus(action, holding),
                CorporateActionType.RightShare  => ApplyRights(action, holding),
                _ => null
            };

            if (entry == null) continue;

            // Apply portfolio mutation
            if (entry.SharesAwarded > 0)
            {
                holding.Quantity += entry.SharesAwarded;
                holding.LastUpdatedAt = DateTime.UtcNow;
            }

            entries.Add(entry);
            totalCash   += entry.CashAmount;
            totalShares += entry.SharesAwarded;

            resultDtos.Add(new CorporateActionLedgerEntryDto
            {
                InvestorId   = holding.InvestorId,
                HoldingQty   = entry.HoldingQtyAtRecord,
                CashAmount   = entry.CashAmount,
                SharesAwarded = entry.SharesAwarded,
                EntryType    = entry.EntryType.ToString(),
                Notes        = entry.Notes
            });
        }

        action.IsProcessed = true;
        _context.CorporateActionLedgers.AddRange(entries);
        await _context.SaveChangesAsync();

        return Result<ProcessCorporateActionResultDto>.Success(new ProcessCorporateActionResultDto
        {
            CorporateActionId    = action.Id,
            ActionType           = action.Type.ToString(),
            AffectedHoldings     = entries.Count,
            TotalCashDistributed = totalCash,
            TotalSharesAwarded   = totalShares,
            Entries              = resultDtos
        });
    }

    public async Task<Result<List<CorporateActionLedgerEntryDto>>> GetLedgerAsync(int corporateActionId)
    {
        var entries = await _context.CorporateActionLedgers
            .Where(l => l.CorporateActionId == corporateActionId)
            .OrderBy(l => l.InvestorId)
            .Select(l => new CorporateActionLedgerEntryDto
            {
                InvestorId    = l.InvestorId,
                HoldingQty    = l.HoldingQtyAtRecord,
                CashAmount    = l.CashAmount,
                SharesAwarded = l.SharesAwarded,
                EntryType     = l.EntryType.ToString(),
                Notes         = l.Notes
            })
            .ToListAsync();

        return Result<List<CorporateActionLedgerEntryDto>>.Success(entries);
    }

    // ── Adjustment calculators ───────────────────────────────────────────

    private static CorporateActionLedger ApplyDividend(CorporateAction action, Portfolio holding)
    {
        var cash = holding.Quantity * action.Value;
        return new CorporateActionLedger
        {
            CorporateActionId  = action.Id,
            InvestorId         = holding.InvestorId,
            StockId            = holding.StockId,
            BrokerageHouseId   = holding.BrokerageHouseId,
            EntryType          = CorporateActionLedgerType.DividendCash,
            HoldingQtyAtRecord = holding.Quantity,
            ActionValue        = action.Value,
            CashAmount         = cash,
            SharesAwarded      = 0,
            Notes              = "Dividend BDT " + action.Value + " x " + holding.Quantity + " shares = BDT " + cash
        };
    }

    private static CorporateActionLedger ApplyBonus(CorporateAction action, Portfolio holding)
    {
        // Value = bonus ratio e.g. 0.10 = 10% bonus
        var shares = (int)Math.Floor(holding.Quantity * action.Value);
        return new CorporateActionLedger
        {
            CorporateActionId  = action.Id,
            InvestorId         = holding.InvestorId,
            StockId            = holding.StockId,
            BrokerageHouseId   = holding.BrokerageHouseId,
            EntryType          = CorporateActionLedgerType.BonusShareCredit,
            HoldingQtyAtRecord = holding.Quantity,
            ActionValue        = action.Value,
            CashAmount         = 0,
            SharesAwarded      = shares,
            Notes              = "Bonus " + (action.Value * 100) + "% on " + holding.Quantity + " shares = " + shares + " bonus shares"
        };
    }

    private static CorporateActionLedger ApplyRights(CorporateAction action, Portfolio holding)
    {
        // Value = rights ratio e.g. 0.25 = 1 right per 4 shares held
        var entitlement = (int)Math.Floor(holding.Quantity * action.Value);
        return new CorporateActionLedger
        {
            CorporateActionId  = action.Id,
            InvestorId         = holding.InvestorId,
            StockId            = holding.StockId,
            BrokerageHouseId   = holding.BrokerageHouseId,
            EntryType          = CorporateActionLedgerType.RightsEntitlement,
            HoldingQtyAtRecord = holding.Quantity,
            ActionValue        = action.Value,
            CashAmount         = 0,
            SharesAwarded      = entitlement,
            Notes              = "Rights entitlement " + (action.Value * 100) + "% on " + holding.Quantity + " shares = " + entitlement + " rights"
        };
    }

    private static CorporateActionResponseDto ToDto(CorporateAction c) => new()
    {
        Id = c.Id, StockId = c.StockId,
        TradingCode = c.Stock?.TradingCode ?? string.Empty,
        CompanyName = c.Stock?.CompanyName ?? string.Empty,
        Type = c.Type.ToString(), Value = c.Value,
        RecordDate = c.RecordDate, PaymentDate = c.PaymentDate,
        Description = c.Description, IsProcessed = c.IsProcessed, CreatedAt = c.CreatedAt
    };
}
