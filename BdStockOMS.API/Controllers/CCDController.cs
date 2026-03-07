// Controllers/CCDController.cs
using BdStockOMS.API.DTOs.CCD;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/ccd")]
[Authorize(Roles = "CCD,Admin,BrokerageHouse")]
public class CCDController : ControllerBase
{
    private readonly ICCDService _ccdService;

    public CCDController(ICCDService ccdService)
    {
        _ccdService = ccdService;
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /api/ccd/bo-accounts/open
    // CCD opens a BO account for an investor
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("bo-accounts/open")]
    public async Task<IActionResult> OpenBOAccount([FromBody] OpenBOAccountDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (account, error) = await _ccdService.OpenBOAccountAsync(dto);
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(account);
    }

    // ─────────────────────────────────────────────────────────────────
    // POST /api/ccd/bo-accounts/deposit
    // CCD deposits cash into investor account
    // ─────────────────────────────────────────────────────────────────
    [HttpPost("bo-accounts/deposit")]
    public async Task<IActionResult> DepositCash([FromBody] DepositCashDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (account, error) = await _ccdService.DepositCashAsync(dto);
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(account);
    }

    // ─────────────────────────────────────────────────────────────────
    // PUT /api/ccd/bo-accounts/margin
    // CCD sets margin limit for a margin account
    // ─────────────────────────────────────────────────────────────────
    [HttpPut("bo-accounts/margin")]
    public async Task<IActionResult> SetMarginLimit([FromBody] SetMarginLimitDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (account, error) = await _ccdService.SetMarginLimitAsync(dto);
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(account);
    }

    // ─────────────────────────────────────────────────────────────────
    // PUT /api/ccd/bo-accounts/{userId}/activate
    // PUT /api/ccd/bo-accounts/{userId}/deactivate
    // ─────────────────────────────────────────────────────────────────
    [HttpPut("bo-accounts/{userId:int}/activate")]
    public async Task<IActionResult> ActivateBOAccount(int userId)
    {
        var (account, error) = await _ccdService.ToggleBOAccountAsync(userId, true);
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(account);
    }

    [HttpPut("bo-accounts/{userId:int}/deactivate")]
    public async Task<IActionResult> DeactivateBOAccount(int userId)
    {
        var (account, error) = await _ccdService.ToggleBOAccountAsync(userId, false);
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(account);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET /api/ccd/bo-accounts
    // CCD views all investor BO accounts in their brokerage
    // ─────────────────────────────────────────────────────────────────
    [HttpGet("bo-accounts")]
    public async Task<IActionResult> GetBOAccounts()
    {
        var brokerageHouseId = GetBrokerageHouseId();
        var accounts = await _ccdService.GetBOAccountsAsync(brokerageHouseId);
        return Ok(accounts);
    }

    // ─────────────────────────────────────────────────────────────────
    // PUT /api/ccd/orders/{orderId}/settle
    // CCD settles an executed order → Completed + portfolio updated
    // ─────────────────────────────────────────────────────────────────
    [HttpPut("orders/{orderId:int}/settle")]
    public async Task<IActionResult> SettleOrder(int orderId)
    {
        var (order, error) = await _ccdService.SettleOrderAsync(orderId);
        if (error != null)
            return BadRequest(new { message = error });

        return Ok(order);
    }

    private int GetBrokerageHouseId()
    {
        var claim = User.FindFirst("BrokerageHouseId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }
}
