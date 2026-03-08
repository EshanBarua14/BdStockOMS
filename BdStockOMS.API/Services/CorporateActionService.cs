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
        var q = _context.CorporateActions
            .Include(c => c.Stock)
            .AsQueryable();

        if (stockId.HasValue)
            q = q.Where(c => c.StockId == stockId.Value);

        if (isProcessed.HasValue)
            q = q.Where(c => c.IsProcessed == isProcessed.Value);

        var items = await q
            .OrderByDescending(c => c.RecordDate)
            .Select(c => ToDto(c))
            .ToListAsync();

        return Result<List<CorporateActionResponseDto>>.Success(items);
    }

    public async Task<Result<CorporateActionResponseDto>> GetByIdAsync(int id)
    {
        var entity = await _context.CorporateActions
            .Include(c => c.Stock)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity == null)
            return Result<CorporateActionResponseDto>.Failure("Corporate action not found.");

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
        if (stock == null)
            return Result<CorporateActionResponseDto>.Failure("Stock not found.");

        if (!Enum.TryParse<CorporateActionType>(dto.Type, true, out var actionType))
            return Result<CorporateActionResponseDto>.Failure($"Invalid corporate action type: {dto.Type}");

        if (dto.Value <= 0)
            return Result<CorporateActionResponseDto>.Failure("Value must be greater than zero.");

        var entity = new CorporateAction
        {
            StockId = dto.StockId,
            Type = actionType,
            Value = dto.Value,
            RecordDate = dto.RecordDate,
            PaymentDate = dto.PaymentDate,
            Description = dto.Description,
            IsProcessed = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.CorporateActions.Add(entity);
        await _context.SaveChangesAsync();

        entity.Stock = stock;
        return Result<CorporateActionResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<CorporateActionResponseDto>> UpdateAsync(int id, UpdateCorporateActionDto dto)
    {
        var entity = await _context.CorporateActions
            .Include(c => c.Stock)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (entity == null)
            return Result<CorporateActionResponseDto>.Failure("Corporate action not found.");

        if (entity.IsProcessed)
            return Result<CorporateActionResponseDto>.Failure("Cannot update an already processed corporate action.");

        if (dto.Value <= 0)
            return Result<CorporateActionResponseDto>.Failure("Value must be greater than zero.");

        entity.Value = dto.Value;
        entity.RecordDate = dto.RecordDate;
        entity.PaymentDate = dto.PaymentDate;
        entity.Description = dto.Description;
        entity.IsProcessed = dto.IsProcessed;

        await _context.SaveChangesAsync();
        return Result<CorporateActionResponseDto>.Success(ToDto(entity));
    }

    public async Task<Result<bool>> MarkProcessedAsync(int id)
    {
        var entity = await _context.CorporateActions.FindAsync(id);
        if (entity == null)
            return Result<bool>.Failure("Corporate action not found.");

        if (entity.IsProcessed)
            return Result<bool>.Failure("Corporate action is already processed.");

        entity.IsProcessed = true;
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DeleteAsync(int id)
    {
        var entity = await _context.CorporateActions.FindAsync(id);
        if (entity == null)
            return Result<bool>.Failure("Corporate action not found.");

        if (entity.IsProcessed)
            return Result<bool>.Failure("Cannot delete a processed corporate action.");

        _context.CorporateActions.Remove(entity);
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static CorporateActionResponseDto ToDto(CorporateAction c) => new()
    {
        Id = c.Id,
        StockId = c.StockId,
        TradingCode = c.Stock?.TradingCode ?? string.Empty,
        CompanyName = c.Stock?.CompanyName ?? string.Empty,
        Type = c.Type.ToString(),
        Value = c.Value,
        RecordDate = c.RecordDate,
        PaymentDate = c.PaymentDate,
        Description = c.Description,
        IsProcessed = c.IsProcessed,
        CreatedAt = c.CreatedAt
    };
}
