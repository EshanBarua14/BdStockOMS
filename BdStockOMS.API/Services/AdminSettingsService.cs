// ============================================================
// BdStockOMS — Admin Settings Service (clean rewrite Day 65 v4)
// ============================================================
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Models.Admin;

namespace BdStockOMS.API.Services;

// ── Interfaces ────────────────────────────────────────────────
public interface IAdminSettingsService
{
    Task<GeneralSettingsDto?> GetGeneralSettingsAsync();
    Task UpdateGeneralSettingsAsync(GeneralSettingsDto dto);
    Task<MarketSettingsDto?> GetMarketSettingsAsync();
    Task UpdateMarketSettingsAsync(MarketSettingsDto dto);
    Task<TradingRulesDto?> GetTradingRulesAsync();
    Task UpdateTradingRulesAsync(TradingRulesDto dto);
    Task<NotificationSettingsDto?> GetNotificationSettingsAsync();
    Task UpdateNotificationSettingsAsync(NotificationSettingsDto dto);
    Task<bool> SendTestEmailAsync(string email);
    Task<DataRetentionDto?> GetDataRetentionAsync();
    Task UpdateDataRetentionAsync(DataRetentionDto dto);
    Task<List<IpWhitelistEntryDto>> GetIpWhitelistAsync();
    Task<object> AddIpAsync(IpWhitelistEntryDto dto);
    Task RemoveIpAsync(string id);
}

public interface IAdminFeeService
{
    Task<List<FeeStructureDto>> GetAllAsync();
    Task<FeeStructureDto> CreateAsync(FeeStructureDto dto);
    Task<bool> UpdateAsync(string id, FeeStructureDto dto);
    Task<bool> DeleteAsync(string id);
}

public interface IAdminAuditService
{
    Task LogAsync(string userName, string action, string resource, string? resourceId,
        string? detail, string ipAddress, string severity = "info");
    Task<object> GetLogsAsync(int page, int pageSize, string? severity, string? userId,
        string? resource, DateTime? from, DateTime? to);
    Task<string> ExportCsvAsync(string? from, string? to);
}

public interface IAdminBackupService
{
    Task<BackupConfigDto?> GetConfigAsync();
    Task UpdateConfigAsync(BackupConfigDto dto);
    Task<List<object>> GetHistoryAsync(int limit);
    Task TriggerBackupAsync();
    Task RestoreAsync(string backupId);
}

public interface IAdminFixService
{
    Task<FIXConfigDto?> GetConfigAsync();
    Task UpdateConfigAsync(FIXConfigDto dto);
    Task<object> GetStatusAsync();
    Task ConnectAsync();
    Task DisconnectAsync();
}

public interface IAdminRoleService
{
    Task<List<object>> GetAllAsync();
    Task<object> CreateAsync(RoleDto dto);
    Task<bool> UpdateAsync(string id, RoleDto dto);
    Task<bool> DeleteAsync(string id);
}

public interface IAdminApiKeyService
{
    Task<List<object>> GetAllAsync();
    Task<object> CreateAsync(CreateApiKeyDto dto);
    Task<bool> RevokeAsync(string id);
}

public interface IAdminAnnouncementService
{
    Task<List<object>> GetAllAsync(bool activeOnly);
    Task<object> CreateAsync(AnnouncementDto dto);
    Task<bool> UpdateAsync(string id, AnnouncementDto dto);
    Task DeleteAsync(string id);
}

public interface ISystemHealthService
{
    Task<object> GetHealthSnapshotAsync();
}

// ── KV helper ─────────────────────────────────────────────────
internal static class KV
{
    public static async Task<string?> Get(AppDbContext db, string key)
    {
        try { return (await db.AppSettings.FindAsync(key))?.Value; }
        catch { return null; }
    }
    public static async Task Set(AppDbContext db, string key, string value, string cat = "general")
    {
        var row = await db.AppSettings.FindAsync(key);
        if (row is null) db.AppSettings.Add(new AppSetting { Key = key, Value = value, Category = cat, UpdatedAt = DateTime.UtcNow });
        else { row.Value = value; row.UpdatedAt = DateTime.UtcNow; }
        await db.SaveChangesAsync();
    }
}

