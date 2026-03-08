using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.SystemSettings;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly AppDbContext _context;

    public SystemSettingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<List<SystemSettingResponseDto>>> GetAllAsync(string? category)
    {
        var q = _context.SystemSettings
            .Include(s => s.UpdatedBy)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
            q = q.Where(s => s.Category == category);

        var items = await q
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .Select(s => ToDto(s))
            .ToListAsync();

        return Result<List<SystemSettingResponseDto>>.Success(items);
    }

    public async Task<Result<SystemSettingResponseDto>> GetByKeyAsync(string key)
    {
        var setting = await _context.SystemSettings
            .Include(s => s.UpdatedBy)
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
            return Result<SystemSettingResponseDto>.Failure($"Setting '{key}' not found.");

        return Result<SystemSettingResponseDto>.Success(ToDto(setting));
    }

    public async Task<Result<SystemSettingResponseDto>> CreateAsync(int userId, CreateSystemSettingDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Key))
            return Result<SystemSettingResponseDto>.Failure("Key is required.");

        if (string.IsNullOrWhiteSpace(dto.Value))
            return Result<SystemSettingResponseDto>.Failure("Value is required.");

        if (string.IsNullOrWhiteSpace(dto.Category))
            return Result<SystemSettingResponseDto>.Failure("Category is required.");

        // Key must be unique
        var exists = await _context.SystemSettings.AnyAsync(s => s.Key == dto.Key);
        if (exists)
            return Result<SystemSettingResponseDto>.Failure($"Setting '{dto.Key}' already exists.");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Result<SystemSettingResponseDto>.Failure("User not found.");

        var setting = new SystemSetting
        {
            Key             = dto.Key,
            Value           = dto.Value,
            Description     = dto.Description,
            Category        = dto.Category,
            IsEncrypted     = dto.IsEncrypted,
            UpdatedAt       = DateTime.UtcNow,
            UpdatedByUserId = userId
        };

        _context.SystemSettings.Add(setting);
        await _context.SaveChangesAsync();

        setting.UpdatedBy = user;
        return Result<SystemSettingResponseDto>.Success(ToDto(setting));
    }

    public async Task<Result<SystemSettingResponseDto>> UpdateAsync(int userId, string key, UpdateSystemSettingDto dto)
    {
        var setting = await _context.SystemSettings
            .Include(s => s.UpdatedBy)
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
            return Result<SystemSettingResponseDto>.Failure($"Setting '{key}' not found.");

        if (string.IsNullOrWhiteSpace(dto.Value))
            return Result<SystemSettingResponseDto>.Failure("Value is required.");

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return Result<SystemSettingResponseDto>.Failure("User not found.");

        setting.Value           = dto.Value;
        setting.Description     = dto.Description ?? setting.Description;
        setting.UpdatedAt       = DateTime.UtcNow;
        setting.UpdatedByUserId = userId;
        setting.UpdatedBy       = user;

        await _context.SaveChangesAsync();
        return Result<SystemSettingResponseDto>.Success(ToDto(setting));
    }

    public async Task<Result<bool>> DeleteAsync(string key)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key);

        if (setting == null)
            return Result<bool>.Failure($"Setting '{key}' not found.");

        _context.SystemSettings.Remove(setting);
        await _context.SaveChangesAsync();
        return Result<bool>.Success(true);
    }

    private static SystemSettingResponseDto ToDto(SystemSetting s) => new()
    {
        Id            = s.Id,
        Key           = s.Key,
        Value         = s.Value,
        Description   = s.Description,
        Category      = s.Category,
        IsEncrypted   = s.IsEncrypted,
        UpdatedAt     = s.UpdatedAt,
        UpdatedByName = s.UpdatedBy?.FullName
    };
}
