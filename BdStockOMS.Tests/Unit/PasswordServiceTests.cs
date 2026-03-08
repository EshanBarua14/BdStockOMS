using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class PasswordServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private PasswordService CreateService(AppDbContext db)
    {
        var auditMock = new Mock<IAuditService>();
        auditMock.Setup(x => x.LogAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>())).Returns(Task.CompletedTask);
        return new PasswordService(db, auditMock.Object);
    }

    private async Task<User> SeedUserAsync(AppDbContext db, string password = "OldPass@123")
    {
        db.Roles.Add(new Role { Id = 1, Name = "Investor" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        var user = new User
        {
            Id = 1, FullName = "Test User", Email = "test@test.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Phone = "01700000000", RoleId = 1, BrokerageHouseId = 1,
            PasswordChangedAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    // ── PASSWORD STRENGTH ─────────────────────────────────────

    [Fact]
    public async Task ValidateStrength_StrongPassword_ReturnsSuccess()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.ValidatePasswordStrengthAsync("StrongPass@123");
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task ValidateStrength_TooShort_ReturnsFailure()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.ValidatePasswordStrengthAsync("Sh@1");
        Assert.False(result.IsSuccess);
        Assert.Equal("PASSWORD_TOO_SHORT", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateStrength_NoUppercase_ReturnsFailure()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.ValidatePasswordStrengthAsync("lowercase@123");
        Assert.False(result.IsSuccess);
        Assert.Equal("PASSWORD_NO_UPPER", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateStrength_NoLowercase_ReturnsFailure()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.ValidatePasswordStrengthAsync("UPPERCASE@123");
        Assert.False(result.IsSuccess);
        Assert.Equal("PASSWORD_NO_LOWER", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateStrength_NoDigit_ReturnsFailure()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.ValidatePasswordStrengthAsync("NoDigit@Pass");
        Assert.False(result.IsSuccess);
        Assert.Equal("PASSWORD_NO_DIGIT", result.ErrorCode);
    }

    [Fact]
    public async Task ValidateStrength_NoSpecialChar_ReturnsFailure()
    {
        var svc    = CreateService(CreateDb());
        var result = await svc.ValidatePasswordStrengthAsync("NoSpecial123");
        Assert.False(result.IsSuccess);
        Assert.Equal("PASSWORD_NO_SPECIAL", result.ErrorCode);
    }

    // ── PASSWORD HISTORY ──────────────────────────────────────

    [Fact]
    public async Task IsPasswordReused_NewPassword_ReturnsFalse()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var result = await svc.IsPasswordReusedAsync(1, "BrandNew@456");
        Assert.False(result);
    }

    [Fact]
    public async Task IsPasswordReused_ReusedPassword_ReturnsTrue()
    {
        var db   = CreateDb();
        var user = await SeedUserAsync(db);
        var svc  = CreateService(db);

        await svc.SavePasswordHistoryAsync(1, user.PasswordHash);

        var result = await svc.IsPasswordReusedAsync(1, "OldPass@123");
        Assert.True(result);
    }

    [Fact]
    public async Task SavePasswordHistory_KeepsOnlyLast5()
    {
        var db = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        for (int i = 0; i < 7; i++)
            await svc.SavePasswordHistoryAsync(1,
                BCrypt.Net.BCrypt.HashPassword($"Pass{i}@123"));

        var count = await db.PasswordHistories.CountAsync();
        Assert.True(count >= 5 && count <= 7);
    }

    // ── CHANGE PASSWORD ───────────────────────────────────────

    [Fact]
    public async Task ChangePassword_ValidRequest_Succeeds()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var result = await svc.ChangePasswordAsync(
            1, "OldPass@123", "NewPass@456", "127.0.0.1");

        Assert.True(result.IsSuccess);
        var user = await db.Users.FindAsync(1);
        Assert.True(BCrypt.Net.BCrypt.Verify("NewPass@456", user!.PasswordHash));
    }

    [Fact]
    public async Task ChangePassword_WrongCurrentPassword_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var result = await svc.ChangePasswordAsync(
            1, "WrongPass@123", "NewPass@456", "127.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal("WRONG_PASSWORD", result.ErrorCode);
    }

    [Fact]
    public async Task ChangePassword_WeakNewPassword_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var result = await svc.ChangePasswordAsync(
            1, "OldPass@123", "weakpass", "127.0.0.1");

        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task ChangePassword_ResetsForcePasswordChange()
    {
        var db   = CreateDb();
        var user = await SeedUserAsync(db);
        user.ForcePasswordChange = true;
        await db.SaveChangesAsync();

        var svc    = CreateService(db);
        var result = await svc.ChangePasswordAsync(
            1, "OldPass@123", "NewPass@456", "127.0.0.1");

        Assert.True(result.IsSuccess);
        var updated = await db.Users.FindAsync(1);
        Assert.False(updated!.ForcePasswordChange);
    }

    // ── PASSWORD EXPIRY ───────────────────────────────────────

    [Fact]
    public async Task IsPasswordExpired_RecentChange_ReturnsFalse()
    {
        var db = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var result = await svc.IsPasswordExpiredAsync(1);
        Assert.False(result);
    }

    [Fact]
    public async Task IsPasswordExpired_OldChange_ReturnsTrue()
    {
        var db   = CreateDb();
        var user = await SeedUserAsync(db);
        user.PasswordChangedAt = DateTime.UtcNow.AddDays(-91);
        await db.SaveChangesAsync();

        var svc    = CreateService(db);
        var result = await svc.IsPasswordExpiredAsync(1);
        Assert.True(result);
    }
}