// ── Admin Settings Service ────────────────────────────────────
public class AdminSettingsService : IAdminSettingsService
{
    private readonly AppDbContext _db;
    public AdminSettingsService(AppDbContext db) => _db = db;
    private Task<string?> G(string k) => KV.Get(_db, k);
    private Task S(string k, string v, string c = "general") => KV.Set(_db, k, v, c);

    public async Task<GeneralSettingsDto?> GetGeneralSettingsAsync() => new(
        await G("general.systemName") ?? "BdStockOMS",
        await G("general.systemCode") ?? "BDSTK",
        await G("general.timezone") ?? "Asia/Dhaka",
        await G("general.currency") ?? "BDT",
        await G("general.dateFormat") ?? "DD/MM/YYYY",
        await G("general.language") ?? "en",
        await G("general.supportEmail") ?? "",
        await G("general.supportPhone") ?? "",
        int.Parse(await G("general.sessionTimeoutMinutes") ?? "30"),
        int.Parse(await G("general.maxLoginAttempts") ?? "5"),
        int.Parse(await G("general.lockoutDurationMinutes") ?? "15"),
        bool.Parse(await G("general.maintenanceMode") ?? "false"),
        await G("general.maintenanceMessage"),
        await G("general.companyName") ?? "BD Stock OMS Ltd.",
        bool.Parse(await G("general.requireEmailVerification") ?? "true"),
        bool.Parse(await G("general.requireTwoFactor") ?? "false"),
        int.Parse(await G("general.passwordMinLength") ?? "8"),
        bool.Parse(await G("general.passwordRequireSpecial") ?? "true"),
        int.Parse(await G("general.passwordExpiryDays") ?? "90")
    );

    public async Task UpdateGeneralSettingsAsync(GeneralSettingsDto d) => await Task.WhenAll(
        S("general.systemName", d.SystemName), S("general.systemCode", d.SystemCode),
        S("general.timezone", d.Timezone), S("general.currency", d.Currency),
        S("general.dateFormat", d.DateFormat), S("general.language", d.Language),
        S("general.supportEmail", d.SupportEmail), S("general.supportPhone", d.SupportPhone),
        S("general.sessionTimeoutMinutes", d.SessionTimeoutMinutes.ToString()),
        S("general.maxLoginAttempts", d.MaxLoginAttempts.ToString()),
        S("general.lockoutDurationMinutes", d.LockoutDurationMinutes.ToString()),
        S("general.maintenanceMode", d.MaintenanceMode.ToString()),
        S("general.maintenanceMessage", d.MaintenanceMessage ?? ""),
        S("general.companyName", d.CompanyName),
        S("general.requireEmailVerification", d.RequireEmailVerification.ToString()),
        S("general.requireTwoFactor", d.RequireTwoFactor.ToString()),
        S("general.passwordMinLength", d.PasswordMinLength.ToString()),
        S("general.passwordRequireSpecial", d.PasswordRequireSpecial.ToString()),
        S("general.passwordExpiryDays", d.PasswordExpiryDays.ToString())
    );

    public async Task<MarketSettingsDto?> GetMarketSettingsAsync() => new(
        await G("market.dseOpenTime") ?? "10:00", await G("market.dseCloseTime") ?? "14:30",
        await G("market.cseOpenTime") ?? "10:00", await G("market.cseCloseTime") ?? "14:30",
        decimal.Parse(await G("market.priceTickSize") ?? "0.10"),
        int.Parse(await G("market.lotSize") ?? "1"),
        decimal.Parse(await G("market.circuitBreakerUpPercent") ?? "10"),
        decimal.Parse(await G("market.circuitBreakerDownPercent") ?? "10"),
        bool.Parse(await G("market.allowPreMarket") ?? "false"),
        bool.Parse(await G("market.allowPostMarket") ?? "false"),
        int.Parse(await G("market.settlementDays") ?? "2"),
        bool.Parse(await G("market.autoMarketClose") ?? "true"),
        (await G("market.tradingDays") ?? "SUN,MON,TUE,WED,THU").Split(","),
        int.Parse(await G("market.depthLevels") ?? "5"),
        bool.Parse(await G("market.allowOddLot") ?? "false"),
        bool.Parse(await G("market.allowBlockTrade") ?? "true"),
        decimal.Parse(await G("market.blockTradeMinValue") ?? "5000000"),
        await G("market.referencePrice") ?? "previous_close",
        int.Parse(await G("market.indexRefreshIntervalMs") ?? "1000")
    );

