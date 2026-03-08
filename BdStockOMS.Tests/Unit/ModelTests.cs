using BdStockOMS.API.Models;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class ModelTests
{
    // ── COMMISSION RATE ───────────────────────────────────────

    [Fact]
    public void CommissionRate_DefaultValues_AreCorrect()
    {
        var rate = new CommissionRate
        {
            BuyRate       = 0.50m,
            SellRate      = 0.50m,
            EffectiveFrom = DateTime.UtcNow
        };
        Assert.Equal(0.015m, rate.CDBLRate);
        Assert.Equal(0.05m,  rate.DSEFeeRate);
        Assert.True(rate.IsActive);
    }

    [Fact]
    public void BrokerageCommissionRate_DefaultIsActive()
    {
        var rate = new BrokerageCommissionRate
        {
            BrokerageHouseId = 1,
            BuyRate          = 0.40m,
            SellRate         = 0.40m,
            EffectiveFrom    = DateTime.UtcNow
        };
        Assert.True(rate.IsActive);
        Assert.Null(rate.EffectiveTo);
    }

    [Fact]
    public void InvestorCommissionRate_DefaultNotApproved()
    {
        var rate = new InvestorCommissionRate
        {
            InvestorId       = 1,
            BrokerageHouseId = 1,
            BuyRate          = 0.30m,
            SellRate         = 0.30m,
            EffectiveFrom    = DateTime.UtcNow
        };
        Assert.False(rate.IsApproved);
        Assert.Null(rate.ApprovedByUserId);
    }

    // ── RMS LIMIT ─────────────────────────────────────────────

    [Fact]
    public void RMSLimit_DefaultAction_IsBlock()
    {
        var limit = new RMSLimit
        {
            Level            = RMSLevel.Investor,
            BrokerageHouseId = 1,
            MaxOrderValue    = 1000000m,
            MaxDailyValue    = 5000000m,
            MaxExposure      = 10000000m,
            ConcentrationPct = 10m
        };
        Assert.Equal(RMSAction.Block, limit.ActionOnBreach);
        Assert.True(limit.IsActive);
    }

    [Fact]
    public void RMSLevel_HasSixLevels()
    {
        var levels = Enum.GetValues<RMSLevel>();
        Assert.Equal(6, levels.Length);
    }

    // ── SECTOR CONFIG ─────────────────────────────────────────

    [Fact]
    public void SectorConfig_DefaultIsActive()
    {
        var sector = new SectorConfig
        {
            SectorName          = "Bank",
            SectorCode          = "BANK",
            MaxConcentrationPct = 10m
        };
        Assert.True(sector.IsActive);
    }

    // ── CORPORATE ACTION ──────────────────────────────────────

    [Fact]
    public void CorporateAction_DefaultNotProcessed()
    {
        var action = new CorporateAction
        {
            StockId    = 1,
            Type       = CorporateActionType.Dividend,
            Value      = 5.0m,
            RecordDate = DateTime.UtcNow
        };
        Assert.False(action.IsProcessed);
        Assert.Null(action.PaymentDate);
    }

    [Fact]
    public void CorporateActionType_HasFiveTypes()
    {
        var types = Enum.GetValues<CorporateActionType>();
        Assert.Equal(5, types.Length);
    }

    // ── FUND REQUEST ──────────────────────────────────────────

    [Fact]
    public void FundRequest_DefaultStatus_IsPending()
    {
        var req = new FundRequest
        {
            InvestorId       = 1,
            BrokerageHouseId = 1,
            Amount           = 50000m,
            PaymentMethod    = PaymentMethod.BEFTN
        };
        Assert.Equal(FundRequestStatus.Pending, req.Status);
        Assert.Null(req.ApprovedAt);
        Assert.Null(req.CompletedAt);
    }

    [Fact]
    public void PaymentMethod_HasEightOptions()
    {
        var methods = Enum.GetValues<PaymentMethod>();
        Assert.Equal(8, methods.Length);
    }

    // ── MARKET DATA ───────────────────────────────────────────

    [Fact]
    public void MarketData_CanSetOHLCV()
    {
        var data = new MarketData
        {
            StockId            = 1,
            Exchange           = "DSE",
            Open               = 100m,
            High               = 110m,
            Low                = 95m,
            Close              = 105m,
            Volume             = 500000,
            ValueInMillionTaka = 52.5m,
            Trades             = 1200,
            Date               = DateTime.UtcNow.Date
        };
        Assert.Equal(110m, data.High);
        Assert.Equal(95m,  data.Low);
        Assert.Equal(500000, data.Volume);
    }

    // ── NEWS ITEM ─────────────────────────────────────────────

    [Fact]
    public void NewsItem_DefaultIsPublished()
    {
        var news = new NewsItem
        {
            Title   = "Market Update",
            Content = "DSE index rose today."
        };
        Assert.True(news.IsPublished);
        Assert.Equal(NewsCategory.General, news.Category);
    }

    // ── WATCHLIST ─────────────────────────────────────────────

    [Fact]
    public void Watchlist_DefaultNotDefault()
    {
        var wl = new Watchlist { UserId = 1, Name = "My Watchlist" };
        Assert.False(wl.IsDefault);
        Assert.Empty(wl.Items);
    }

    [Fact]
    public void WatchlistItem_CanBeCreated()
    {
        var item = new WatchlistItem
        {
            WatchlistId = 1,
            StockId     = 5,
            SortOrder   = 1
        };
        Assert.Equal(5, item.StockId);
        Assert.Equal(1, item.SortOrder);
    }

    // ── NOTIFICATION ──────────────────────────────────────────

    [Fact]
    public void Notification_DefaultNotRead()
    {
        var notif = new Notification
        {
            UserId  = 1,
            Type    = NotificationType.OrderExecuted,
            Title   = "Order Executed",
            Message = "Your buy order for BRAC was executed."
        };
        Assert.False(notif.IsRead);
        Assert.Null(notif.ReadAt);
    }

    [Fact]
    public void NotificationType_HasElevenTypes()
    {
        var types = Enum.GetValues<NotificationType>();
        Assert.Equal(11, types.Length);
    }

    // ── SYSTEM SETTING ────────────────────────────────────────

    [Fact]
    public void SystemSetting_DefaultNotEncrypted()
    {
        var setting = new SystemSetting
        {
            Key      = "MaxOrderValue",
            Value    = "1000000",
            Category = "Trading"
        };
        Assert.False(setting.IsEncrypted);
        Assert.Null(setting.UpdatedByUserId);
    }

    // ── ORDER AMENDMENT ───────────────────────────────────────

    [Fact]
    public void OrderAmendment_CanTrackChanges()
    {
        var amendment = new OrderAmendment
        {
            OrderId         = 1,
            AmendedByUserId = 5,
            OldQuantity     = 100,
            NewQuantity     = 150,
            OldPrice        = 55.50m,
            NewPrice        = 56.00m,
            Reason          = "Price adjustment"
        };
        Assert.Equal(100, amendment.OldQuantity);
        Assert.Equal(150, amendment.NewQuantity);
    }

    // ── TRADER REASSIGNMENT ───────────────────────────────────

    [Fact]
    public void TraderReassignment_CanBeCreated()
    {
        var reassignment = new TraderReassignment
        {
            InvestorId         = 10,
            OldTraderId        = 3,
            NewTraderId        = 7,
            ReassignedByUserId = 1,
            BrokerageHouseId   = 1,
            Reason             = "Trader left"
        };
        Assert.Equal(3, reassignment.OldTraderId);
        Assert.Equal(7, reassignment.NewTraderId);
    }

    // ── TRUSTED DEVICE ────────────────────────────────────────

    [Fact]
    public void TrustedDevice_IsActive_WhenNotRevokedAndNotExpired()
    {
        var device = new TrustedDevice
        {
            UserId      = 1,
            DeviceToken = "token123",
            DeviceName  = "Chrome on Windows",
            IpAddress   = "127.0.0.1",
            ExpiresAt   = DateTime.UtcNow.AddDays(30),
            IsRevoked   = false
        };
        Assert.True(device.IsActive);
    }

    [Fact]
    public void TrustedDevice_IsNotActive_WhenRevoked()
    {
        var device = new TrustedDevice
        {
            UserId      = 1,
            DeviceToken = "token123",
            DeviceName  = "Chrome on Windows",
            IpAddress   = "127.0.0.1",
            ExpiresAt   = DateTime.UtcNow.AddDays(30),
            IsRevoked   = true
        };
        Assert.False(device.IsActive);
    }

    [Fact]
    public void TrustedDevice_IsNotActive_WhenExpired()
    {
        var device = new TrustedDevice
        {
            UserId      = 1,
            DeviceToken = "token123",
            DeviceName  = "Old Device",
            IpAddress   = "127.0.0.1",
            ExpiresAt   = DateTime.UtcNow.AddDays(-1),
            IsRevoked   = false
        };
        Assert.False(device.IsActive);
    }
}
