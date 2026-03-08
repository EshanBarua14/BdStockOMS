using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.SystemSettings;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class SystemSettingServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private async Task<(AppDbContext db, User admin)> SeedAsync()
    {
        var db = CreateDb();

        var role = new Role { Id = 1, Name = "SuperAdmin" };
        db.Roles.Add(role);

        var bh = new BrokerageHouse
        {
            Id = 1, Name = "Test BH", LicenseNumber = "TB001",
            Email = "bh@test.com", Phone = "01000000000",
            Address = "Dhaka", IsActive = true
        };
        db.BrokerageHouses.Add(bh);

        var admin = new User
        {
            Id = 1, FullName = "Super Admin", Email = "admin@test.com",
            PasswordHash = "hash", RoleId = 1, BrokerageHouseId = 1,
            IsActive = true, CashBalance = 0
        };
        db.Users.Add(admin);

        // Seed some settings
        db.SystemSettings.AddRange(
            new SystemSetting { Id = 1, Key = "market_open_time",  Value = "10:00", Category = "Trading",  Description = "Market open time",  UpdatedAt = DateTime.UtcNow },
            new SystemSetting { Id = 2, Key = "market_close_time", Value = "14:30", Category = "Trading",  Description = "Market close time", UpdatedAt = DateTime.UtcNow },
            new SystemSetting { Id = 3, Key = "maintenance_mode",  Value = "false", Category = "System",   Description = "Maintenance mode",  UpdatedAt = DateTime.UtcNow },
            new SystemSetting { Id = 4, Key = "session_timeout",   Value = "30",    Category = "Security", Description = "Session timeout",   UpdatedAt = DateTime.UtcNow }
        );

        await db.SaveChangesAsync();
        return (db, admin);
    }

    // ── GET ALL TESTS ──────────────────────────────

    [Fact]
    public async Task GetAll_NoFilter_ReturnsAllSettings()
    {
        var (db, _) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.GetAllAsync(null);

        Assert.True(result.IsSuccess);
        Assert.Equal(4, result.Value!.Count);
    }

    [Fact]
    public async Task GetAll_FilterByCategory_ReturnsOnlyThatCategory()
    {
        var (db, _) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.GetAllAsync("Trading");

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Count);
        Assert.All(result.Value, s => Assert.Equal("Trading", s.Category));
    }

    [Fact]
    public async Task GetAll_UnknownCategory_ReturnsEmptyList()
    {
        var (db, _) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.GetAllAsync("NonExistent");

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    // ── GET BY KEY TESTS ───────────────────────────

    [Fact]
    public async Task GetByKey_ExistingKey_ReturnsSetting()
    {
        var (db, _) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.GetByKeyAsync("market_open_time");

        Assert.True(result.IsSuccess);
        Assert.Equal("10:00", result.Value!.Value);
        Assert.Equal("Trading", result.Value.Category);
    }

    [Fact]
    public async Task GetByKey_NonExistentKey_ReturnsFail()
    {
        var (db, _) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.GetByKeyAsync("nonexistent_key");

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    // ── CREATE TESTS ───────────────────────────────

    [Fact]
    public async Task Create_ValidSetting_ReturnsSuccess()
    {
        var (db, admin) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.CreateAsync(admin.Id, new CreateSystemSettingDto
        {
            Key = "max_order_value", Value = "1000000",
            Category = "Trading", Description = "Max single order value"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("max_order_value", result.Value!.Key);
        Assert.Equal("Trading", result.Value.Category);
        Assert.Equal("Super Admin", result.Value.UpdatedByName);
    }

    [Fact]
    public async Task Create_DuplicateKey_ReturnsFail()
    {
        var (db, admin) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.CreateAsync(admin.Id, new CreateSystemSettingDto
        {
            Key = "market_open_time", Value = "09:00", Category = "Trading"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("already exists", result.Error);
    }

    [Fact]
    public async Task Create_EmptyKey_ReturnsFail()
    {
        var (db, admin) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.CreateAsync(admin.Id, new CreateSystemSettingDto
        {
            Key = "", Value = "test", Category = "Trading"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Key is required", result.Error);
    }

    [Fact]
    public async Task Create_EmptyCategory_ReturnsFail()
    {
        var (db, admin) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.CreateAsync(admin.Id, new CreateSystemSettingDto
        {
            Key = "new_key", Value = "test", Category = ""
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("Category is required", result.Error);
    }

    // ── UPDATE TESTS ───────────────────────────────

    [Fact]
    public async Task Update_ExistingKey_ReturnsUpdatedValue()
    {
        var (db, admin) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.UpdateAsync(admin.Id, "market_open_time", new UpdateSystemSettingDto
        {
            Value = "09:30", Description = "Updated open time"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("09:30", result.Value!.Value);
        Assert.Equal("Super Admin", result.Value.UpdatedByName);
    }

    [Fact]
    public async Task Update_NonExistentKey_ReturnsFail()
    {
        var (db, admin) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.UpdateAsync(admin.Id, "nonexistent_key", new UpdateSystemSettingDto
        {
            Value = "test"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    // ── DELETE TESTS ───────────────────────────────

    [Fact]
    public async Task Delete_ExistingKey_ReturnsSuccess()
    {
        var (db, _) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.DeleteAsync("maintenance_mode");

        Assert.True(result.IsSuccess);
        Assert.True(result.Value);
    }

    [Fact]
    public async Task Delete_NonExistentKey_ReturnsFail()
    {
        var (db, _) = await SeedAsync();
        var svc = new SystemSettingService(db);

        var result = await svc.DeleteAsync("nonexistent_key");

        Assert.False(result.IsSuccess);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task Delete_ConfirmRemovedFromDb()
    {
        var (db, _) = await SeedAsync();
        var svc = new SystemSettingService(db);

        await svc.DeleteAsync("session_timeout");
        var result = await svc.GetByKeyAsync("session_timeout");

        Assert.False(result.IsSuccess);
    }
}
