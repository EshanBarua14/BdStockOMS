using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.SystemSettings;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 64 — SystemSettingService Tests
// ============================================================

public class Day64SystemSettingTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private void SeedSettings(AppDbContext db)
    {
        db.SystemSettings.AddRange(
            new SystemSetting { Key = "market.open",  Value = "10:00", Category = "market",  UpdatedByUserId = 1, UpdatedAt = DateTime.UtcNow },
            new SystemSetting { Key = "market.close", Value = "14:30", Category = "market",  UpdatedByUserId = 1, UpdatedAt = DateTime.UtcNow },
            new SystemSetting { Key = "rms.enabled",  Value = "true",  Category = "trading", UpdatedByUserId = 1, UpdatedAt = DateTime.UtcNow }
        );
        db.SaveChanges();
    }

    [Fact]
    public async Task GetAllAsync_WithSeeded_ReturnsAll()
    {
        var db = CreateDb();
        SeedSettings(db);
        var result = await new SystemSettingService(db).GetAllAsync(null);
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value!.Count);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDb_ReturnsEmptyList()
    {
        var result = await new SystemSettingService(CreateDb()).GetAllAsync(null);
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Fact]
    public async Task GetAllAsync_FilterByCategory_ReturnsFiltered()
    {
        var db = CreateDb();
        SeedSettings(db);
        var result = await new SystemSettingService(db).GetAllAsync("market");
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
    }

    [Fact]
    public async Task GetByKeyAsync_ExistingKey_ReturnsValue()
    {
        var db = CreateDb();
        SeedSettings(db);
        var result = await new SystemSettingService(db).GetByKeyAsync("market.open");
        Assert.True(result.IsSuccess);
        Assert.Equal("10:00", result.Value!.Value);
    }

    [Fact]
    public async Task GetByKeyAsync_NonExistentKey_ReturnsFailure()
    {
        var result = await new SystemSettingService(CreateDb()).GetByKeyAsync("nonexistent");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task GetAllAsync_OrderedByCategoryThenKey()
    {
        var db = CreateDb();
        SeedSettings(db);
        var result = await new SystemSettingService(db).GetAllAsync(null);
        var list   = result.Value!;
        Assert.Equal("market", list[0].Category);
        Assert.Equal("market.close", list[0].Key);
    }

    [Fact]
    public async Task GetAllAsync_CategoryFilter_CaseMatters()
    {
        var db = CreateDb();
        SeedSettings(db);
        var result = await new SystemSettingService(db).GetAllAsync("MARKET");
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }
}
