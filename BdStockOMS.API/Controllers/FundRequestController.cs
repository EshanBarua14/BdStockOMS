using System.Security.Claims;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/fund-requests")]
[Authorize]
public class FundRequestController : ControllerBase
{
    private readonly IFundRequestService _fundService;
    private readonly AppDbContext _db;

    public FundRequestController(IFundRequestService fundService, AppDbContext db)
    {
        _fundService = fundService;
        _db          = db;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    // POST /api/fund-requests
    [HttpPost]
    [Authorize(Roles = "Investor")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateFundRequestDto request)
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var result = await _fundService.CreateRequestAsync(
            userId, request.Amount, request.PaymentMethod,
            request.ReferenceNumber, request.Notes, user.BrokerageHouseId);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });

        return CreatedAtAction(nameof(GetMyRequests),
            new { message = "Fund request created.", requestId = result.Value!.Id });
    }

    // GET /api/fund-requests/my
    [HttpGet("my")]
    [Authorize(Roles = "Investor")]
    public async Task<IActionResult> GetMyRequests(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        var result = await _fundService.GetMyRequestsAsync(userId, page, pageSize);
        return Ok(result);
    }

    // GET /api/fund-requests
    [HttpGet]
    [Authorize(Roles = "Trader,CCD,SuperAdmin,Admin")]
    public async Task<IActionResult> GetRequests(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] FundRequestStatus? status = null,
        [FromQuery] int? investorId = null)
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        var result = await _fundService.GetRequestsAsync(
            user.BrokerageHouseId, page, pageSize, status, investorId);
        return Ok(result);
    }

    // PUT /api/fund-requests/{id}/approve-trader
    [HttpPut("{id}/approve-trader")]
    [Authorize(Roles = "Trader,SuperAdmin")]
    public async Task<IActionResult> ApproveByTrader(
        int id, [FromBody] ApproveRequestDto? dto)
    {
        var result = await _fundService.ApproveByTraderAsync(
            id, GetUserId(), dto?.Notes);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });

        return Ok(new { message = "Fund request approved by trader." });
    }

    // PUT /api/fund-requests/{id}/approve-ccd
    [HttpPut("{id}/approve-ccd")]
    [Authorize(Roles = "CCD,SuperAdmin")]
    public async Task<IActionResult> ApproveByCCD(int id)
    {
        var result = await _fundService.ApproveByCCDAsync(id, GetUserId());

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });

        return Ok(new { message = "Fund request approved by CCD." });
    }

    // PUT /api/fund-requests/{id}/reject
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Trader,CCD,SuperAdmin,Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectRequestDto dto)
    {
        var result = await _fundService.RejectAsync(id, GetUserId(), dto.Reason);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });

        return Ok(new { message = "Fund request rejected." });
    }

    // PUT /api/fund-requests/{id}/complete
    [HttpPut("{id}/complete")]
    [Authorize(Roles = "CCD,SuperAdmin")]
    public async Task<IActionResult> Complete(int id)
    {
        var result = await _fundService.CompleteAsync(id, GetUserId());

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });

        return Ok(new { message = "Fund request completed. Balance credited." });
    }

    // GET /api/fund-requests/balance
    [HttpGet("balance")]
    [Authorize(Roles = "Investor")]
    public async Task<IActionResult> GetBalance()
    {
        var userId = GetUserId();
        var user   = await _db.Users.FindAsync(userId);
        if (user == null) return Unauthorized();

        return Ok(new { cashBalance = user.CashBalance, currency = "BDT" });
    }
}

public record CreateFundRequestDto(
    decimal Amount, PaymentMethod PaymentMethod,
    string? ReferenceNumber, string? Notes);

public record ApproveRequestDto(string? Notes);
public record RejectRequestDto(string Reason);
