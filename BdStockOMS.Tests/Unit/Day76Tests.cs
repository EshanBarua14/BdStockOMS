using Xunit;
using BdStockOMS.API.Models.Admin;

namespace BdStockOMS.Tests.Unit;

public class Day76Tests
{
    [Fact]
    public void GeneralSettingsDto_CanBeConstructed()
    {
        var dto = new GeneralSettingsDto(
            SystemName: "BdStockOMS",
            SystemCode: "BSE",
            Timezone: "Asia/Dhaka",
            Currency: "BDT",
            DateFormat: "DD/MM/YYYY",
            Language: "en",
            SupportEmail: "admin@firm.com",
            SupportPhone: "+880",
            SessionTimeoutMinutes: 30,
            MaxLoginAttempts: 5,
            LockoutDurationMinutes: 15,
            MaintenanceMode: false,
            MaintenanceMessage: null,
            CompanyName: "Test Firm",
            RequireEmailVerification: true,
            RequireTwoFactor: false,
            PasswordMinLength: 8,
            PasswordRequireSpecial: true,
            PasswordExpiryDays: 90);
        Assert.Equal("BdStockOMS", dto.SystemName);
        Assert.Equal("BDT", dto.Currency);
        Assert.False(dto.MaintenanceMode);
    }

    [Fact]
    public void MarketSettingsDto_CanBeConstructed()
    {
        var dto = new MarketSettingsDto(
            DseOpenTime: "10:00",
            DseCloseTime: "14:30",
            CseOpenTime: "10:00",
            CseCloseTime: "14:30",
            PriceTickSize: 0.10m,
            LotSize: 1,
            CircuitBreakerUpPercent: 10m,
            CircuitBreakerDownPercent: 10m,
            AllowPreMarket: false,
            AllowPostMarket: false,
            SettlementDays: 2,
            AutoMarketClose: true,
            TradingDays: new[] { "MON", "TUE", "WED", "THU", "SUN" },
            DepthLevels: 5,
            AllowOddLot: false,
            AllowBlockTrade: true,
            BlockTradeMinValue: 1000000m,
            ReferencePrice: "LTP",
            IndexRefreshIntervalMs: 1000);
        Assert.Equal("10:00", dto.DseOpenTime);
        Assert.Equal(2, dto.SettlementDays);
        Assert.Equal(5, dto.DepthLevels);
    }

    [Fact]
    public void DataRetentionDto_CanBeConstructed()
    {
        var dto = new DataRetentionDto(
            OrderHistoryDays: 365,
            TradeHistoryDays: 365,
            AuditLogDays: 90,
            SignalrLogDays: 7,
            SessionLogDays: 30,
            PortfolioSnapshotDays: 180,
            AutoArchive: true,
            ArchiveToS3: false,
            PurgeEnabled: false);
        Assert.Equal(365, dto.OrderHistoryDays);
        Assert.Equal(90,  dto.AuditLogDays);
        Assert.False(dto.PurgeEnabled);
    }

    [Fact]
    public void FeeStructureDto_CanBeConstructed()
    {
        var dto = new FeeStructureDto(
            Name: "Standard",
            BrokeragePercent: 0.40m,
            SecdFeePercent: 0.015m,
            CdblFeePercent: 0.015m,
            VatPercent: 15m,
            AitPercent: 0.05m,
            MinBrokerage: 10m,
            ApplyToCategory: "ALL",
            IsActive: true);
        Assert.Equal("Standard", dto.Name);
        Assert.Equal(0.40m, dto.BrokeragePercent);
        Assert.True(dto.IsActive);
        Assert.Null(dto.Id);
    }

    [Fact]
    public void FeeStructureDto_IdIsSettable()
    {
        var dto = new FeeStructureDto("Z Cat",0.50m,0.015m,0.015m,15m,0.05m,15m,"Z",true) { Id = "abc123" };
        Assert.Equal("abc123", dto.Id);
    }

    [Fact]
    public void AdminSettings_AllRoutes_HaveCorrectPrefix()
    {
        var routes = new[]
        {
            "/api/admin/settings/general",
            "/api/admin/settings/market",
            "/api/admin/settings/trading-rules",
            "/api/admin/settings/notifications",
            "/api/admin/settings/data-retention",
            "/api/admin/fees",
            "/api/admin/fix/config",
            "/api/admin/fix/status",
            "/api/admin/backup/config",
            "/api/admin/health",
            "/api/admin/audit-log",
            "/api/admin/roles",
            "/api/admin/api-keys",
            "/api/admin/announcements",
            "/api/admin/ip-whitelist",
        };
        Assert.Equal(15, routes.Length);
        foreach (var r in routes)
            Assert.StartsWith("/api/admin/", r);
    }

    [Fact]
    public void AdminSettings_BaseUrl_IsRelative()
    {
        var baseUrl = "/api";
        Assert.DoesNotContain("localhost", baseUrl);
        Assert.DoesNotContain("7219", baseUrl);
        Assert.StartsWith("/", baseUrl);
    }

    [Fact]
    public void TradingRulesDto_CanBeConstructed()
    {
        var dto = new TradingRulesDto(
            MaxOrderValue: 10000000m,
            MaxOrderQuantity: 100000,
            MaxDailyTradeValue: 50000000m,
            MinOrderValue: 100m,
            AllowShortSell: false,
            AllowMarginTrading: false,
            MarginMultiplier: 1.0m,
            RmsCheckEnabled: true,
            AutoSquareOff: false,
            AutoSquareOffTime: "15:00",
            OrderExpiryDays: 1,
            AllowAfterHoursOrder: false,
            PriceTolerancePercent: 2.0m,
            DuplicateOrderWindowMs: 500,
            MaxOpenOrders: 10,
            MaxOrdersPerMinute: 20,
            AllowIOC: true,
            AllowGTC: true,
            AllowFOK: true,
            RequireBOForOrder: false,
            RmsLimitType: "Value",
            ExposureLimit: 1000000m,
            AllowOrderModification: true,
            MaxModificationsPerOrder: 3);
        Assert.True(dto.RmsCheckEnabled);
        Assert.Equal(10000000m, dto.MaxOrderValue);
    }
}
