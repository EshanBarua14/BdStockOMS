using System.Security.Claims;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/rms")]
[Authorize]
public class RMSController : ControllerBase
{
    private readonly IRMSValidationService _rms;
    private readonly AppDbContext _db;

    public RMSController(IRMSValidationService rms, AppDbContext db)
    {
        _rms = rms;
        _db  = db;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    // POST /api/rms/validate-order
    [HttpPost("validate-order")]
    public async Task<IActionResult> ValidateOrder([FromBody] ValidateOrderRequest request)
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var result = await _rms.ValidateOrderAsync(
            userId, request.StockId, request.Exchange,
            request.OrderValue, request.OrderSide, user.BrokerageHouseId);

        return Ok(new
        {
            isAllowed  = result.IsAllowed,
            action     = result.Action?.ToString(),
            violations = result.Violations,
            warnings   = result.Warnings
        });
    }

    // GET /api/rms/my-limits
    [HttpGet("my-limits")]
    public async Task<IActionResult> GetMyLimits()
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var limits = await _db.RMSLimits
            .Where(r => r.EntityId == userId &&
                        r.BrokerageHouseId == user.BrokerageHouseId &&
                        r.IsActive)
            .ToListAsync();

        return Ok(limits.Any() ? limits : new object[]
        {
            new {
                level         = "Default",
                maxOrderValue = 5_000_000,
                maxDailyValue = 20_000_000,
                maxExposure   = 50_000_000,
                concentrationPct = 10
            }
        });
    }

    // POST /api/rms/set-limit
    [HttpPost("set-limit")]
    [Authorize(Roles = "SuperAdmin,Admin,ComplianceOfficer")]
    public async Task<IActionResult> SetLimit([FromBody] SetRMSLimitRequest request)
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        // Deactivate existing limit for this entity
        var existing = await _db.RMSLimits
            .Where(r => r.Level == request.Level &&
                        r.EntityId == request.EntityId &&
                        r.BrokerageHouseId == request.BrokerageHouseId &&
                        r.IsActive)
            .ToListAsync();
        foreach (var e in existing) e.IsActive = false;

        _db.RMSLimits.Add(new RMSLimit
        {
            Level            = request.Level,
            EntityId         = request.EntityId,
            EntityType       = request.EntityType,
            BrokerageHouseId = request.BrokerageHouseId,
            MaxOrderValue    = request.MaxOrderValue,
            MaxDailyValue    = request.MaxDailyValue,
            MaxExposure      = request.MaxExposure,
            ConcentrationPct = request.ConcentrationPct,
            ActionOnBreach   = request.ActionOnBreach,
            IsActive         = true,
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "RMS limit set successfully." });
    }
}

public record ValidateOrderRequest(
    int StockId, string Exchange,
    decimal OrderValue, string OrderSide);

public record SetRMSLimitRequest(
    RMSLevel Level, int? EntityId, string EntityType,
    int BrokerageHouseId, decimal MaxOrderValue,
    decimal MaxDailyValue, decimal MaxExposure,
    decimal ConcentrationPct,
    RMSAction ActionOnBreach = RMSAction.Block);