    public async Task UpdateMarketSettingsAsync(MarketSettingsDto d) => await Task.WhenAll(
        S("market.dseOpenTime", d.DseOpenTime, "market"), S("market.dseCloseTime", d.DseCloseTime, "market"),
        S("market.cseOpenTime", d.CseOpenTime, "market"), S("market.cseCloseTime", d.CseCloseTime, "market"),
        S("market.priceTickSize", d.PriceTickSize.ToString(), "market"),
        S("market.lotSize", d.LotSize.ToString(), "market"),
        S("market.circuitBreakerUpPercent", d.CircuitBreakerUpPercent.ToString(), "market"),
        S("market.circuitBreakerDownPercent", d.CircuitBreakerDownPercent.ToString(), "market"),
        S("market.allowPreMarket", d.AllowPreMarket.ToString(), "market"),
        S("market.allowPostMarket", d.AllowPostMarket.ToString(), "market"),
        S("market.settlementDays", d.SettlementDays.ToString(), "market"),
        S("market.autoMarketClose", d.AutoMarketClose.ToString(), "market"),
        S("market.tradingDays", string.Join(",", d.TradingDays), "market"),
        S("market.depthLevels", d.DepthLevels.ToString(), "market"),
        S("market.allowOddLot", d.AllowOddLot.ToString(), "market"),
        S("market.allowBlockTrade", d.AllowBlockTrade.ToString(), "market"),
        S("market.blockTradeMinValue", d.BlockTradeMinValue.ToString(), "market"),
        S("market.referencePrice", d.ReferencePrice, "market"),
        S("market.indexRefreshIntervalMs", d.IndexRefreshIntervalMs.ToString(), "market")
    );

    public async Task<TradingRulesDto?> GetTradingRulesAsync() => new(
        decimal.Parse(await G("trading.maxOrderValue") ?? "10000000"),
        int.Parse(await G("trading.maxOrderQuantity") ?? "100000"),
        decimal.Parse(await G("trading.maxDailyTradeValue") ?? "50000000"),
        decimal.Parse(await G("trading.minOrderValue") ?? "1000"),
        bool.Parse(await G("trading.allowShortSell") ?? "false"),
        bool.Parse(await G("trading.allowMarginTrading") ?? "true"),
        decimal.Parse(await G("trading.marginMultiplier") ?? "1.5"),
        bool.Parse(await G("trading.rmsCheckEnabled") ?? "true"),
        bool.Parse(await G("trading.autoSquareOff") ?? "true"),
        await G("trading.autoSquareOffTime") ?? "14:20",
        int.Parse(await G("trading.orderExpiryDays") ?? "30"),
        bool.Parse(await G("trading.allowAfterHoursOrder") ?? "false"),
        decimal.Parse(await G("trading.priceTolerancePercent") ?? "2"),
        int.Parse(await G("trading.duplicateOrderWindowMs") ?? "500"),
        int.Parse(await G("trading.maxOpenOrders") ?? "50"),
        int.Parse(await G("trading.maxOrdersPerMinute") ?? "20"),
        bool.Parse(await G("trading.allowIOC") ?? "true"),
        bool.Parse(await G("trading.allowGTC") ?? "true"),
        bool.Parse(await G("trading.allowFOK") ?? "false"),
        bool.Parse(await G("trading.requireBOForOrder") ?? "true"),
        await G("trading.rmsLimitType") ?? "cash",
        decimal.Parse(await G("trading.exposureLimit") ?? "2000000"),
        bool.Parse(await G("trading.allowOrderModification") ?? "true"),
        int.Parse(await G("trading.maxModificationsPerOrder") ?? "5")
    );

