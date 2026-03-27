// ============================================================
// BdStockOMS — Admin Settings Controller
// File: BdStockOMS.API/Controllers/AdminSettingsController.cs
// ============================================================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BdStockOMS.API.Services;
using BdStockOMS.API.Models.Admin;

namespace BdStockOMS.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "SuperAdmin,Admin")]
public class AdminSettingsController : ControllerBase
{
    private readonly IAdminSettingsService _settings;
    private readonly IAdminFeeService _fees;
    private readonly IAdminAuditService _audit;
    private readonly IAdminBackupService _backup;
    private readonly IAdminFixService _fix;
    private readonly IAdminRoleService _roles;
    private readonly IAdminApiKeyService _apiKeys;
    private readonly IAdminAnnouncementService _announcements;
    private readonly ISystemHealthService _health;
    private readonly ILogger<AdminSettingsController> _logger;

    public AdminSettingsController(
        IAdminSettingsService settings,
        IAdminFeeService fees,
        IAdminAuditService audit,
        IAdminBackupService backup,
        IAdminFixService fix,
        IAdminRoleService roles,
        IAdminApiKeyService apiKeys,
        IAdminAnnouncementService announcements,
        ISystemHealthService health,
        ILogger<AdminSettingsController> logger)
    {
        _settings = settings;
        _fees = fees;
        _audit = audit;
        _backup = backup;
        _fix = fix;
        _roles = roles;
        _apiKeys = apiKeys;
        _announcements = announcements;
        _health = health;
        _logger = logger;
    }

    // ── General Settings ──────────────────────────────────────
    [HttpGet("settings/general")]
    public async Task<IActionResult> GetGeneralSettings()
    {
        var settings = await _settings.GetGeneralSettingsAsync();
        return Ok(settings);
    }

