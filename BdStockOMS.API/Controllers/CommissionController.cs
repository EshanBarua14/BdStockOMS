using System.Security.Claims;
using BdStockOMS.API.Data;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/commission")]
[Authorize]
public class CommissionController : ControllerBase
{
    private readonly ICommissionCalculatorService _calculator;
    private readonly AppDbContext _db;

    public CommissionController(ICommissionCalculatorService calculator, AppDbContext db)
    {
        _calculator = calculator;
        _db         = db;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    // POST /api/commission/calculate
    [HttpPost("calculate")]
    public async Task<IActionResult> Calculate([FromBody] CalculateCommissionRequest request)
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var result = request.OrderType.ToUpper() == "BUY"
            ? await _calculator.CalculateBuyCommissionAsync(
                userId, user.BrokerageHouseId, request.TradeValue, request.Exchange)
            : await _calculator.CalculateSellCommissionAsync(
                userId, user.BrokerageHouseId, request.TradeValue, request.Exchange);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });

        return Ok(result.Value);
    }

    // GET /api/commission/rates
    [HttpGet("rates")]
    public async Task<IActionResult> GetMyRates()
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var buyRate  = await _calculator.GetEffectiveBuyRateAsync(userId, user.BrokerageHouseId);
        var sellRate = await _calculator.GetEffectiveSellRateAsync(userId, user.BrokerageHouseId);

        return Ok(new
        {
            buyRate        = buyRate * 100,
            sellRate       = sellRate * 100,
            cdblRate       = 0.015,
            exchangeFeeRate = 0.05,
            currency       = "BDT"
        });
    }

    // GET /api/commission/system-rates
    [HttpGet("system-rates")]
    [Authorize(Roles = "SuperAdmin,Admin,ComplianceOfficer")]
    public async Task<IActionResult> GetSystemRates()
    {
        var rates = await _db.CommissionRates
            .Where(r => r.IsActive)
            .OrderByDescending(r => r.EffectiveFrom)
            .Select(r => new
            {
                r.Id, r.BuyRate, r.SellRate,
                r.CDBLRate, r.DSEFeeRate,
                r.EffectiveFrom, r.EffectiveTo
            })
            .ToListAsync();

        return Ok(rates);
    }

    // POST /api/commission/system-rates
    [HttpPost("system-rates")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> SetSystemRate([FromBody] SetSystemRateRequest request)
    {
        // Deactivate current rates
        var current = await _db.CommissionRates.Where(r => r.IsActive).ToListAsync();
        foreach (var r in current)
        {
            r.IsActive    = false;
            r.EffectiveTo = DateTime.UtcNow;
        }

        _db.CommissionRates.Add(new API.Models.CommissionRate
        {
            BuyRate       = request.BuyRate,
            SellRate      = request.SellRate,
            IsActive      = true,
            EffectiveFrom = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return Ok(new { message = "System commission rate updated." });
    }
}

public record CalculateCommissionRequest(
    decimal TradeValue, string Exchange, string OrderType);

public record SetSystemRateRequest(decimal BuyRate, decimal SellRate);