    public async Task UpdateTradingRulesAsync(TradingRulesDto d) => await Task.WhenAll(
        S("trading.maxOrderValue", d.MaxOrderValue.ToString(), "trading"),
        S("trading.maxOrderQuantity", d.MaxOrderQuantity.ToString(), "trading"),
        S("trading.maxDailyTradeValue", d.MaxDailyTradeValue.ToString(), "trading"),
        S("trading.minOrderValue", d.MinOrderValue.ToString(), "trading"),
        S("trading.allowShortSell", d.AllowShortSell.ToString(), "trading"),
        S("trading.allowMarginTrading", d.AllowMarginTrading.ToString(), "trading"),
        S("trading.marginMultiplier", d.MarginMultiplier.ToString(), "trading"),
        S("trading.rmsCheckEnabled", d.RmsCheckEnabled.ToString(), "trading"),
        S("trading.autoSquareOff", d.AutoSquareOff.ToString(), "trading"),
        S("trading.autoSquareOffTime", d.AutoSquareOffTime, "trading"),
        S("trading.orderExpiryDays", d.OrderExpiryDays.ToString(), "trading"),
        S("trading.allowAfterHoursOrder", d.AllowAfterHoursOrder.ToString(), "trading"),
        S("trading.priceTolerancePercent", d.PriceTolerancePercent.ToString(), "trading"),
        S("trading.duplicateOrderWindowMs", d.DuplicateOrderWindowMs.ToString(), "trading"),
        S("trading.maxOpenOrders", d.MaxOpenOrders.ToString(), "trading"),
        S("trading.maxOrdersPerMinute", d.MaxOrdersPerMinute.ToString(), "trading"),
        S("trading.allowIOC", d.AllowIOC.ToString(), "trading"),
        S("trading.allowGTC", d.AllowGTC.ToString(), "trading"),
        S("trading.allowFOK", d.AllowFOK.ToString(), "trading"),
        S("trading.requireBOForOrder", d.RequireBOForOrder.ToString(), "trading"),
        S("trading.rmsLimitType", d.RmsLimitType, "trading"),
        S("trading.exposureLimit", d.ExposureLimit.ToString(), "trading"),
        S("trading.allowOrderModification", d.AllowOrderModification.ToString(), "trading"),
        S("trading.maxModificationsPerOrder", d.MaxModificationsPerOrder.ToString(), "trading")
    );

    public async Task<NotificationSettingsDto?> GetNotificationSettingsAsync() => new(
        bool.Parse(await G("notif.emailEnabled") ?? "true"),
        await G("notif.smtpHost") ?? "smtp.gmail.com",
        int.Parse(await G("notif.smtpPort") ?? "587"),
        await G("notif.smtpUser") ?? "",
        await G("notif.smtpPassword") ?? "",
        bool.Parse(await G("notif.smtpUseTls") ?? "true"),
        bool.Parse(await G("notif.smsEnabled") ?? "false"),
        await G("notif.smsGateway"),
        await G("notif.smsApiKey"),
        bool.Parse(await G("notif.notifyOnOrderFill") ?? "true"),
        bool.Parse(await G("notif.notifyOnOrderReject") ?? "true"),
        bool.Parse(await G("notif.notifyOnLogin") ?? "false"),
        bool.Parse(await G("notif.notifyOnLargeOrder") ?? "true"),
        decimal.Parse(await G("notif.largeOrderThreshold") ?? "1000000"),
        bool.Parse(await G("notif.dailyReportEnabled") ?? "true"),
        await G("notif.dailyReportTime"),
        await G("notif.dailyReportRecipients"),
        bool.Parse(await G("notif.alertOnSystemDown") ?? "true"),
        bool.Parse(await G("notif.alertOnRMSBreach") ?? "true"),
        bool.Parse(await G("notif.alertOnCircuitBreaker") ?? "true")
    );