    [HttpPut("settings/general")]
    public async Task<IActionResult> UpdateGeneralSettings([FromBody] GeneralSettingsDto dto)
    {
        await _settings.UpdateGeneralSettingsAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "GeneralSettings", null, null, GetIp());
        return Ok(new { success = true });
    }

    // ── Market Settings ───────────────────────────────────────
    [HttpGet("settings/market")]
    public async Task<IActionResult> GetMarketSettings()
    {
        var settings = await _settings.GetMarketSettingsAsync();
        return Ok(settings);
    }

    [HttpPut("settings/market")]
    public async Task<IActionResult> UpdateMarketSettings([FromBody] MarketSettingsDto dto)
    {
        await _settings.UpdateMarketSettingsAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "MarketSettings", null, null, GetIp());
        return Ok(new { success = true });
    }

    // ── Trading Rules ─────────────────────────────────────────
    [HttpGet("settings/trading-rules")]
    public async Task<IActionResult> GetTradingRules()
    {
        var rules = await _settings.GetTradingRulesAsync();
        return Ok(rules);
    }

    [HttpPut("settings/trading-rules")]
    public async Task<IActionResult> UpdateTradingRules([FromBody] TradingRulesDto dto)
    {
        await _settings.UpdateTradingRulesAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "TradingRules", null, null, GetIp());
        return Ok(new { success = true });
    }

    // ── Notifications ─────────────────────────────────────────
    [HttpGet("settings/notifications")]
    public async Task<IActionResult> GetNotificationSettings()
    {
        var settings = await _settings.GetNotificationSettingsAsync();
        // Mask passwords
        
        
        return Ok(settings);
    }

    [HttpPut("settings/notifications")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] NotificationSettingsDto dto)
    {
        await _settings.UpdateNotificationSettingsAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "NotificationSettings", null, null, GetIp());
        return Ok(new { success = true });
    }

    [HttpPost("notifications/test-email")]
    public async Task<IActionResult> TestEmail([FromBody] TestEmailRequest req)
    {
        var ok = await _settings.SendTestEmailAsync(req.Email);
        return ok ? Ok(new { success = true }) : StatusCode(500, new { error = "Email delivery failed" });
    }

    // ── Data Retention ────────────────────────────────────────
    [HttpGet("settings/data-retention")]
    public async Task<IActionResult> GetDataRetention()
        => Ok(await _settings.GetDataRetentionAsync());

    [HttpPut("settings/data-retention")]
    public async Task<IActionResult> UpdateDataRetention([FromBody] DataRetentionDto dto)
    {
        await _settings.UpdateDataRetentionAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "DataRetention", null, null, GetIp());
        return Ok(new { success = true });
    }

    // ── Fee Structure ─────────────────────────────────────────
    [HttpGet("fees")]
    public async Task<IActionResult> GetFees()
        => Ok(await _fees.GetAllAsync());

    [HttpPost("fees")]
    public async Task<IActionResult> CreateFee([FromBody] FeeStructureDto dto)
    {
        var created = await _fees.CreateAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "CREATE", "FeeStructure", null, created.Name, GetIp());
        return StatusCode(201, created);
    }

    [HttpPut("fees/{id}")]
    public async Task<IActionResult> UpdateFee(string id, [FromBody] FeeStructureDto dto)
    {
        var ok = await _fees.UpdateAsync(id, dto);
        if (!ok) return NotFound();
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "FeeStructure", id, null, GetIp());
        return Ok(new { success = true });
    }

    [HttpDelete("fees/{id}")]
    public async Task<IActionResult> DeleteFee(string id)
    {
        var ok = await _fees.DeleteAsync(id);
        if (!ok) return NotFound();
        await _audit.LogAsync(User.Identity!.Name!, "DELETE", "FeeStructure", id, null, GetIp());
        return NoContent();
    }

    // ── FIX Engine ────────────────────────────────────────────
    [HttpGet("fix/config")]
    public async Task<IActionResult> GetFixConfig()
    {
        var cfg = await _fix.GetConfigAsync();
        if (cfg is not null) cfg.Password = cfg.Password is not null ? "••••••••" : null;
        return Ok(cfg);
    }

    [HttpPut("fix/config")]
    public async Task<IActionResult> UpdateFixConfig([FromBody] FIXConfigDto dto)
    {
        await _fix.UpdateConfigAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "FIXConfig", null, null, GetIp());
        return Ok(new { success = true });
    }

    [HttpGet("fix/status")]
    public async Task<IActionResult> GetFixStatus()
        => Ok(await _fix.GetStatusAsync());

    [HttpPost("fix/connect")]
    public async Task<IActionResult> ConnectFix()
    {
        await _fix.ConnectAsync();
        await _audit.LogAsync(User.Identity!.Name!, "FIX_CONNECT", "FIXEngine", null, null, GetIp(), "warning");
        return Ok(new { status = "connecting" });
    }

    [HttpPost("fix/disconnect")]
    public async Task<IActionResult> DisconnectFix()
    {
        await _fix.DisconnectAsync();
        await _audit.LogAsync(User.Identity!.Name!, "FIX_DISCONNECT", "FIXEngine", null, null, GetIp(), "warning");
        return Ok(new { status = "disconnected" });
    }

    // ── Backup ────────────────────────────────────────────────
    [HttpGet("backup/config")]
    public async Task<IActionResult> GetBackupConfig()
    {
        var cfg = await _backup.GetConfigAsync();
        if (cfg is not null)
        {
            cfg.S3AccessKey = cfg.S3AccessKey is not null ? "••••••••" : null;
            cfg.S3SecretKey = cfg.S3SecretKey is not null ? "••••••••" : null;
        }
        return Ok(cfg);
    }

    [HttpPut("backup/config")]
    public async Task<IActionResult> UpdateBackupConfig([FromBody] BackupConfigDto dto)
    {
        await _backup.UpdateConfigAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "BackupConfig", null, null, GetIp());
        return Ok(new { success = true });
    }

    [HttpGet("backup/history")]
    public async Task<IActionResult> GetBackupHistory([FromQuery] int limit = 20)
        => Ok(await _backup.GetHistoryAsync(limit));

    [HttpPost("backup/trigger")]
    public async Task<IActionResult> TriggerBackup()
    {
        await _backup.TriggerBackupAsync();
        await _audit.LogAsync(User.Identity!.Name!, "BACKUP_TRIGGER", "Backup", null, null, GetIp(), "warning");
        return Ok(new { message = "Backup started" });
    }

    [HttpPost("backup/restore/{backupId}")]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> RestoreBackup(string backupId)
    {
        await _backup.RestoreAsync(backupId);
        await _audit.LogAsync(User.Identity!.Name!, "RESTORE", "Backup", backupId, null, GetIp(), "critical");
        return Ok(new { message = "Restore initiated" });
    }

    // ── System Health ─────────────────────────────────────────
    [HttpGet("health")]
    [AllowAnonymous] // Used by monitoring tools too
    public async Task<IActionResult> GetSystemHealth()
        => Ok(await _health.GetHealthSnapshotAsync());

    // ── Audit Log ─────────────────────────────────────────────
    [HttpGet("audit-log")]
    public async Task<IActionResult> GetAuditLog(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? severity = null,
        [FromQuery] string? userId = null,
        [FromQuery] string? resource = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var result = await _audit.GetLogsAsync(page, pageSize, severity, userId, resource, from, to);
        return Ok(result);
    }

    [HttpGet("audit-log/export")]
    public async Task<IActionResult> ExportAuditLog([FromQuery] string? from = null, [FromQuery] string? to = null)
    {
        var csv = await _audit.ExportCsvAsync(from, to);
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"audit-log-{DateTime.UtcNow:yyyyMMdd}.csv");
    }

    // ── Roles & Permissions ───────────────────────────────────
    [HttpGet("roles")]
    public async Task<IActionResult> GetRoles()
        => Ok(await _roles.GetAllAsync());

    [HttpPost("roles")]
    public async Task<IActionResult> CreateRole([FromBody] RoleDto dto)
    {
        var created = await _roles.CreateAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "CREATE", "Role", null, dto.Name, GetIp());
        return StatusCode(201, created);
    }

    [HttpPut("roles/{id}")]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] RoleDto dto)
    {
        var ok = await _roles.UpdateAsync(id, dto);
        if (!ok) return NotFound();
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "Role", id, null, GetIp());
        return Ok(new { success = true });
    }

    [HttpDelete("roles/{id}")]
    public async Task<IActionResult> DeleteRole(string id)
    {
        var ok = await _roles.DeleteAsync(id);
        if (!ok) return BadRequest(new { error = "Cannot delete system role or role with active users" });
        await _audit.LogAsync(User.Identity!.Name!, "DELETE", "Role", id, null, GetIp(), "warning");
        return NoContent();
    }

    // ── API Keys ──────────────────────────────────────────────
    [HttpGet("api-keys")]
    public async Task<IActionResult> GetApiKeys()
        => Ok(await _apiKeys.GetAllAsync());

    [HttpPost("api-keys")]
    public async Task<IActionResult> CreateApiKey([FromBody] CreateApiKeyDto dto)
    {
        var result = await _apiKeys.CreateAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "CREATE", "ApiKey", null, dto.Name, GetIp());
        return StatusCode(201, result);
    }

    [HttpDelete("api-keys/{id}")]
    public async Task<IActionResult> RevokeApiKey(string id)
    {
        var ok = await _apiKeys.RevokeAsync(id);
        if (!ok) return NotFound();
        await _audit.LogAsync(User.Identity!.Name!, "REVOKE", "ApiKey", id, null, GetIp(), "warning");
        return NoContent();
    }

    // ── Announcements ─────────────────────────────────────────
    [HttpGet("announcements")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAnnouncements([FromQuery] bool activeOnly = false)
        => Ok(await _announcements.GetAllAsync(activeOnly));

    [HttpPost("announcements")]
    public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementDto dto)
    {
        var created = await _announcements.CreateAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "CREATE", "Announcement", null, dto.Title, GetIp());
        return StatusCode(201, created);
    }

    [HttpPut("announcements/{id}")]
    public async Task<IActionResult> UpdateAnnouncement(string id, [FromBody] AnnouncementDto dto)
    {
        var ok = await _announcements.UpdateAsync(id, dto);
        if (!ok) return NotFound();
        await _audit.LogAsync(User.Identity!.Name!, "UPDATE", "Announcement", id, null, GetIp());
        return Ok(new { success = true });
    }

    [HttpDelete("announcements/{id}")]
    public async Task<IActionResult> DeleteAnnouncement(string id)
    {
        await _announcements.DeleteAsync(id);
        await _audit.LogAsync(User.Identity!.Name!, "DELETE", "Announcement", id, null, GetIp());
        return NoContent();
    }

    // ── IP Whitelist ──────────────────────────────────────────
    [HttpGet("ip-whitelist")]
    public async Task<IActionResult> GetIpWhitelist()
        => Ok(await _settings.GetIpWhitelistAsync());

    [HttpPost("ip-whitelist")]
    public async Task<IActionResult> AddIp([FromBody] IpWhitelistEntryDto dto)
    {
        var created = await _settings.AddIpAsync(dto);
        await _audit.LogAsync(User.Identity!.Name!, "ADD_IP", "IpWhitelist", null, dto.Ip, GetIp(), "warning");
        return StatusCode(201, created);
    }

    [HttpDelete("ip-whitelist/{id}")]
    public async Task<IActionResult> RemoveIp(string id)
    {
        await _settings.RemoveIpAsync(id);
        await _audit.LogAsync(User.Identity!.Name!, "REMOVE_IP", "IpWhitelist", id, null, GetIp(), "warning");
        return NoContent();
    }

    // ── Helpers ───────────────────────────────────────────────
    private string GetIp() =>
        HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

// ── Request/Response models ───────────────────────────────────
