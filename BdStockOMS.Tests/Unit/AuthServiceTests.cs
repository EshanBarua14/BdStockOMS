using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Auth;
using BdStockOMS.API.Models;
using BdStockOMS.API.Repositories;
using BdStockOMS.API.Repositories.Interfaces;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class AuthServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private IConfiguration CreateConfig() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JwtSettings:SecretKey",    "TestSecretKey-That-Is-Long-Enough-32chars!" },
                { "JwtSettings:Issuer",       "BdStockOMS" },
                { "JwtSettings:Audience",     "BdStockOMS" },
                { "JwtSettings:ExpiryInDays", "7" }
            })
            .Build();

    private AuthService CreateService(AppDbContext db)
    {
        var refreshTokenRepo  = new RefreshTokenRepository(db);
        var auditService      = new AuditService(db);
        var blacklistMock     = new Mock<ITokenBlacklistService>();
        blacklistMock.Setup(x => x.BlacklistTokenAsync(It.IsAny<string>(), It.IsAny<TimeSpan>()))
                     .Returns(Task.CompletedTask);
        blacklistMock.Setup(x => x.IsBlacklistedAsync(It.IsAny<string>()))
                     .ReturnsAsync(false);
        return new AuthService(db, CreateConfig(), refreshTokenRepo, auditService, blacklistMock.Object);
    }

    private async Task SeedRolesAsync(AppDbContext db)
    {
        await db.Roles.AddRangeAsync(
            new Role { Id = 1, Name = "SuperAdmin" },
            new Role { Id = 2, Name = "BrokerageHouse" },
            new Role { Id = 3, Name = "Admin" },
            new Role { Id = 4, Name = "CCD" },
            new Role { Id = 5, Name = "ITSupport" },
            new Role { Id = 6, Name = "Trader" },
            new Role { Id = 7, Name = "Investor" }
        );
        await db.SaveChangesAsync();
    }

    // ── REGISTER ──────────────────────────────────────────────

    [Fact]
    public async Task RegisterBrokerage_CreatesUserAndBrokerage()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        var result = await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName      = "Test Securities",
            LicenseNumber = "LIC-001",
            FirmEmail     = "firm@test.com",
            FullName      = "Test Owner",
            Email         = "owner@test.com",
            Password      = "Password@123"
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.Equal("Test Owner", result.FullName);
        Assert.Equal("BrokerageHouse", result.Role);
        Assert.Equal(1, await db.Users.CountAsync());
        Assert.Equal(1, await db.BrokerageHouses.CountAsync());
    }

    [Fact]
    public async Task RegisterBrokerage_DuplicateEmail_ReturnsNull()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities", LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",  FullName = "Test Owner",
            Email = "owner@test.com",     Password = "Password@123"
        });

        var result = await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Another Sec",    LicenseNumber = "LIC-002",
            FirmEmail = "another@test.com", FullName = "Another Owner",
            Email = "owner@test.com",    Password = "Password@123"
        });

        Assert.Null(result);
    }

    // ── LOGIN ─────────────────────────────────────────────────

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities", LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",  FullName = "Test Owner",
            Email = "owner@test.com",     Password = "Password@123"
        });

        var result = await svc.LoginAsync(
            new LoginDto { Email = "owner@test.com", Password = "Password@123" },
            "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.Token);
        Assert.Equal("owner@test.com", result.Value.Email);
        Assert.Equal("BrokerageHouse", result.Value.Role);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities", LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",  FullName = "Test Owner",
            Email = "owner@test.com",     Password = "Password@123"
        });

        var result = await svc.LoginAsync(
            new LoginDto { Email = "owner@test.com", Password = "WrongPassword!" },
            "127.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
    }

    [Fact]
    public async Task Login_NonExistentEmail_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        var result = await svc.LoginAsync(
            new LoginDto { Email = "nobody@test.com", Password = "Password@123" },
            "127.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_CREDENTIALS", result.ErrorCode);
    }

    [Fact]
    public async Task Login_RecordsLoginHistory_OnSuccess()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities", LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",  FullName = "Test Owner",
            Email = "owner@test.com",     Password = "Password@123"
        });

        await svc.LoginAsync(
            new LoginDto { Email = "owner@test.com", Password = "Password@123" },
            "192.168.1.1");

        var history = await db.LoginHistories.FirstOrDefaultAsync();
        Assert.NotNull(history);
        Assert.True(history.IsSuccess);
        Assert.Equal("192.168.1.1", history.IpAddress);
    }

    [Fact]
    public async Task Login_RecordsLoginHistory_OnFailure()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities", LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",  FullName = "Test Owner",
            Email = "owner@test.com",     Password = "Password@123"
        });

        await svc.LoginAsync(
            new LoginDto { Email = "owner@test.com", Password = "BadPass!" },
            "10.0.0.1");

        var history = await db.LoginHistories.FirstOrDefaultAsync();
        Assert.NotNull(history);
        Assert.False(history.IsSuccess);
    }

    [Fact]
    public async Task Login_IssuesRefreshToken_OnSuccess()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities", LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",  FullName = "Test Owner",
            Email = "owner@test.com",     Password = "Password@123"
        });

        var result = await svc.LoginAsync(
            new LoginDto { Email = "owner@test.com", Password = "Password@123" },
            "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.NotEmpty(result.Value!.RefreshToken);
        Assert.Equal(1, await db.RefreshTokens.CountAsync());
    }

    // ── LOCKOUT ───────────────────────────────────────────────

    [Fact]
    public async Task Login_5FailedAttempts_LocksAccount()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities", LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",  FullName = "Test Owner",
            Email = "owner@test.com",     Password = "Password@123"
        });

        for (int i = 0; i < 5; i++)
            await svc.LoginAsync(
                new LoginDto { Email = "owner@test.com", Password = "BadPass!" },
                "127.0.0.1");

        var user = await db.Users.FirstAsync();
        Assert.True(user.IsLocked);
        Assert.NotNull(user.LockoutUntil);
    }

    [Fact]
    public async Task Login_LockedAccount_ReturnsLockedError()
    {
        var db  = CreateDb();
        await SeedRolesAsync(db);
        var svc = CreateService(db);

        await svc.RegisterBrokerageAsync(new RegisterBrokerageDto
        {
            FirmName = "Test Securities", LicenseNumber = "LIC-001",
            FirmEmail = "firm@test.com",  FullName = "Test Owner",
            Email = "owner@test.com",     Password = "Password@123"
        });

        // Lock the account manually
        var user         = await db.Users.FirstAsync();
        user.IsLocked    = true;
        user.LockoutUntil = DateTime.UtcNow.AddMinutes(30);
        await db.SaveChangesAsync();

        var result = await svc.LoginAsync(
            new LoginDto { Email = "owner@test.com", Password = "Password@123" },
            "127.0.0.1");

        Assert.False(result.IsSuccess);
        Assert.Equal("ACCOUNT_LOCKED", result.ErrorCode);
    }
}