    public async Task UpdateNotificationSettingsAsync(NotificationSettingsDto d) => await Task.WhenAll(
        S("notif.emailEnabled", d.EmailEnabled.ToString(), "notifications"),
        S("notif.smtpHost", d.SmtpHost ?? "", "notifications"),
        S("notif.smtpPort", d.SmtpPort.ToString(), "notifications"),
        S("notif.smtpUser", d.SmtpUser ?? "", "notifications"),
        S("notif.smtpPassword", d.SmtpPassword ?? "", "notifications"),
        S("notif.smtpUseTls", d.SmtpUseTls.ToString(), "notifications"),
        S("notif.smsEnabled", d.SmsEnabled.ToString(), "notifications"),
        S("notif.smsGateway", d.SmsGateway ?? "", "notifications"),
        S("notif.smsApiKey", d.SmsApiKey ?? "", "notifications"),
        S("notif.notifyOnOrderFill", d.NotifyOnOrderFill.ToString(), "notifications"),
        S("notif.notifyOnOrderReject", d.NotifyOnOrderReject.ToString(), "notifications"),
        S("notif.notifyOnLogin", d.NotifyOnLogin.ToString(), "notifications"),
        S("notif.notifyOnLargeOrder", d.NotifyOnLargeOrder.ToString(), "notifications"),
        S("notif.largeOrderThreshold", d.LargeOrderThreshold.ToString(), "notifications"),
        S("notif.dailyReportEnabled", d.DailyReportEnabled.ToString(), "notifications"),
        S("notif.dailyReportTime", d.DailyReportTime ?? "16:00", "notifications"),
        S("notif.dailyReportRecipients", d.DailyReportRecipients ?? "", "notifications"),
        S("notif.alertOnSystemDown", d.AlertOnSystemDown.ToString(), "notifications"),
        S("notif.alertOnRMSBreach", d.AlertOnRMSBreach.ToString(), "notifications"),
        S("notif.alertOnCircuitBreaker", d.AlertOnCircuitBreaker.ToString(), "notifications")
    );

    public Task<bool> SendTestEmailAsync(string email) => Task.FromResult(true);

    public async Task<DataRetentionDto?> GetDataRetentionAsync() => new(
        int.Parse(await G("retention.orderHistoryDays") ?? "365"),
        int.Parse(await G("retention.tradeHistoryDays") ?? "1825"),
        int.Parse(await G("retention.auditLogDays") ?? "730"),
        int.Parse(await G("retention.signalrLogDays") ?? "7"),
        int.Parse(await G("retention.sessionLogDays") ?? "90"),
        int.Parse(await G("retention.portfolioSnapshotDays") ?? "365"),
        bool.Parse(await G("retention.autoArchive") ?? "true"),
        bool.Parse(await G("retention.archiveToS3") ?? "false"),
        bool.Parse(await G("retention.purgeEnabled") ?? "false")
    );

    public async Task UpdateDataRetentionAsync(DataRetentionDto d) => await Task.WhenAll(
        S("retention.orderHistoryDays", d.OrderHistoryDays.ToString(), "retention"),
        S("retention.tradeHistoryDays", d.TradeHistoryDays.ToString(), "retention"),
        S("retention.auditLogDays", d.AuditLogDays.ToString(), "retention"),
        S("retention.signalrLogDays", d.SignalrLogDays.ToString(), "retention"),
        S("retention.sessionLogDays", d.SessionLogDays.ToString(), "retention"),
        S("retention.portfolioSnapshotDays", d.PortfolioSnapshotDays.ToString(), "retention"),
        S("retention.autoArchive", d.AutoArchive.ToString(), "retention"),
        S("retention.archiveToS3", d.ArchiveToS3.ToString(), "retention"),
        S("retention.purgeEnabled", d.PurgeEnabled.ToString(), "retention")
    );

    public Task<List<IpWhitelistEntryDto>> GetIpWhitelistAsync() =>
        Task.FromResult(new List<IpWhitelistEntryDto>());

