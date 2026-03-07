// Controllers/AuthController.cs
using System.Security.Claims;
using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.DTOs.User;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register-brokerage
    [HttpPost("register-brokerage")]
    public async Task<IActionResult> RegisterBrokerage([FromBody] RegisterBrokerageDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.RegisterBrokerageAsync(dto);
        if (result == null)
            return Conflict(new { message = "Email already registered or setup failed." });

        return Ok(result);
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _authService.LoginAsync(dto);
        if (result == null)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(result);
    }

    // GET /api/auth/me  — returns the currently logged-in user's profile
    [HttpGet("me")]
    [Authorize]   // Any authenticated user can call this
    public async Task<IActionResult> GetMe()
    {
        // Extract user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Invalid token." });

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found or deactivated." });

        return Ok(new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role?.Name ?? "",
            BrokerageHouseName = user.BrokerageHouse?.Name,
            BrokerageHouseId = user.BrokerageHouseId,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        });
    }
}