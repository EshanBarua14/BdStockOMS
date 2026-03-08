using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminDashboardController : ControllerBase
{
    private readonly IAdminDashboardService _service;

    public AdminDashboardController(IAdminDashboardService service)
    {
        _service = service;
    }

    // Full dashboard — all stats in one call
    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var result = await _service.GetDashboardAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    // Individual sections for partial refresh
    [HttpGet("users")]
    public async Task<IActionResult> GetUserStats()
    {
        var result = await _service.GetUserStatsAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrderStats()
    {
        var result = await _service.GetOrderStatsAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("fund-requests")]
    public async Task<IActionResult> GetFundRequestStats()
    {
        var result = await _service.GetFundRequestStatsAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("system")]
    public async Task<IActionResult> GetSystemStats()
    {
        var result = await _service.GetSystemStatsAsync();
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }

    [HttpGet("activity")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int count = 10)
    {
        var result = await _service.GetRecentActivityAsync(count);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(result.Error);
    }
}