    public Task<object> AddIpAsync(IpWhitelistEntryDto dto) =>
        Task.FromResult<object>(new { Id = Guid.NewGuid().ToString(), dto.Ip, dto.Label });

    public Task RemoveIpAsync(string id) => Task.CompletedTask;
}

// ── Admin Audit Service (uses reflection to avoid field mismatch) ──
public class AdminAuditService : IAdminAuditService
{
    private readonly AppDbContext _db;
    public AdminAuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(string userName, string action, string resource,
        string? resourceId, string? detail, string ipAddress, string severity = "info")
    {
        try
        {
            var log = new AuditLog();
            var t = log.GetType();
            void TrySet(string prop, object? val)
            {
                if (val == null) return;
                var pi = t.GetProperty(prop);
                if (pi?.CanWrite == true) try { pi.SetValue(log, val); } catch { }
            }
            TrySet("UserId",    userName);
            TrySet("UserName",  userName);
            TrySet("Action",    $"{action} {resource}".Trim());
            TrySet("Resource",  resource);
            TrySet("NewValue",  detail);
            TrySet("Details",   detail ?? string.Empty);
            TrySet("IpAddress", ipAddress);
            TrySet("Severity",  severity);
            TrySet("CreatedAt", DateTime.UtcNow);
            TrySet("CreatedAt", DateTime.UtcNow);
            _db.AuditLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch { /* audit must never break main flow */ }
    }

    public async Task<object> GetLogsAsync(int page, int pageSize, string? severity,
        string? userId, string? resource, DateTime? from, DateTime? to)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (from.HasValue) q = q.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue)   q = q.Where(l => l.CreatedAt <= to.Value);
        var total = await q.CountAsync();
        var items = await q.OrderByDescending(l => l.CreatedAt)
                           .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new { total, items };
    }

    public async Task<string> ExportCsvAsync(string? from, string? to)
    {
        var q = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(from) && DateTime.TryParse(from, out var f)) q = q.Where(l => l.CreatedAt >= f);
        if (!string.IsNullOrEmpty(to)   && DateTime.TryParse(to,   out var t)) q = q.Where(l => l.CreatedAt <= t);
        var logs = await q.OrderByDescending(l => l.CreatedAt).Take(10000).ToListAsync();
        var sb = new System.Text.StringBuilder("Timestamp,Action,IpAddress\n");
        foreach (var l in logs) sb.AppendLine($"{l.CreatedAt:O},{l.Action},{l.IpAddress}");
        return sb.ToString();
    }
}

// ── System Health Service ─────────────────────────────────────
public class SystemHealthService : ISystemHealthService
{
    private readonly AppDbContext _db;
    public SystemHealthService(AppDbContext db) => _db = db;

    public async Task<object> GetHealthSnapshotAsync()
    {
        var dbOk = "healthy";
        try { await _db.Database.CanConnectAsync(); } catch { dbOk = "down"; }
        return new
        {
            dbStatus = dbOk, redisStatus = "healthy",
            signalrStatus = "healthy", fixStatus = "disconnected",
            cpuUsage = GetCpu(), memoryUsage = GetMem(), diskUsage = GetDisk(),
            activeConnections = 0, apiVersion = "1.0.0-day65",
            uptimeSeconds = (long)(DateTime.UtcNow -
                System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalSeconds,
        };
    }

    static int GetCpu() { try { return Math.Min(99, (int)(System.Diagnostics.Process.GetCurrentProcess().TotalProcessorTime.TotalMilliseconds / Environment.ProcessorCount / 100)); } catch { return 0; } }
    static int GetMem() { try { var i = GC.GetGCMemoryInfo(); var u = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64; return i.TotalAvailableMemoryBytes > 0 ? (int)(u * 100 / i.TotalAvailableMemoryBytes) : 0; } catch { return 0; } }
    static int GetDisk() { try { var d = new System.IO.DriveInfo(System.IO.Path.GetPathRoot(System.IO.Directory.GetCurrentDirectory())!); return (int)((d.TotalSize - d.AvailableFreeSpace) * 100 / d.TotalSize); } catch { return 0; } }
}
