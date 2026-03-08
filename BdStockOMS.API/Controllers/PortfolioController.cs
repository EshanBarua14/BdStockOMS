using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioPnlService _service;

    public PortfolioController(IPortfolioPnlService service)
    {
        _service = service;
    }

    // Full portfolio summary with all holdings and total P&L
    // Investors can only see their own — Traders/CCD/Admin can see any
    [HttpGet("{investorId:int}/summary")]
    public async Task<IActionResult> GetSummary(int investorId)
    {
        var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        // Investors can only view their own portfolio
        if (role == "Investor" && requesterId != investorId)
            return Forbid();

        var result = await _service.GetPortfolioSummaryAsync(investorId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // P&L for one specific stock holding
    [HttpGet("{investorId:int}/holding/{stockId:int}")]
    public async Task<IActionResult> GetHolding(int investorId, int stockId)
    {
        var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (role == "Investor" && requesterId != investorId)
            return Forbid();

        var result = await _service.GetHoldingAsync(investorId, stockId);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }

    // Historical portfolio value for charting (last N days)
    [HttpGet("{investorId:int}/history")]
    public async Task<IActionResult> GetHistory(int investorId, [FromQuery] int days = 30)
    {
        var requesterId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var role = User.FindFirstValue(ClaimTypes.Role);

        if (role == "Investor" && requesterId != investorId)
            return Forbid();

        var result = await _service.GetPortfolioHistoryAsync(investorId, days);
        return result.IsSuccess ? Ok(result.Value) : NotFound(result.Error);
    }
}
