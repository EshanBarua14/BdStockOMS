using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/broker-management")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class BrokerManagementController : ControllerBase
{
    private readonly IBrokerManagementService _svc;
    public BrokerManagementController(IBrokerManagementService svc) => _svc = svc;

    // ── Brokerage Houses ──────────────────────────────────────

    [HttpGet("brokerages")]
    public async Task<IActionResult> GetBrokerages()
        => Ok(await _svc.GetAllBrokeragesAsync());

    [HttpGet("brokerages/{id:int}")]
    public async Task<IActionResult> GetBrokerage(int id)
    {
        var r = await _svc.GetBrokerageByIdAsync(id);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost("brokerages")]
    public async Task<IActionResult> CreateBrokerage([FromBody] CreateBrokerageHouseDto dto)
        => Ok(await _svc.CreateBrokerageAsync(dto));

    [HttpPut("brokerages/{id:int}")]
    public async Task<IActionResult> UpdateBrokerage(int id, [FromBody] UpdateBrokerageHouseDto dto)
    {
        var r = await _svc.UpdateBrokerageAsync(id, dto);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPut("brokerages/{id:int}/activate")]
    public async Task<IActionResult> ActivateBrokerage(int id)
        => await _svc.ToggleBrokerageAsync(id, true) ? Ok() : NotFound();

    [HttpPut("brokerages/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateBrokerage(int id)
        => await _svc.ToggleBrokerageAsync(id, false) ? Ok() : NotFound();

    // ── Branch Offices ────────────────────────────────────────

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches([FromQuery] int? brokerageHouseId)
        => Ok(await _svc.GetAllBranchesAsync(brokerageHouseId));

    [HttpGet("branches/{id:int}")]
    public async Task<IActionResult> GetBranch(int id)
    {
        var r = await _svc.GetBranchByIdAsync(id);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchOfficeDto dto)
        => Ok(await _svc.CreateBranchAsync(dto));

    [HttpPut("branches/{id:int}")]
    public async Task<IActionResult> UpdateBranch(int id, [FromBody] UpdateBranchOfficeDto dto)
    {
        var r = await _svc.UpdateBranchAsync(id, dto);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPut("branches/{id:int}/activate")]
    public async Task<IActionResult> ActivateBranch(int id)
        => await _svc.ToggleBranchAsync(id, true) ? Ok() : NotFound();

    [HttpPut("branches/{id:int}/deactivate")]
    public async Task<IActionResult> DeactivateBranch(int id)
        => await _svc.ToggleBranchAsync(id, false) ? Ok() : NotFound();

    // ── BO Accounts ───────────────────────────────────────────

    [HttpGet("bo-accounts")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> GetBOAccounts([FromQuery] int? brokerageHouseId)
        => Ok(await _svc.GetAllBOAccountsAsync(brokerageHouseId));

    [HttpGet("bo-accounts/{userId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> GetBOAccount(int userId)
    {
        var r = await _svc.GetBOAccountByUserIdAsync(userId);
        return r is null ? NotFound() : Ok(r);
    }

    [HttpPut("bo-accounts/{userId:int}")]
    [Authorize(Roles = "SuperAdmin,Admin,CCD")]
    public async Task<IActionResult> UpdateBOAccount(int userId, [FromBody] UpdateBOAccountDto dto)
    {
        var r = await _svc.UpdateBOAccountAsync(userId, dto);
        return r is null ? NotFound() : Ok(r);
    }
}
