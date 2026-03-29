using BdStockOMS.API.Authorization;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
namespace BdStockOMS.API.Controllers;
[ApiController]
[Route("api/permissions")]
[Authorize]
public class UserPermissionsController : ControllerBase
{
    private readonly IUserPermissionService _svc;
    public UserPermissionsController(IUserPermissionService svc) => _svc = svc;
    private int GetUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("my")]
    public async Task<IActionResult> GetMyPermissions()
    {
        var perms = await _svc.GetUserPermissionsAsync(GetUserId());
        return Ok(perms.Select(p => new { p.Permission, p.Module, p.IsGranted, p.ExpiresAt }));
    }

    [HttpGet("my/check/{permission}")]
    public async Task<IActionResult> CheckMyPermission(string permission)
        => Ok(new { permission, hasPermission = await _svc.HasPermissionAsync(GetUserId(), permission) });

    [HttpGet("user/{userId}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> GetUserPermissions(int userId)
        => Ok(await _svc.GetUserPermissionsAsync(userId));

    [HttpPost("grant")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Grant([FromBody] GrantPermissionRequest req)
        => Ok(await _svc.GrantPermissionAsync(req.UserId, req.Permission, req.Module, GetUserId(), req.ExpiresAt));

    [HttpPost("revoke")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> Revoke([FromBody] RevokePermissionRequest req)
    {
        var ok = await _svc.RevokePermissionAsync(req.UserId, req.Permission);
        return ok ? Ok(new { message = "Permission revoked." }) : NotFound();
    }

    [HttpGet("constants")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public IActionResult GetAllConstants()
    {
        var all = Permissions.All().GroupBy(p => p.Split('.')[0])
            .Select(g => new { module = g.Key, permissions = g.ToList() });
        return Ok(all);
    }

    [HttpGet("defaults/{roleName}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public IActionResult GetDefaultsForRole(string roleName)
        => Ok(new { roleName, permissions = Permissions.DefaultsForRole(roleName) });

    [HttpPost("seed/{userId}/{roleName}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    public async Task<IActionResult> SeedDefaultPermissions(int userId, string roleName)
    {
        var defaults = Permissions.DefaultsForRole(roleName).ToList();
        var grantedBy = GetUserId();
        foreach (var perm in defaults)
            await _svc.GrantPermissionAsync(userId, perm, perm.Split('.')[0], grantedBy);
        return Ok(new { message = $"Seeded {defaults.Count} permissions.", count = defaults.Count });
    }
}
public record GrantPermissionRequest(int UserId, string Permission, string Module, DateTime? ExpiresAt);
public record RevokePermissionRequest(int UserId, string Permission);
