using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    // Controller depends on IAuthService interface
    // not AuthService directly — loose coupling
    private readonly IAuthService _authService;

    // DI injects IAuthService automatically
    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    // POST /api/auth/register-brokerage
    // Public — no login required to register
    [HttpPost("register-brokerage")]
    public async Task<IActionResult> RegisterBrokerage(
        [FromBody] RegisterBrokerageDto dto)
    {
        // [FromBody] = read data from request body JSON
        // ModelState.IsValid = checks [Required],
        // [EmailAddress] etc from our DTO
        if (!ModelState.IsValid)
            return BadRequest(ModelState);
        // BadRequest = HTTP 400

        try
        {
            var result = await _authService
                .RegisterBrokerageAsync(dto);

            // Created = HTTP 201 — new resource created
            return Created("", result);
        }
        catch (InvalidOperationException ex)
        {
            // ex.Message = "Email already registered" etc
            return BadRequest(new { message = ex.Message });
        }
    }

    // POST /api/auth/login
    // Public — no login required obviously
    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _authService.LoginAsync(dto);

            // Ok = HTTP 200 with token in body
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            // Unauthorized = HTTP 401
            // Don't reveal if email exists or not
            return Unauthorized(new { message = ex.Message });
        }
    }
}