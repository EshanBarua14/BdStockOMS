using BdStockOMS.API.Data;
using BdStockOMS.API.Models.Admin;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class Day65AdminSettingsTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    [Fact]
    public async Task GetGeneralSettings_ReturnsDefaults_WhenNoKVExists()
    {
        var result = await new AdminSettingsService(CreateDb()).GetGeneralSettingsAsync();
        Assert.NotNull(result);
        Assert.Equal("BdStockOMS", result!.SystemName);
    }

    [Fact]
    public async Task UpdateGeneralSettings_ThenGet_ReturnsUpdatedValues()
    {
        var db  = CreateDb();
        var svc = new AdminSettingsService(db);
        var dto = new GeneralSettingsDto("TestOMS","TST","Asia/Dhaka","BDT","DD/MM/YYYY","en",
            "test@test.com","+8801700000000",60,3,10,false,null,"Test Corp",true,false,10,true,90);
        await svc.UpdateGeneralSettingsAsync(dto);
        var result = await svc.GetGeneralSettingsAsync();
        Assert.Equal("TestOMS", result!.SystemName);
        Assert.Equal("TST", result.SystemCode);
    }

    [Fact]
    public async Task UpdateGeneralSettings_MaintenanceMode_Persists()
    {
        var db  = CreateDb();
        var svc = new AdminSettingsService(db);
        var dto = new GeneralSettingsDto("OMS","OMS","Asia/Dhaka","BDT","DD/MM/YYYY","en",
            "","",30,5,15,true,"Under maintenance","Corp",true,false,8,true,90);
        await svc.UpdateGeneralSettingsAsync(dto);
        var result = await svc.GetGeneralSettingsAsync();
        Assert.True(result!.MaintenanceMode);
    }

    [Fact]
    public async Task GetMarketSettings_ReturnsDefaults()
    {
        var result = await new AdminSettingsService(CreateDb()).GetMarketSettingsAsync();
        Assert.NotNull(result);
        Assert.Equal("10:00", result!.DseOpenTime);
        Assert.Equal("14:30", result.DseCloseTime);
    }

    [Fact]
    public async Task UpdateMarketSettings_ThenGet_ReturnsUpdated()
    {
        var db  = CreateDb();
        var svc = new AdminSettingsService(db);
        var dto = new MarketSettingsDto("09:30","15:00","09:30","15:00",
            0.10m,1,10m,10m,false,false,2,true,
            new[]{"SUN","MON","TUE","WED","THU"},5,false,true,5000000m,"previous_close",1000);
        await svc.UpdateMarketSettingsAsync(dto);
        var result = await svc.GetMarketSettingsAsync();
        Assert.Equal("09:30", result!.DseOpenTime);
    }

    [Fact]
    public async Task GetTradingRules_ReturnsDefaults()
    {
        var result = await new AdminSettingsService(CreateDb()).GetTradingRulesAsync();
        Assert.NotNull(result);
        Assert.True(result!.RmsCheckEnabled);
    }

    [Fact]
    public async Task UpdateTradingRules_MaxOrderValue_Persists()
    {
        var db  = CreateDb();
        var svc = new AdminSettingsService(db);
        var dto = new TradingRulesDto(20000000m,200000,100000000m,500m,
            false,true,1.5m,true,true,"14:20",30,false,2m,500,50,20,
            true,true,false,true,"cash",2000000m,true,5);
        await svc.UpdateTradingRulesAsync(dto);
        var result = await svc.GetTradingRulesAsync();
        Assert.Equal(20000000m, result!.MaxOrderValue);
    }

    [Fact]
    public async Task GetDataRetention_ReturnsDefaults()
    {
        var result = await new AdminSettingsService(CreateDb()).GetDataRetentionAsync();
        Assert.NotNull(result);
        Assert.Equal(365, result!.OrderHistoryDays);
    }

    [Fact]
    public async Task UpdateDataRetention_Persists()
    {
        var db  = CreateDb();
        var svc = new AdminSettingsService(db);
        var dto = new DataRetentionDto(730,3650,1460,14,180,730,true,false,false);
        await svc.UpdateDataRetentionAsync(dto);
        var result = await svc.GetDataRetentionAsync();
        Assert.Equal(730,  result!.OrderHistoryDays);
        Assert.Equal(3650, result.TradeHistoryDays);
    }

    [Fact]
    public async Task KVSet_ThenGet_ReturnsSameValue()
    {
        var db = CreateDb();
        await KV.Set(db, "test.key", "hello", "general");
        var val = await KV.Get(db, "test.key");
        Assert.Equal("hello", val);
    }

    [Fact]
    public async Task KVGet_MissingKey_ReturnsNull()
    {
        var val = await KV.Get(CreateDb(), "nonexistent.key");
        Assert.Null(val);
    }

    [Fact]
    public async Task KVSet_Upsert_UpdatesExistingValue()
    {
        var db = CreateDb();
        await KV.Set(db, "test.key", "original", "general");
        await KV.Set(db, "test.key", "updated",  "general");
        var val = await KV.Get(db, "test.key");
        Assert.Equal("updated", val);
    }
}
