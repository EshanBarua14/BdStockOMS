using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Models.Admin;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class AdminFeeService : IAdminFeeService
{
    private readonly AppDbContext _db;
    public AdminFeeService(AppDbContext db) => _db = db;

    public async Task<List<FeeStructureDto>> GetAllAsync() =>
        await _db.FeeStructures.OrderBy(f => f.Name).Select(f => ToDto(f)).ToListAsync();

    public async Task<FeeStructureDto> CreateAsync(FeeStructureDto dto)
    {
        var e = new FeeStructure {
            Id = Guid.NewGuid().ToString(), Name = dto.Name,
            BrokeragePercent = dto.BrokeragePercent, SecdFeePercent = dto.SecdFeePercent,
            CdblFeePercent = dto.CdblFeePercent, VatPercent = dto.VatPercent,
            AitPercent = dto.AitPercent, MinBrokerage = dto.MinBrokerage,
            ApplyToCategory = dto.ApplyToCategory, IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.FeeStructures.Add(e);
        await _db.SaveChangesAsync();
        return ToDto(e);
    }

    public async Task<bool> UpdateAsync(string id, FeeStructureDto dto)
    {
        var e = await _db.FeeStructures.FindAsync(id);
        if (e is null) return false;
        e.Name = dto.Name; e.BrokeragePercent = dto.BrokeragePercent;
        e.SecdFeePercent = dto.SecdFeePercent; e.CdblFeePercent = dto.CdblFeePercent;
        e.VatPercent = dto.VatPercent; e.AitPercent = dto.AitPercent;
        e.MinBrokerage = dto.MinBrokerage; e.ApplyToCategory = dto.ApplyToCategory;
        e.IsActive = dto.IsActive; e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var e = await _db.FeeStructures.FindAsync(id);
        if (e is null) return false;
        _db.FeeStructures.Remove(e);
        await _db.SaveChangesAsync();
        return true;
    }

    private static FeeStructureDto ToDto(FeeStructure f) => new(
        f.Name, f.BrokeragePercent, f.SecdFeePercent, f.CdblFeePercent,
        f.VatPercent, f.AitPercent, f.MinBrokerage, f.ApplyToCategory, f.IsActive)
    { Id = f.Id };
}