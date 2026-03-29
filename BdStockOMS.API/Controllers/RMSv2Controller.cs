using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/rms/v2")]
[Authorize]
public class RMSv2Controller : ControllerBase
{
    private readonly IRMSCascadeService _cascade;
    private readonly IEDRService        _edr;
    private readonly ITenantContext     _tenant;

    public RMSv2Controller(IRMSCascadeService cascade, IEDRService edr, ITenantContext tenant)
    {
        _cascade = cascade; _edr = edr; _tenant = tenant;
    }

    private int GetUserId() => int.TryParse(
        User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpPost("validate")]
    public async Task<IActionResult> ValidateCascade([FromBody] CascadeValidateRequest req)
    {
        var result = await _cascade.CheckCascadeAsync(
            req.InvestorId, _tenant.BrokerageHouseId,
            req.BasketId, req.OrderValue, req.OrderSide);
        return Ok(result);
    }

    [HttpGet("edr/{investorId}")]
    [Authorize(Roles = "SuperAdmin,Admin,Trader,CCD")]
    public async Task<IActionResult> GetEDR(int investorId)
    {
        var result = await _edr.CalculateAsync(investorId, _tenant.BrokerageHouseId);
        return Ok(result);
    }

    [HttpGet("edr/my")]
    public async Task<IActionResult> GetMyEDR()
    {
        var result = await _edr.CalculateAsync(GetUserId(), _tenant.BrokerageHouseId);
        return Ok(result);
    }

    [HttpPost("edr/{investorId}/snapshot")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> SaveEDRSnapshot(int investorId)
    {
        var snap = await _edr.SaveSnapshotAsync(investorId, _tenant.BrokerageHouseId);
        return Ok(snap);
    }

    [HttpGet("cascade/{investorId}")]
    [Authorize(Roles = "SuperAdmin,Admin,Trader")]
    public async Task<IActionResult> GetCascadeLimits(int investorId)
    {
        var limits = await _cascade.GetCascadeLimitsAsync(investorId, _tenant.BrokerageHouseId);
        return Ok(limits);
    }

    [HttpPost("limits")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SetLimit([FromBody] RMSLimitV2 limit)
    {
        limit.BrokerageHouseId = _tenant.BrokerageHouseId;
        await _cascade.SetLimitAsync(limit);
        return Ok(new { message = "RMS limit set." });
    }

    [HttpGet("margin-tiers")]
    public IActionResult GetMarginTiers() => Ok(new[]
    {
        new { tier = "Safe",     threshold = "< 50%",  action = "None"  },
        new { tier = "Watch",    threshold = "50-75%", action = "Warn"  },
        new { tier = "Warning",  threshold = "75-90%", action = "Alert" },
        new { tier = "Critical", threshold = "> 90%",  action = "Block" },
    });
}

public record CascadeValidateRequest(
    int InvestorId, decimal OrderValue, string OrderSide, int? BasketId = null);
