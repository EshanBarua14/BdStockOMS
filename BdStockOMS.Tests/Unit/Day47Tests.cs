using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit
{
    // ============================================================
    //  UserPermission Service Tests
    // ============================================================
    public class UserPermissionServiceTests
    {
        private AppDbContext CreateDb()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(opts);
        }

        private UserPermissionService CreateService(AppDbContext db) =>
            new UserPermissionService(db);

        [Fact]
        public async Task GrantPermission_NewPermission_IsGranted()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            var perm = await svc.GrantPermissionAsync(1, "orders.approve", "Orders", 99);
            Assert.True(perm.IsGranted);
            Assert.Equal("orders.approve", perm.Permission);
        }

        [Fact]
        public async Task HasPermission_AfterGrant_ReturnsTrue()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            await svc.GrantPermissionAsync(1, "kyc.view", "KYC", 99);
            var result = await svc.HasPermissionAsync(1, "kyc.view");
            Assert.True(result);
        }

        [Fact]
        public async Task HasPermission_NotGranted_ReturnsFalse()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            var result = await svc.HasPermissionAsync(1, "kyc.view");
            Assert.False(result);
        }

        [Fact]
        public async Task RevokePermission_AfterGrant_ReturnsFalse()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            await svc.GrantPermissionAsync(1, "orders.approve", "Orders", 99);
            await svc.RevokePermissionAsync(1, "orders.approve");
            var result = await svc.HasPermissionAsync(1, "orders.approve");
            Assert.False(result);
        }

        [Fact]
        public async Task RevokePermission_NonExistent_ReturnsFalse()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            var result = await svc.RevokePermissionAsync(1, "nonexistent.perm");
            Assert.False(result);
        }

        [Fact]
        public async Task GrantPermission_Duplicate_Upserts()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            await svc.GrantPermissionAsync(1, "orders.approve", "Orders", 99);
            await svc.RevokePermissionAsync(1, "orders.approve");
            await svc.GrantPermissionAsync(1, "orders.approve", "Orders", 99);
            var result = await svc.HasPermissionAsync(1, "orders.approve");
            Assert.True(result);
        }

        [Fact]
        public async Task GetUserPermissions_ReturnsOnlyActive()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            await svc.GrantPermissionAsync(1, "orders.view",   "Orders", 99);
            await svc.GrantPermissionAsync(1, "orders.approve","Orders", 99);
            await svc.GrantPermissionAsync(1, "kyc.view",      "KYC",    99);
            var perms = await svc.GetUserPermissionsAsync(1);
            Assert.Equal(3, ((List<UserPermission>)perms).Count);
        }

        [Fact]
        public async Task GetModulePermissions_FiltersCorrectly()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            await svc.GrantPermissionAsync(1, "orders.view",   "Orders", 99);
            await svc.GrantPermissionAsync(1, "orders.approve","Orders", 99);
            await svc.GrantPermissionAsync(1, "kyc.view",      "KYC",    99);
            var orderPerms = await svc.GetModulePermissionsAsync(1, "Orders");
            Assert.Equal(2, ((List<UserPermission>)orderPerms).Count);
        }

        [Fact]
        public async Task GrantPermission_WithExpiry_ExpiredIsInactive()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            await svc.GrantPermissionAsync(1, "temp.perm", "Temp", 99,
                expiresAt: DateTime.UtcNow.AddSeconds(-1));
            var result = await svc.HasPermissionAsync(1, "temp.perm");
            Assert.False(result);
        }

        [Fact]
        public async Task GrantPermission_WithFutureExpiry_IsActive()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            await svc.GrantPermissionAsync(1, "temp.perm", "Temp", 99,
                expiresAt: DateTime.UtcNow.AddHours(1));
            var result = await svc.HasPermissionAsync(1, "temp.perm");
            Assert.True(result);
        }

        [Fact]
        public async Task GetUserPermissions_DifferentUsers_Isolated()
        {
            using var db = CreateDb();
            var svc = CreateService(db);
            await svc.GrantPermissionAsync(1, "orders.view", "Orders", 99);
            await svc.GrantPermissionAsync(2, "kyc.view",    "KYC",    99);
            var user1Perms = await svc.GetUserPermissionsAsync(1);
            Assert.Single((List<UserPermission>)user1Perms);
        }
    }

    // ============================================================
    //  SessionPolicy Tests
    // ============================================================
    public class SessionPolicyTests
    {
        private AppDbContext CreateDb()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(opts);
        }

        [Fact]
        public void SessionPolicy_SuperAdmin_MfaRequired()
        {
            var policy = new SessionPolicy
            {
                RoleName    = "SuperAdmin",
                MfaRequired = true,
                MaxConcurrentSessions = 1,
                SingleSessionOnly = true
            };
            Assert.True(policy.MfaRequired);
            Assert.Equal(1, policy.MaxConcurrentSessions);
            Assert.True(policy.SingleSessionOnly);
        }

        [Fact]
        public void SessionPolicy_Admin_MfaRequired()
        {
            var policy = new SessionPolicy
            {
                RoleName    = "Admin",
                MfaRequired = true,
                MaxConcurrentSessions = 1,
                SingleSessionOnly = true
            };
            Assert.True(policy.MfaRequired);
        }

        [Fact]
        public void SessionPolicy_Investor_MfaNotRequired()
        {
            var policy = new SessionPolicy
            {
                RoleName    = "Investor",
                MfaRequired = false,
                MaxConcurrentSessions = 5
            };
            Assert.False(policy.MfaRequired);
            Assert.Equal(5, policy.MaxConcurrentSessions);
        }

        [Fact]
        public void SessionPolicy_InactivityTimeout_DefaultsCorrect()
        {
            var policy = new SessionPolicy
            {
                RoleName                 = "Trader",
                InactivityTimeoutMinutes = 60
            };
            Assert.Equal(60, policy.InactivityTimeoutMinutes);
        }

        [Fact]
        public void SessionPolicy_ZeroMaxSessions_MeansUnlimited()
        {
            var policy = new SessionPolicy
            {
                RoleName              = "Investor",
                MaxConcurrentSessions = 0
            };
            Assert.Equal(0, policy.MaxConcurrentSessions);
        }

        [Fact]
        public async Task CreateSession_StoresCorrectly()
        {
            using var db = CreateDb();
            var session = new UserSession
            {
                UserId       = 1,
                SessionToken = Guid.NewGuid().ToString("N"),
                IpAddress    = "127.0.0.1",
                CreatedAt    = DateTime.UtcNow,
                LastSeenAt   = DateTime.UtcNow,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(30),
                IsRevoked    = false
            };
            db.UserSessions.Add(session);
            await db.SaveChangesAsync();

            var stored = await db.UserSessions.FindAsync(session.Id);
            Assert.NotNull(stored);
            Assert.Equal(1, stored!.UserId);
            Assert.False(stored.IsRevoked);
        }

        [Fact]
        public async Task Session_IsActive_WhenNotExpiredNotRevoked()
        {
            using var db = CreateDb();
            var session = new UserSession
            {
                UserId       = 1,
                SessionToken = Guid.NewGuid().ToString("N"),
                IpAddress    = "127.0.0.1",
                CreatedAt    = DateTime.UtcNow,
                LastSeenAt   = DateTime.UtcNow,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(30),
                IsRevoked    = false
            };
            db.UserSessions.Add(session);
            await db.SaveChangesAsync();
            Assert.True(session.IsActive);
        }

        [Fact]
        public async Task Session_IsInactive_WhenRevoked()
        {
            using var db = CreateDb();
            var session = new UserSession
            {
                UserId       = 1,
                SessionToken = Guid.NewGuid().ToString("N"),
                IpAddress    = "127.0.0.1",
                CreatedAt    = DateTime.UtcNow,
                LastSeenAt   = DateTime.UtcNow,
                ExpiresAt    = DateTime.UtcNow.AddMinutes(30),
                IsRevoked    = true
            };
            db.UserSessions.Add(session);
            await db.SaveChangesAsync();
            Assert.False(session.IsActive);
        }

        [Fact]
        public async Task Session_IsInactive_WhenExpired()
        {
            using var db = CreateDb();
            var session = new UserSession
            {
                UserId       = 1,
                SessionToken = Guid.NewGuid().ToString("N"),
                IpAddress    = "127.0.0.1",
                CreatedAt    = DateTime.UtcNow.AddHours(-2),
                LastSeenAt   = DateTime.UtcNow.AddHours(-2),
                ExpiresAt    = DateTime.UtcNow.AddMinutes(-1),
                IsRevoked    = false
            };
            db.UserSessions.Add(session);
            await db.SaveChangesAsync();
            Assert.False(session.IsActive);
        }

        [Fact]
        public async Task MultipleSessions_OnlyActiveOnesCount()
        {
            using var db = CreateDb();
            db.UserSessions.AddRange(
                new UserSession { UserId = 1, SessionToken = "a", IpAddress = "1.1.1.1", ExpiresAt = DateTime.UtcNow.AddMinutes(30), IsRevoked = false, LastSeenAt = DateTime.UtcNow },
                new UserSession { UserId = 1, SessionToken = "b", IpAddress = "1.1.1.2", ExpiresAt = DateTime.UtcNow.AddMinutes(30), IsRevoked = true,  LastSeenAt = DateTime.UtcNow },
                new UserSession { UserId = 1, SessionToken = "c", IpAddress = "1.1.1.3", ExpiresAt = DateTime.UtcNow.AddMinutes(-1), IsRevoked = false, LastSeenAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var active = await db.UserSessions
                .Where(s => s.UserId == 1 && !s.IsRevoked && s.ExpiresAt > DateTime.UtcNow)
                .CountAsync();
            Assert.Equal(1, active);
        }
    }

    // ============================================================
    //  MFA Model Tests
    // ============================================================
    public class MfaModelTests
    {
        private AppDbContext CreateDb()
        {
            var opts = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(opts);
        }

        [Fact]
        public async Task TwoFactorOtp_Store_AndRetrieve()
        {
            using var db = CreateDb();
            var otp = new TwoFactorOtp
            {
                UserId    = 1,
                OtpCode   = "123456",
                Purpose   = "LOGIN",
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed    = false
            };
            db.TwoFactorOtps.Add(otp);
            await db.SaveChangesAsync();

            var stored = await db.TwoFactorOtps.FindAsync(otp.Id);
            Assert.NotNull(stored);
            Assert.Equal("123456", stored!.OtpCode);
            Assert.False(stored.IsUsed);
        }

        [Fact]
        public async Task TwoFactorOtp_MarkUsed_IsUsedTrue()
        {
            using var db = CreateDb();
            var otp = new TwoFactorOtp
            {
                UserId    = 1,
                OtpCode   = "654321",
                Purpose   = "LOGIN",
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed    = false
            };
            db.TwoFactorOtps.Add(otp);
            await db.SaveChangesAsync();

            otp.IsUsed = true;
            await db.SaveChangesAsync();

            var stored = await db.TwoFactorOtps.FindAsync(otp.Id);
            Assert.True(stored!.IsUsed);
        }

        [Fact]
        public async Task TwoFactorOtp_Expired_ShouldNotBeValid()
        {
            using var db = CreateDb();
            var otp = new TwoFactorOtp
            {
                UserId    = 1,
                OtpCode   = "000000",
                Purpose   = "LOGIN",
                ExpiresAt = DateTime.UtcNow.AddMinutes(-1),
                IsUsed    = false
            };
            db.TwoFactorOtps.Add(otp);
            await db.SaveChangesAsync();

            var valid = await db.TwoFactorOtps
                .AnyAsync(o => o.UserId == 1 && o.OtpCode == "000000"
                            && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);
            Assert.False(valid);
        }

        [Fact]
        public async Task TwoFactorOtp_Valid_ExistsInDb()
        {
            using var db = CreateDb();
            var otp = new TwoFactorOtp
            {
                UserId    = 1,
                OtpCode   = "999999",
                Purpose   = "LOGIN",
                ExpiresAt = DateTime.UtcNow.AddMinutes(5),
                IsUsed    = false
            };
            db.TwoFactorOtps.Add(otp);
            await db.SaveChangesAsync();

            var valid = await db.TwoFactorOtps
                .AnyAsync(o => o.UserId == 1 && o.OtpCode == "999999"
                            && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);
            Assert.True(valid);
        }

        [Fact]
        public void UserPermission_IsActive_WhenGrantedNoExpiry()
        {
            var perm = new UserPermission
            {
                UserId    = 1,
                Permission = "orders.view",
                IsGranted  = true,
                ExpiresAt  = null
            };
            Assert.True(perm.IsActive);
        }

        [Fact]
        public void UserPermission_IsInactive_WhenRevoked()
        {
            var perm = new UserPermission
            {
                UserId    = 1,
                Permission = "orders.view",
                IsGranted  = false,
                ExpiresAt  = null
            };
            Assert.False(perm.IsActive);
        }

        [Fact]
        public void UserPermission_IsInactive_WhenExpired()
        {
            var perm = new UserPermission
            {
                UserId    = 1,
                Permission = "orders.view",
                IsGranted  = true,
                ExpiresAt  = DateTime.UtcNow.AddMinutes(-1)
            };
            Assert.False(perm.IsActive);
        }

        [Fact]
        public void SessionPolicy_Defaults_AreReasonable()
        {
            var policy = new SessionPolicy();
            Assert.Equal(1, policy.MaxConcurrentSessions);
            Assert.Equal(30, policy.InactivityTimeoutMinutes);
            Assert.False(policy.MfaRequired);
            Assert.False(policy.SingleSessionOnly);
        }
    }
}
