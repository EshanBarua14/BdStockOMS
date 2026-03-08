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

    private string GetIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

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
        var result = await _authService.LoginAsync(dto, GetIpAddress());
        if (!result.IsSuccess)
            return Unauthorized(new { message = result.Error, errorCode = result.ErrorCode });

        // Set refresh token as httpOnly cookie
        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(result.Value);
    }

    // POST /api/auth/refresh
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var refreshToken = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(refreshToken))
            return Unauthorized(new { message = "Refresh token missing.", errorCode = "NO_REFRESH_TOKEN" });

        var result = await _authService.RefreshTokenAsync(refreshToken, GetIpAddress());
        if (!result.IsSuccess)
            return Unauthorized(new { message = result.Error, errorCode = result.ErrorCode });

        SetRefreshTokenCookie(result.Value!.RefreshToken);
        return Ok(result.Value);
    }

    // POST /api/auth/logout
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var accessToken  = Request.Headers["Authorization"]
                                  .FirstOrDefault()?.Split(" ").Last() ?? "";
        var refreshToken = Request.Cookies["refreshToken"] ?? "";

        var result = await _authService.LogoutAsync(accessToken, refreshToken, GetIpAddress());

        // Clear the cookie regardless
        Response.Cookies.Delete("refreshToken");

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error });

        return Ok(new { message = "Logged out successfully." });
    }

    // GET /api/auth/me
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMe()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out int userId))
            return Unauthorized(new { message = "Invalid token." });

        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null)
            return NotFound(new { message = "User not found or deactivated." });

        return Ok(new UserResponseDto
        {
            Id                 = user.Id,
            FullName           = user.FullName,
            Email              = user.Email,
            Role               = user.Role?.Name ?? "",
            BrokerageHouseName = user.BrokerageHouse?.Name,
            BrokerageHouseId   = user.BrokerageHouseId,
            IsActive           = user.IsActive,
            CreatedAt          = user.CreatedAt
        });
    }

    // ── PRIVATE HELPERS ────────────────────────────────────────
    private void SetRefreshTokenCookie(string token)
    {
        Response.Cookies.Append("refreshToken", token, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = true,
            SameSite  = SameSiteMode.Strict,
            Expires   = DateTime.UtcNow.AddDays(7)
        });
    }
}
