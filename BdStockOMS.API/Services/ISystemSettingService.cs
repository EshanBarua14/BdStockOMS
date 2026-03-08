using BdStockOMS.API.Common;
using BdStockOMS.API.DTOs.SystemSettings;

namespace BdStockOMS.API.Services;

public interface ISystemSettingService
{
    // Get all settings, optionally filtered by category
    Task<Result<List<SystemSettingResponseDto>>> GetAllAsync(string? category);

    // Get one setting by its key (e.g. "market_open_time")
    Task<Result<SystemSettingResponseDto>> GetByKeyAsync(string key);

    // Create a new setting
    Task<Result<SystemSettingResponseDto>> CreateAsync(int userId, CreateSystemSettingDto dto);

    // Update an existing setting's value
    Task<Result<SystemSettingResponseDto>> UpdateAsync(int userId, string key, UpdateSystemSettingDto dto);

    // Delete a setting
    Task<Result<bool>> DeleteAsync(string key);
}
