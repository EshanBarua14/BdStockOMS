using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class BrokerManagementService : IBrokerManagementService
{
    private readonly AppDbContext _db;
    public BrokerManagementService(AppDbContext db) => _db = db;

    // ── Brokerage House ───────────────────────────────────────

    public async Task<IEnumerable<BrokerageHouseDto>> GetAllBrokeragesAsync() =>
        await _db.BrokerageHouses
            .OrderBy(b => b.Name)
            .Select(b => new BrokerageHouseDto(
                b.Id, b.Name, b.LicenseNumber, b.Email, b.Phone, b.Address, b.IsActive, b.CreatedAt,
                _db.BranchOffices.Count(br => br.BrokerageHouseId == b.Id),
                _db.Users.Count(u => u.BrokerageHouseId == b.Id)
            ))
            .ToListAsync();

    public async Task<BrokerageHouseDto?> GetBrokerageByIdAsync(int id)
    {
        var b = await _db.BrokerageHouses.FindAsync(id);
        if (b is null) return null;
        return new BrokerageHouseDto(
            b.Id, b.Name, b.LicenseNumber, b.Email, b.Phone, b.Address, b.IsActive, b.CreatedAt,
            await _db.BranchOffices.CountAsync(br => br.BrokerageHouseId == b.Id),
            await _db.Users.CountAsync(u => u.BrokerageHouseId == b.Id)
        );
    }

    public async Task<BrokerageHouseDto> CreateBrokerageAsync(CreateBrokerageHouseDto dto)
    {
        var e = new BrokerageHouse
        {
            Name = dto.Name, LicenseNumber = dto.LicenseNumber,
            Email = dto.Email, Phone = dto.Phone, Address = dto.Address,
            IsActive = true, CreatedAt = DateTime.UtcNow
        };
        _db.BrokerageHouses.Add(e);
        await _db.SaveChangesAsync();
        return new BrokerageHouseDto(e.Id, e.Name, e.LicenseNumber, e.Email, e.Phone, e.Address, e.IsActive, e.CreatedAt, 0, 0);
    }

    public async Task<BrokerageHouseDto?> UpdateBrokerageAsync(int id, UpdateBrokerageHouseDto dto)
    {
        var e = await _db.BrokerageHouses.FindAsync(id);
        if (e is null) return null;
        if (dto.Name    is not null) e.Name    = dto.Name;
        if (dto.Email   is not null) e.Email   = dto.Email;
        if (dto.Phone   is not null) e.Phone   = dto.Phone;
        if (dto.Address is not null) e.Address = dto.Address;
        if (dto.IsActive is not null) e.IsActive = dto.IsActive.Value;
        await _db.SaveChangesAsync();
        return await GetBrokerageByIdAsync(id);
    }

    public async Task<bool> ToggleBrokerageAsync(int id, bool active)
    {
        var e = await _db.BrokerageHouses.FindAsync(id);
        if (e is null) return false;
        e.IsActive = active;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── Branch Office ─────────────────────────────────────────

    public async Task<IEnumerable<BranchOfficeDto>> GetAllBranchesAsync(int? brokerageHouseId = null)
    {
        var q = _db.BranchOffices.Include(b => b.BrokerageHouse).AsQueryable();
        if (brokerageHouseId.HasValue)
            q = q.Where(b => b.BrokerageHouseId == brokerageHouseId.Value);
        return await q.OrderBy(b => b.BrokerageHouseId).ThenBy(b => b.Name)
            .Select(b => new BranchOfficeDto(
                b.Id, b.BrokerageHouseId, b.BrokerageHouse.Name,
                b.Name, b.BranchCode, b.Address, b.Phone, b.Email,
                b.ManagerName, b.IsActive, b.CreatedAt,
                _db.Users.Count(u => u.BrokerageHouseId == b.BrokerageHouseId)
            ))
            .ToListAsync();
    }

    public async Task<BranchOfficeDto?> GetBranchByIdAsync(int id)
    {
        var b = await _db.BranchOffices.Include(x => x.BrokerageHouse).FirstOrDefaultAsync(x => x.Id == id);
        if (b is null) return null;
        return new BranchOfficeDto(
            b.Id, b.BrokerageHouseId, b.BrokerageHouse.Name,
            b.Name, b.BranchCode, b.Address, b.Phone, b.Email,
            b.ManagerName, b.IsActive, b.CreatedAt,
            await _db.Users.CountAsync(u => u.BrokerageHouseId == b.BrokerageHouseId)
        );
    }

    public async Task<BranchOfficeDto> CreateBranchAsync(CreateBranchOfficeDto dto)
    {
        var e = new BranchOffice
        {
            BrokerageHouseId = dto.BrokerageHouseId, Name = dto.Name,
            BranchCode = dto.BranchCode, Address = dto.Address,
            Phone = dto.Phone, Email = dto.Email, ManagerName = dto.ManagerName,
            IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        _db.BranchOffices.Add(e);
        await _db.SaveChangesAsync();
        return (await GetBranchByIdAsync(e.Id))!;
    }

    public async Task<BranchOfficeDto?> UpdateBranchAsync(int id, UpdateBranchOfficeDto dto)
    {
        var e = await _db.BranchOffices.FindAsync(id);
        if (e is null) return null;
        if (dto.Name        is not null) e.Name        = dto.Name;
        if (dto.Address     is not null) e.Address     = dto.Address;
        if (dto.Phone       is not null) e.Phone       = dto.Phone;
        if (dto.Email       is not null) e.Email       = dto.Email;
        if (dto.ManagerName is not null) e.ManagerName = dto.ManagerName;
        if (dto.IsActive    is not null) e.IsActive    = dto.IsActive.Value;
        e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return await GetBranchByIdAsync(id);
    }

    public async Task<bool> ToggleBranchAsync(int id, bool active)
    {
        var e = await _db.BranchOffices.FindAsync(id);
        if (e is null) return false;
        e.IsActive = active; e.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return true;
    }

    // ── BO Accounts ───────────────────────────────────────────

    public async Task<IEnumerable<BOAccountListDto>> GetAllBOAccountsAsync(int? brokerageHouseId = null)
    {
        var q = _db.Users.Include(u => u.BrokerageHouse)
            .Where(u => u.BONumber != null).AsQueryable();
        if (brokerageHouseId.HasValue)
            q = q.Where(u => u.BrokerageHouseId == brokerageHouseId.Value);
        return await q.OrderBy(u => u.BrokerageHouseId).ThenBy(u => u.BONumber)
            .Select(u => new BOAccountListDto(
                u.Id, u.FullName, u.Email, u.BONumber,
                u.AccountType.HasValue ? u.AccountType.Value.ToString() : null,
                u.CashBalance, u.MarginLimit, u.MarginUsed,
                u.MarginLimit - u.MarginUsed,
                u.IsActive, u.IsBOAccountActive,
                u.BrokerageHouseId, u.BrokerageHouse.Name
            ))
            .ToListAsync();
    }

    public async Task<BOAccountListDto?> GetBOAccountByUserIdAsync(int userId)
    {
        var u = await _db.Users.Include(x => x.BrokerageHouse).FirstOrDefaultAsync(x => x.Id == userId);
        if (u is null) return null;
        return new BOAccountListDto(
            u.Id, u.FullName, u.Email, u.BONumber,
            u.AccountType.HasValue ? u.AccountType.Value.ToString() : null,
            u.CashBalance, u.MarginLimit, u.MarginUsed,
            u.MarginLimit - u.MarginUsed,
            u.IsActive, u.IsBOAccountActive,
            u.BrokerageHouseId, u.BrokerageHouse.Name
        );
    }

    public async Task<BOAccountListDto?> UpdateBOAccountAsync(int userId, UpdateBOAccountDto dto)
    {
        var u = await _db.Users.FindAsync(userId);
        if (u is null) return null;
        if (dto.FullName          is not null) u.FullName          = dto.FullName;
        if (dto.Phone             is not null) u.Phone             = dto.Phone;
        if (dto.IsActive          is not null) u.IsActive          = dto.IsActive.Value;
        if (dto.IsBOAccountActive is not null) u.IsBOAccountActive = dto.IsBOAccountActive.Value;
        if (dto.MarginLimit       is not null) u.MarginLimit       = dto.MarginLimit.Value;
        await _db.SaveChangesAsync();
        return await GetBOAccountByUserIdAsync(userId);
    }
}
