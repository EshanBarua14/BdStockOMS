// ============================================================
// BdStockOMS — Admin Settings DTOs
// File: BdStockOMS.API/Models/AdminDtos.cs
// ============================================================
namespace BdStockOMS.API.Models.Admin;

public record GeneralSettingsDto(
    string SystemName, string SystemCode, string Timezone, string Currency,
    string DateFormat, string Language, string SupportEmail, string SupportPhone,
    int SessionTimeoutMinutes, int MaxLoginAttempts, int LockoutDurationMinutes,
    bool MaintenanceMode, string? MaintenanceMessage, string CompanyName,
    bool RequireEmailVerification, bool RequireTwoFactor, int PasswordMinLength,
    bool PasswordRequireSpecial, int PasswordExpiryDays);

public record MarketSettingsDto(
    string DseOpenTime, string DseCloseTime, string CseOpenTime, string CseCloseTime,
    decimal PriceTickSize, int LotSize, decimal CircuitBreakerUpPercent, decimal CircuitBreakerDownPercent,
    bool AllowPreMarket, bool AllowPostMarket, int SettlementDays, bool AutoMarketClose,
    string[] TradingDays, int DepthLevels, bool AllowOddLot, bool AllowBlockTrade,
    decimal BlockTradeMinValue, string ReferencePrice, int IndexRefreshIntervalMs);

public record TradingRulesDto(
    decimal MaxOrderValue, int MaxOrderQuantity, decimal MaxDailyTradeValue, decimal MinOrderValue,
    bool AllowShortSell, bool AllowMarginTrading, decimal MarginMultiplier, bool RmsCheckEnabled,
    bool AutoSquareOff, string AutoSquareOffTime, int OrderExpiryDays, bool AllowAfterHoursOrder,
    decimal PriceTolerancePercent, int DuplicateOrderWindowMs, int MaxOpenOrders,
    int MaxOrdersPerMinute, bool AllowIOC, bool AllowGTC, bool AllowFOK,
    bool RequireBOForOrder, string RmsLimitType, decimal ExposureLimit,
    bool AllowOrderModification, int MaxModificationsPerOrder);

public record NotificationSettingsDto(
    bool EmailEnabled, string? SmtpHost, int SmtpPort, string? SmtpUser, string? SmtpPassword, bool SmtpUseTls,
    bool SmsEnabled, string? SmsGateway, string? SmsApiKey,
    bool NotifyOnOrderFill, bool NotifyOnOrderReject, bool NotifyOnLogin,
    bool NotifyOnLargeOrder, decimal LargeOrderThreshold,
    bool DailyReportEnabled, string? DailyReportTime, string? DailyReportRecipients,
    bool AlertOnSystemDown, bool AlertOnRMSBreach, bool AlertOnCircuitBreaker);

public record DataRetentionDto(
    int OrderHistoryDays, int TradeHistoryDays, int AuditLogDays,
    int SignalrLogDays, int SessionLogDays, int PortfolioSnapshotDays,
    bool AutoArchive, bool ArchiveToS3, bool PurgeEnabled);

public record FeeStructureDto(
    string Name, decimal BrokeragePercent, decimal SecdFeePercent, decimal CdblFeePercent,
    decimal VatPercent, decimal AitPercent, decimal MinBrokerage, string ApplyToCategory, bool IsActive)
{
    public string? Id { get; init; }
}

public record FIXConfigDto(
    bool Enabled, string SenderCompId, string TargetCompId, string Host, int Port,
    int HeartbeatIntervalSec, int ReconnectIntervalSec, bool LogMessages, bool UseSSL,
    string FixVersion, bool ResetOnLogon, bool ResetOnLogout,
    int MaxReconnectAttempts, int MessageQueueSize, int SendingTimeToleranceSec)
{
    public string? Password { get; set; }
}

public record BackupConfigDto(
    bool AutoBackupEnabled, string BackupFrequency, string BackupTime, int RetentionDays,
    bool S3Enabled, string? S3Bucket, string? S3Region, string? S3AccessKeyIn, string? S3SecretKeyIn)
{
    public string? S3AccessKey { get; set; } = S3AccessKeyIn;
    public string? S3SecretKey { get; set; } = S3SecretKeyIn;
}

public record RoleDto(string Name, string Description, List<PermissionDto> Permissions)
{
    public string? Id { get; init; }
}

public record PermissionDto(string Module, List<string> Actions);

public record CreateApiKeyDto(string Name, List<string> Scopes, string? ExpiresAt);

public record AnnouncementDto(string Title, string Body, string Type, bool Active, bool Pinned, string? ExpiresAt);

public record IpWhitelistEntryDto(string Ip, string? Label);

public record TestEmailRequest(string Email);
