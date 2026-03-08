using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class CommissionCalculatorTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private CommissionCalculatorService CreateService(AppDbContext db) =>
        new CommissionCalculatorService(db);

    private async Task SeedBaseDataAsync(AppDbContext db)
    {
        db.Roles.Add(new Role { Id = 1, Name = "Investor" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test Brokerage", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 1, FullName = "Test Investor", Email = "investor@test.com",
            PasswordHash = "hash", Phone = "01700000000",
            RoleId = 1, BrokerageHouseId = 1
        });
        await db.SaveChangesAsync();
    }

    // ── CALCULATE FROM RATE ───────────────────────────────────

    [Fact]
    public async Task CalculateFromRate_BuyOrder_AddsChargesToTradeValue()
    {
        var svc       = CreateService(CreateDb());
        var tradeValue = 100000m;
        var brokerRate = 0.005m; // 0.5%

        var result = await svc.CalculateFromRateAsync(tradeValue, brokerRate, "DSE", "BUY");

        Assert.Equal(500m,    result.BrokerCommission); // 0.5% of 100000
        Assert.Equal(15m,     result.CDBLCharge);       // 0.015% of 100000
        Assert.Equal(50m,     result.ExchangeFee);      // 0.05% of 100000
        Assert.Equal(565m,    result.TotalCharges);
        Assert.Equal(100565m, result.NetAmount);
    }

    [Fact]
    public async Task CalculateFromRate_SellOrder_DeductsChargesFromTradeValue()
    {
        var svc       = CreateService(CreateDb());
        var tradeValue = 100000m;
        var brokerRate = 0.005m;

        var result = await svc.CalculateFromRateAsync(tradeValue, brokerRate, "DSE", "SELL");

        Assert.Equal(565m,    result.TotalCharges);
        Assert.Equal(99435m,  result.NetAmount);
    }

    [Fact]
    public async Task CalculateFromRate_CSEExchange_UseCSEFeeRate()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.CalculateFromRateAsync(100000m, 0.005m, "CSE", "BUY");

        Assert.Equal("CSE", result.Exchange);
        Assert.Equal(0.0005m, result.ExchangeFeeRate);
    }

    [Fact]
    public async Task CalculateFromRate_DSEExchange_UseDSEFeeRate()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.CalculateFromRateAsync(100000m, 0.005m, "DSE", "BUY");

        Assert.Equal("DSE", result.Exchange);
        Assert.Equal(0.0005m, result.ExchangeFeeRate);
    }

    [Fact]
    public async Task CalculateFromRate_CDBLRateIsFixed()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.CalculateFromRateAsync(200000m, 0.003m, "DSE", "BUY");

        Assert.Equal(0.00015m, result.CDBLRate);
        Assert.Equal(30m, result.CDBLCharge); // 0.015% of 200000
    }

    [Fact]
    public async Task CalculateFromRate_LargeTradeValue_CorrectCalculation()
    {
        var svc       = CreateService(CreateDb());
        var tradeValue = 10000000m; // 1 crore BDT
        var brokerRate = 0.004m;    // 0.4%

        var result = await svc.CalculateFromRateAsync(tradeValue, brokerRate, "DSE", "BUY");

        Assert.Equal(40000m,   result.BrokerCommission);
        Assert.Equal(1500m,    result.CDBLCharge);
        Assert.Equal(5000m,    result.ExchangeFee);
        Assert.Equal(46500m,   result.TotalCharges);
        Assert.Equal(10046500m, result.NetAmount);
    }

    // ── EFFECTIVE RATE RESOLUTION ─────────────────────────────

    [Fact]
    public async Task GetEffectiveBuyRate_NoRatesInDb_ReturnsDefault()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var rate = await svc.GetEffectiveBuyRateAsync(1, 1);
        Assert.Equal(0.005m, rate); // 0.5% default
    }

    [Fact]
    public async Task GetEffectiveBuyRate_SystemRateExists_UsesSystemRate()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);
        db.CommissionRates.Add(new CommissionRate
        {
            BuyRate = 0.40m, SellRate = 0.40m,
            IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var rate = await svc.GetEffectiveBuyRateAsync(1, 1);
        Assert.Equal(0.004m, rate); // 0.40 / 100
    }

    [Fact]
    public async Task GetEffectiveBuyRate_BrokerageRateExists_UsesBrokerageRate()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);
        db.CommissionRates.Add(new CommissionRate
        {
            BuyRate = 0.50m, SellRate = 0.50m,
            IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        db.BrokerageCommissionRates.Add(new BrokerageCommissionRate
        {
            BrokerageHouseId = 1, BuyRate = 0.45m, SellRate = 0.45m,
            IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var rate = await svc.GetEffectiveBuyRateAsync(1, 1);
        Assert.Equal(0.0045m, rate); // brokerage rate takes priority over system
    }

    [Fact]
    public async Task GetEffectiveBuyRate_InvestorRateExists_UsesInvestorRate()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);
        db.CommissionRates.Add(new CommissionRate
        {
            BuyRate = 0.50m, SellRate = 0.50m,
            IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        db.BrokerageCommissionRates.Add(new BrokerageCommissionRate
        {
            BrokerageHouseId = 1, BuyRate = 0.45m, SellRate = 0.45m,
            IsActive = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        db.InvestorCommissionRates.Add(new InvestorCommissionRate
        {
            InvestorId = 1, BrokerageHouseId = 1,
            BuyRate = 0.35m, SellRate = 0.35m,
            IsApproved = true, EffectiveFrom = DateTime.UtcNow.AddDays(-1)
        });
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        var rate = await svc.GetEffectiveBuyRateAsync(1, 1);
        Assert.Equal(0.0035m, rate); // investor rate takes highest priority
    }

    // ── CALCULATE BUY/SELL ────────────────────────────────────

    [Fact]
    public async Task CalculateBuyCommission_ValidRequest_ReturnsSuccess()
    {
        var db = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CalculateBuyCommissionAsync(1, 1, 50000m, "DSE");

        Assert.True(result.IsSuccess);
        Assert.Equal("BUY", result.Value!.OrderType);
        Assert.Equal(50000m, result.Value.TradeValue);
    }

    [Fact]
    public async Task CalculateBuyCommission_ZeroTradeValue_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CalculateBuyCommissionAsync(1, 1, 0m, "DSE");

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TRADE_VALUE", result.ErrorCode);
    }

    [Fact]
    public async Task CalculateSellCommission_ValidRequest_ReturnsSuccess()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CalculateSellCommissionAsync(1, 1, 75000m, "DSE");

        Assert.True(result.IsSuccess);
        Assert.Equal("SELL", result.Value!.OrderType);
        Assert.True(result.Value.NetAmount < 75000m);
    }

    [Fact]
    public async Task CalculateSellCommission_NegativeTradeValue_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedBaseDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CalculateSellCommissionAsync(1, 1, -1000m, "DSE");

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_TRADE_VALUE", result.ErrorCode);
    }

    [Fact]
    public async Task CommissionBreakdown_TotalCharges_EqualsSumOfComponents()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.CalculateFromRateAsync(100000m, 0.005m, "DSE", "BUY");

        Assert.Equal(
            result.BrokerCommission + result.CDBLCharge + result.ExchangeFee,
            result.TotalCharges);
    }
}
