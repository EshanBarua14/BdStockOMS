using System.Security.Claims;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/password")]
[Authorize]
public class PasswordController : ControllerBase
{
    private readonly IPasswordService _passwordService;
    private readonly ITwoFactorService _twoFactorService;

    public PasswordController(IPasswordService passwordService,
                              ITwoFactorService twoFactorService)
    {
        _passwordService  = passwordService;
        _twoFactorService = twoFactorService;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("userId")?.Value;
        return int.TryParse(claim, out int id) ? id : 0;
    }

    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    // POST /api/password/change
    [HttpPost("change")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _passwordService.ChangePasswordAsync(
            GetUserId(), request.CurrentPassword, request.NewPassword, GetIp());

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });

        return Ok(new { message = "Password changed successfully." });
    }

    // POST /api/password/validate-strength
    [HttpPost("validate-strength")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateStrength([FromBody] ValidateStrengthRequest request)
    {
        var result = await _passwordService.ValidatePasswordStrengthAsync(request.Password);
        return Ok(new { isValid = result.IsSuccess, error = result.Error });
    }

    // POST /api/2fa/generate
    [HttpPost("~/api/2fa/generate")]
    public async Task<IActionResult> GenerateOtp([FromBody] GenerateOtpRequest request)
    {
        var userId = GetUserId();
        var otp    = await _twoFactorService.GenerateOtpAsync(userId, request.Purpose);

        // In production: send via SMS/email — for now return in response (dev only)
        return Ok(new
        {
            message = "OTP generated. Check your registered mobile/email.",
            otp     = otp // Remove in production
        });
    }

    // POST /api/2fa/validate
    [HttpPost("~/api/2fa/validate")]
    public async Task<IActionResult> ValidateOtp([FromBody] ValidateOtpRequest request)
    {
        var userId = GetUserId();
        var result = await _twoFactorService.ValidateOtpAsync(
            userId, request.OtpCode, request.Purpose);

        if (!result.IsSuccess)
            return BadRequest(new { message = result.Error, errorCode = result.ErrorCode });

        // If user wants to trust this device
        string? deviceToken = null;
        if (request.TrustDevice)
        {
            deviceToken = await _twoFactorService.AddTrustedDeviceAsync(
                userId, request.DeviceName ?? "Unknown Device", GetIp());
        }

        return Ok(new
        {
            message     = "OTP validated successfully.",
            deviceToken = deviceToken
        });
    }

    // POST /api/2fa/trust-device
    [HttpPost("~/api/2fa/trust-device")]
    public async Task<IActionResult> AddTrustedDevice([FromBody] TrustDeviceRequest request)
    {
        var userId      = GetUserId();
        var deviceToken = await _twoFactorService.AddTrustedDeviceAsync(
            userId, request.DeviceName, GetIp());

        return Ok(new { deviceToken, message = "Device trusted for 30 days." });
    }

    // DELETE /api/2fa/trusted-devices
    [HttpDelete("~/api/2fa/trusted-devices")]
    public async Task<IActionResult> RevokeAllTrustedDevices()
    {
        await _twoFactorService.RevokeAllTrustedDevicesAsync(GetUserId());
        return Ok(new { message = "All trusted devices revoked." });
    }
}

// ── REQUEST DTOs ───────────────────────────────────────────────
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public record ValidateStrengthRequest(string Password);
public record GenerateOtpRequest(string Purpose);
public record ValidateOtpRequest(
    string OtpCode, string Purpose,
    bool TrustDevice = false, string? DeviceName = null);
public record TrustDeviceRequest(string DeviceName);
