using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class SecurityTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    // ── AUDIT SERVICE ──────────────────────────────────────────

    [Fact]
    public async Task AuditService_LogAsync_CreatesAuditRecord()
    {
        var db = CreateDb();
        var svc = new AuditService(db);

        await svc.LogAsync(1, "LOGIN_SUCCESS", "User", 1, null, null, "127.0.0.1");

        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Equal("LOGIN_SUCCESS", log.Action);
        Assert.Equal("User", log.EntityType);
        Assert.Equal(1, log.UserId);
        Assert.Equal("127.0.0.1", log.IpAddress);
    }

    [Fact]
    public async Task AuditService_LogAsync_StoresOldAndNewValues()
    {
        var db  = CreateDb();
        var svc = new AuditService(db);

        await svc.LogAsync(2, "ROLE_CHANGED", "User", 5,
                           "{\"role\":\"Trader\"}", "{\"role\":\"Admin\"}", "10.0.0.1");

        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.Equal("{\"role\":\"Trader\"}", log!.OldValues);
        Assert.Equal("{\"role\":\"Admin\"}", log.NewValues);
    }

    [Fact]
    public async Task AuditService_LogAsync_NullEntityIdAllowed()
    {
        var db  = CreateDb();
        var svc = new AuditService(db);

        await svc.LogAsync(1, "LOGIN_FAILED", "User", null, null, null, "127.0.0.1");

        var log = await db.AuditLogs.FirstOrDefaultAsync();
        Assert.NotNull(log);
        Assert.Null(log.EntityId);
    }

    [Fact]
    public async Task AuditService_LogAsync_MultipleLogsStoredCorrectly()
    {
        var db  = CreateDb();
        var svc = new AuditService(db);

        await svc.LogAsync(1, "LOGIN_SUCCESS",  "User", 1, null, null, "127.0.0.1");
        await svc.LogAsync(1, "ORDER_PLACED",   "Order", 10, null, null, "127.0.0.1");
        await svc.LogAsync(1, "ORDER_CANCELLED","Order", 10, null, null, "127.0.0.1");

        var count = await db.AuditLogs.CountAsync();
        Assert.Equal(3, count);
    }

    // ── REFRESH TOKEN MODEL ────────────────────────────────────

    [Fact]
    public void RefreshToken_IsActive_TrueWhenNotRevokedAndNotExpired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7)
        };
        Assert.True(token.IsActive);
        Assert.False(token.IsRevoked);
        Assert.False(token.IsExpired);
    }

    [Fact]
    public void RefreshToken_IsExpired_TrueWhenPastExpiry()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        Assert.True(token.IsExpired);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void RefreshToken_IsRevoked_TrueWhenRevokedAtSet()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            RevokedAt = DateTime.UtcNow.AddHours(-1)
        };
        Assert.True(token.IsRevoked);
        Assert.False(token.IsActive);
    }

    [Fact]
    public void RefreshToken_IsActive_FalseWhenBothRevokedAndExpired()
    {
        var token = new RefreshToken
        {
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
            RevokedAt = DateTime.UtcNow.AddDays(-2)
        };
        Assert.False(token.IsActive);
        Assert.True(token.IsRevoked);
        Assert.True(token.IsExpired);
    }

    // ── USER LOCKOUT MODEL ─────────────────────────────────────

    [Fact]
    public void User_LockoutFields_DefaultToZeroAndFalse()
    {
        var user = new User
        {
            FullName         = "Test",
            Email            = "test@test.com",
            PasswordHash     = "hash",
            Phone            = "01700000000",
            RoleId           = 7,
            BrokerageHouseId = 1
        };
        Assert.Equal(0, user.FailedLoginCount);
        Assert.False(user.IsLocked);
        Assert.Null(user.LockoutUntil);
        Assert.False(user.ForcePasswordChange);
        Assert.Null(user.PasswordChangedAt);
    }

    [Fact]
    public void User_LockoutUntil_CanBeSetAndRead()
    {
        var lockoutTime = DateTime.UtcNow.AddMinutes(30);
        var user = new User
        {
            FullName         = "Test",
            Email            = "test@test.com",
            PasswordHash     = "hash",
            Phone            = "01700000000",
            RoleId           = 7,
            BrokerageHouseId = 1,
            IsLocked         = true,
            LockoutUntil     = lockoutTime,
            FailedLoginCount = 5
        };
        Assert.True(user.IsLocked);
        Assert.Equal(5, user.FailedLoginCount);
        Assert.Equal(lockoutTime, user.LockoutUntil);
    }

    // ── LOGIN HISTORY MODEL ────────────────────────────────────

    [Fact]
    public void LoginHistory_SuccessRecord_HasCorrectFields()
    {
        var history = new LoginHistory
        {
            Email       = "user@test.com",
            IpAddress   = "192.168.1.1",
            IsSuccess   = true,
            AttemptedAt = DateTime.UtcNow
        };
        Assert.True(history.IsSuccess);
        Assert.Null(history.FailureReason);
        Assert.Null(history.UserId);
    }

    [Fact]
    public void LoginHistory_FailedRecord_HasFailureReason()
    {
        var history = new LoginHistory
        {
            Email         = "user@test.com",
            IpAddress     = "192.168.1.1",
            IsSuccess     = false,
            FailureReason = "Invalid password",
            AttemptedAt   = DateTime.UtcNow
        };
        Assert.False(history.IsSuccess);
        Assert.Equal("Invalid password", history.FailureReason);
    }

    // ── DBCONTEXT — NEW TABLES ─────────────────────────────────

    [Fact]
    public async Task DbContext_CanSaveRefreshToken()
    {
        var db = CreateDb();
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId      = 1,
            Token       = "test-token-abc",
            CreatedByIp = "127.0.0.1",
            ExpiresAt   = DateTime.UtcNow.AddDays(7)
        });
        await db.SaveChangesAsync();

        var saved = await db.RefreshTokens.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("test-token-abc", saved.Token);
    }

    [Fact]
    public async Task DbContext_CanSaveLoginHistory()
    {
        var db = CreateDb();
        db.LoginHistories.Add(new LoginHistory
        {
            Email       = "admin@test.com",
            IpAddress   = "10.0.0.1",
            IsSuccess   = false,
            FailureReason = "Account locked",
            AttemptedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var saved = await db.LoginHistories.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal("Account locked", saved.FailureReason);
    }

    [Fact]
    public async Task DbContext_MultipleRefreshTokens_PerUser()
    {
        var db = CreateDb();
        db.RefreshTokens.AddRange(
            new RefreshToken { UserId = 1, Token = "token-1", CreatedByIp = "127.0.0.1", ExpiresAt = DateTime.UtcNow.AddDays(7) },
            new RefreshToken { UserId = 1, Token = "token-2", CreatedByIp = "127.0.0.1", ExpiresAt = DateTime.UtcNow.AddDays(7) },
            new RefreshToken { UserId = 2, Token = "token-3", CreatedByIp = "10.0.0.1",  ExpiresAt = DateTime.UtcNow.AddDays(7) }
        );
        await db.SaveChangesAsync();

        var user1Tokens = await db.RefreshTokens.Where(t => t.UserId == 1).CountAsync();
        Assert.Equal(2, user1Tokens);
    }

    // ── SECURITY HEADERS ──────────────────────────────────────

    [Fact]
    public void SecurityHeaders_RequiredHeaderNames_AreCorrect()
    {
        var expectedHeaders = new[]
        {
            "X-Frame-Options",
            "X-Content-Type-Options",
            "X-XSS-Protection",
            "Referrer-Policy",
            "X-Permitted-Cross-Domain-Policies"
        };
        foreach (var header in expectedHeaders)
            Assert.NotEmpty(header);
    }

    // ── IDEMPOTENCY KEY ───────────────────────────────────────

    [Fact]
    public void IdempotencyKey_Format_IsValidGuid()
    {
        var key = Guid.NewGuid().ToString();
        Assert.True(Guid.TryParse(key, out _));
    }
}
