using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/tenant-features")]
[Authorize(Roles = "SuperAdmin")]
public class TenantFeatureFlagsController : ControllerBase
{
    private readonly AppDbContext _db;

    public TenantFeatureFlagsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("{brokerageHouseId}")]
    public async Task<IActionResult> GetFlags(int brokerageHouseId)
    {
        var flags = await _db.TenantFeatureFlags
            .Where(f => f.BrokerageHouseId == brokerageHouseId)
            .OrderBy(f => f.FeatureKey)
            .ToListAsync();
        return Ok(flags);
    }

    [HttpPost("{brokerageHouseId}")]
    public async Task<IActionResult> SetFlag(int brokerageHouseId, [FromBody] SetFeatureFlagRequest request)
    {
        var existing = await _db.TenantFeatureFlags
            .FirstOrDefaultAsync(f =>
                f.BrokerageHouseId == brokerageHouseId &&
                f.FeatureKey == request.FeatureKey);

        if (existing != null)
        {
            existing.IsEnabled    = request.IsEnabled;
            existing.Value        = request.Value;
            existing.Description  = request.Description;
            existing.UpdatedAt    = DateTime.UtcNow;
            existing.SetByUserId  = User.Identity?.Name;
        }
        else
        {
            _db.TenantFeatureFlags.Add(new TenantFeatureFlag
            {
                BrokerageHouseId = brokerageHouseId,
                FeatureKey       = request.FeatureKey,
                IsEnabled        = request.IsEnabled,
                Value            = request.Value,
                Description      = request.Description,
                CreatedAt        = DateTime.UtcNow,
                UpdatedAt        = DateTime.UtcNow,
                SetByUserId      = User.Identity?.Name
            });
        }

        await _db.SaveChangesAsync();
        return Ok(new { message = $"Feature '{request.FeatureKey}' set to {request.IsEnabled}" });
    }

    [HttpDelete("{brokerageHouseId}/{featureKey}")]
    public async Task<IActionResult> DeleteFlag(int brokerageHouseId, string featureKey)
    {
        var flag = await _db.TenantFeatureFlags
            .FirstOrDefaultAsync(f =>
                f.BrokerageHouseId == brokerageHouseId &&
                f.FeatureKey == featureKey);

        if (flag == null) return NotFound();

        _db.TenantFeatureFlags.Remove(flag);
        await _db.SaveChangesAsync();
        return Ok(new { message = $"Feature '{featureKey}' removed" });
    }

    [HttpGet("all-flags")]
    public async Task<IActionResult> GetAllFlags()
    {
        var flags = await _db.TenantFeatureFlags
            .Include(f => f.BrokerageHouse)
            .OrderBy(f => f.BrokerageHouseId)
            .ThenBy(f => f.FeatureKey)
            .ToListAsync();
        return Ok(flags);
    }

    [HttpPost("bulk/{brokerageHouseId}")]
    public async Task<IActionResult> BulkSetFlags(int brokerageHouseId,
        [FromBody] List<SetFeatureFlagRequest> requests)
    {
        foreach (var request in requests)
        {
            var existing = await _db.TenantFeatureFlags
                .FirstOrDefaultAsync(f =>
                    f.BrokerageHouseId == brokerageHouseId &&
                    f.FeatureKey == request.FeatureKey);

            if (existing != null)
            {
                existing.IsEnabled   = request.IsEnabled;
                existing.Value       = request.Value;
                existing.UpdatedAt   = DateTime.UtcNow;
                existing.SetByUserId = User.Identity?.Name;
            }
            else
            {
                _db.TenantFeatureFlags.Add(new TenantFeatureFlag
                {
                    BrokerageHouseId = brokerageHouseId,
                    FeatureKey       = request.FeatureKey,
                    IsEnabled        = request.IsEnabled,
                    Value            = request.Value,
                    CreatedAt        = DateTime.UtcNow,
                    UpdatedAt        = DateTime.UtcNow,
                    SetByUserId      = User.Identity?.Name
                });
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new { message = $"{requests.Count} feature flags updated" });
    }
}

public record SetFeatureFlagRequest(
    string FeatureKey,
    bool IsEnabled,
    string? Value = null,
    string? Description = null);
