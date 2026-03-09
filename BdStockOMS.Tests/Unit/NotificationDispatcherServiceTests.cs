using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;

namespace BdStockOMS.Tests.Unit
{
    public class NotificationDispatcherServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly Mock<IEmailService> _emailMock;
        private readonly Mock<ISmsService> _smsMock;
        private readonly Mock<ILogger<NotificationDispatcherService>> _loggerMock;
        private readonly NotificationDispatcherService _sut;

        public NotificationDispatcherServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _emailMock = new Mock<IEmailService>();
            _smsMock = new Mock<ISmsService>();
            _loggerMock = new Mock<ILogger<NotificationDispatcherService>>();
            _sut = new NotificationDispatcherService(_db, _emailMock.Object, _smsMock.Object, _loggerMock.Object);
        }

        private async Task<User> SeedUserAsync(string email = "test@test.com", string phone = "01700000000")
        {
            var role = new Role { Id = 99, Name = "Investor" };
            _db.Roles.Add(role);
            var user = new User
            {
                Id = 1,
                FullName = "Test User",
                Email = email,
                Phone = phone,
                PasswordHash = "hash",
                RoleId = 99,
                IsActive = true
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        // --- IEmailService tests ---

        [Fact]
        public async Task SimulatedEmailService_SendAsync_LogsAndReturnsTrue()
        {
            var logger = new Mock<ILogger<SimulatedEmailService>>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var svc = new SimulatedEmailService(logger.Object, db);

            var result = await svc.SendAsync("a@b.com", "Subject", "Body");

            Assert.True(result);
            Assert.Equal(1, db.NotificationLogs.Count());
        }

        [Fact]
        public async Task SimulatedEmailService_SendAsync_SavesCorrectChannel()
        {
            var logger = new Mock<ILogger<SimulatedEmailService>>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var svc = new SimulatedEmailService(logger.Object, db);

            await svc.SendAsync("a@b.com", "Subject", "Body");

            var log = db.NotificationLogs.First();
            Assert.Equal(NotificationChannel.Email, log.Channel);
        }

        [Fact]
        public async Task SimulatedEmailService_SendBulkAsync_SendsToAll()
        {
            var logger = new Mock<ILogger<SimulatedEmailService>>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var svc = new SimulatedEmailService(logger.Object, db);

            var result = await svc.SendBulkAsync(new[] { "a@b.com", "c@d.com", "e@f.com" }, "Sub", "Body");

            Assert.True(result);
            Assert.Equal(3, db.NotificationLogs.Count());
        }

        [Fact]
        public async Task SimulatedEmailService_SendAsync_RecipientStoredCorrectly()
        {
            var logger = new Mock<ILogger<SimulatedEmailService>>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var svc = new SimulatedEmailService(logger.Object, db);

            await svc.SendAsync("specific@email.com", "Test", "Body");

            var log = db.NotificationLogs.First();
            Assert.Equal("specific@email.com", log.Recipient);
        }

        // --- ISmsService tests ---

        [Fact]
        public async Task SimulatedSmsService_SendAsync_LogsAndReturnsTrue()
        {
            var logger = new Mock<ILogger<SimulatedSmsService>>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var svc = new SimulatedSmsService(logger.Object, db);

            var result = await svc.SendAsync("01700000000", "Test message");

            Assert.True(result);
            Assert.Equal(1, db.NotificationLogs.Count());
        }

        [Fact]
        public async Task SimulatedSmsService_SendAsync_SavesCorrectChannel()
        {
            var logger = new Mock<ILogger<SimulatedSmsService>>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var svc = new SimulatedSmsService(logger.Object, db);

            await svc.SendAsync("01700000000", "Test message");

            var log = db.NotificationLogs.First();
            Assert.Equal(NotificationChannel.Sms, log.Channel);
        }

        [Fact]
        public async Task SimulatedSmsService_SendBulkAsync_SendsToAll()
        {
            var logger = new Mock<ILogger<SimulatedSmsService>>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var svc = new SimulatedSmsService(logger.Object, db);

            var result = await svc.SendBulkAsync(new[] { "017", "018", "019" }, "msg");

            Assert.True(result);
            Assert.Equal(3, db.NotificationLogs.Count());
        }

        [Fact]
        public async Task SimulatedSmsService_SendAsync_SubjectIsSms()
        {
            var logger = new Mock<ILogger<SimulatedSmsService>>();
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var svc = new SimulatedSmsService(logger.Object, db);

            await svc.SendAsync("01700000000", "Hello");

            var log = db.NotificationLogs.First();
            Assert.Equal("SMS", log.Subject);
        }

        // --- NotificationDispatcherService tests ---

        [Fact]
        public async Task DispatchAsync_UserNotFound_DoesNotSendAnything()
        {
            _emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            await _sut.DispatchAsync(999, NotificationEventType.OrderFilled, "Sub", "Body");

            _emailMock.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DispatchAsync_NoPreference_DefaultsToEmail()
        {
            await SeedUserAsync();
            _emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            await _sut.DispatchAsync(1, NotificationEventType.OrderFilled, "Sub", "Body");

            _emailMock.Verify(e => e.SendAsync("test@test.com", "Sub", "Body"), Times.Once);
        }

        [Fact]
        public async Task DispatchAsync_EmailPreference_SendsEmail()
        {
            await SeedUserAsync();
            await _sut.UpsertPreferenceAsync(1, NotificationEventType.OrderFilled, NotificationChannel.Email, true);
            _emailMock.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(true);

            await _sut.DispatchAsync(1, NotificationEventType.OrderFilled, "Sub", "Body");

            _emailMock.Verify(e => e.SendAsync("test@test.com", "Sub", "Body"), Times.Once);
            _smsMock.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DispatchAsync_SmsPreference_SendsSms()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var smsLogger = new Mock<ILogger<SimulatedSmsService>>();
            var emailLogger = new Mock<ILogger<SimulatedEmailService>>();
            var realSms = new SimulatedSmsService(smsLogger.Object, db);
            var realEmail = new SimulatedEmailService(emailLogger.Object, db);
            var dispLogger = new Mock<ILogger<NotificationDispatcherService>>();
            var sut = new NotificationDispatcherService(db, realEmail, realSms, dispLogger.Object);

            var role = new Role { Id = 1, Name = "Investor" };
            db.Roles.Add(role);
            db.Users.Add(new User { Id = 1, FullName = "T", Email = "t@t.com", Phone = "01700000000", PasswordHash = "h", RoleId = 1, IsActive = true });
            await db.SaveChangesAsync();
            await sut.UpsertPreferenceAsync(1, NotificationEventType.OrderFilled, NotificationChannel.Sms, true);

            await sut.DispatchAsync(1, NotificationEventType.OrderFilled, "Sub", "Body");

            var logs = db.NotificationLogs.ToList();
            Assert.Single(logs);
            Assert.Equal(NotificationChannel.Sms, logs[0].Channel);
        }

        [Fact]
        public async Task DispatchAsync_BothPreference_SendsEmailAndSms()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var smsLogger = new Mock<ILogger<SimulatedSmsService>>();
            var emailLogger = new Mock<ILogger<SimulatedEmailService>>();
            var realSms = new SimulatedSmsService(smsLogger.Object, db);
            var realEmail = new SimulatedEmailService(emailLogger.Object, db);
            var dispLogger = new Mock<ILogger<NotificationDispatcherService>>();
            var sut = new NotificationDispatcherService(db, realEmail, realSms, dispLogger.Object);

            var role = new Role { Id = 1, Name = "Investor" };
            db.Roles.Add(role);
            db.Users.Add(new User { Id = 1, FullName = "T", Email = "t@t.com", Phone = "01700000000", PasswordHash = "h", RoleId = 1, IsActive = true });
            await db.SaveChangesAsync();
            var savedUser = db.Users.First();
            await sut.UpsertPreferenceAsync(savedUser.Id, NotificationEventType.OrderFilled, NotificationChannel.Both, true);

            await sut.DispatchAsync(savedUser.Id, NotificationEventType.OrderFilled, "Sub", "Body");

            var emailLogs = db.NotificationLogs.Where(l => l.Channel == NotificationChannel.Email).ToList();
            var smsLogs = db.NotificationLogs.Where(l => l.Channel == NotificationChannel.Sms).ToList();
            Assert.Single(emailLogs);
            Assert.Single(smsLogs);
        }

        [Fact]
        public async Task DispatchAsync_DisabledPreference_DoesNotSend()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var smsLogger = new Mock<ILogger<SimulatedSmsService>>();
            var emailLogger = new Mock<ILogger<SimulatedEmailService>>();
            var realSms = new SimulatedSmsService(smsLogger.Object, db);
            var realEmail = new SimulatedEmailService(emailLogger.Object, db);
            var dispLogger = new Mock<ILogger<NotificationDispatcherService>>();
            var sut = new NotificationDispatcherService(db, realEmail, realSms, dispLogger.Object);

            var role = new Role { Id = 1, Name = "Investor" };
            db.Roles.Add(role);
            db.Users.Add(new User { Id = 1, FullName = "T", Email = "t@t.com", Phone = "01700000000", PasswordHash = "h", RoleId = 1, IsActive = true });
            await db.SaveChangesAsync();
            await sut.UpsertPreferenceAsync(1, NotificationEventType.OrderFilled, NotificationChannel.Email, false);

            await sut.DispatchAsync(1, NotificationEventType.OrderFilled, "Sub", "Body");

            Assert.Empty(db.NotificationLogs.ToList());
        }

        [Fact]
        public async Task UpsertPreferenceAsync_CreatesNewPreference()
        {
            await SeedUserAsync();

            await _sut.UpsertPreferenceAsync(1, NotificationEventType.PriceBreach, NotificationChannel.Sms, true);

            var prefs = await _db.NotificationPreferences.ToListAsync();
            Assert.Single(prefs);
            Assert.Equal(NotificationChannel.Sms, prefs[0].Channel);
        }

        [Fact]
        public async Task UpsertPreferenceAsync_UpdatesExistingPreference()
        {
            await SeedUserAsync();
            await _sut.UpsertPreferenceAsync(1, NotificationEventType.PriceBreach, NotificationChannel.Sms, true);

            await _sut.UpsertPreferenceAsync(1, NotificationEventType.PriceBreach, NotificationChannel.Email, false);

            var prefs = await _db.NotificationPreferences.ToListAsync();
            Assert.Single(prefs);
            Assert.Equal(NotificationChannel.Email, prefs[0].Channel);
            Assert.False(prefs[0].IsEnabled);
        }

        [Fact]
        public async Task GetPreferencesAsync_ReturnsUserPreferences()
        {
            await SeedUserAsync();
            await _sut.UpsertPreferenceAsync(1, NotificationEventType.PriceBreach, NotificationChannel.Email, true);
            await _sut.UpsertPreferenceAsync(1, NotificationEventType.RmsBreach, NotificationChannel.Sms, true);

            var prefs = await _sut.GetPreferencesAsync(1);

            Assert.Equal(2, prefs.Count());
        }

        [Fact]
        public async Task GetPreferencesAsync_EmptyForNewUser()
        {
            await SeedUserAsync();

            var prefs = await _sut.GetPreferencesAsync(1);

            Assert.Empty(prefs);
        }

        [Fact]
        public async Task GetLogsAsync_ReturnsAllLogs_WhenNoUserFilter()
        {
            _db.NotificationLogs.AddRange(
                new NotificationLog { Recipient = "a@b.com", Subject = "S", Body = "B", Channel = NotificationChannel.Email, IsSuccess = true },
                new NotificationLog { Recipient = "c@d.com", Subject = "S", Body = "B", Channel = NotificationChannel.Email, IsSuccess = true }
            );
            await _db.SaveChangesAsync();

            var logs = await _sut.GetLogsAsync();

            Assert.Equal(2, logs.Count());
        }

        [Fact]
        public async Task GetLogsAsync_FiltersByUserId()
        {
            _db.NotificationLogs.AddRange(
                new NotificationLog { UserId = 1, Recipient = "a@b.com", Subject = "S", Body = "B", Channel = NotificationChannel.Email, IsSuccess = true },
                new NotificationLog { UserId = 2, Recipient = "c@d.com", Subject = "S", Body = "B", Channel = NotificationChannel.Email, IsSuccess = true }
            );
            await _db.SaveChangesAsync();

            var logs = await _sut.GetLogsAsync(userId: 1);

            Assert.Single(logs);
        }

        [Fact]
        public async Task GetLogsAsync_PaginationWorks()
        {
            for (int i = 0; i < 25; i++)
                _db.NotificationLogs.Add(new NotificationLog { Recipient = $"r{i}@b.com", Subject = "S", Body = "B", Channel = NotificationChannel.Email, IsSuccess = true });
            await _db.SaveChangesAsync();

            var page1 = await _sut.GetLogsAsync(page: 1, pageSize: 10);
            var page2 = await _sut.GetLogsAsync(page: 2, pageSize: 10);
            var page3 = await _sut.GetLogsAsync(page: 3, pageSize: 10);

            Assert.Equal(10, page1.Count());
            Assert.Equal(10, page2.Count());
            Assert.Equal(5, page3.Count());
        }

        [Fact]
        public async Task DispatchAsync_NoEmail_SkipsEmailSend()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = new AppDbContext(options);
            var smsLogger = new Mock<ILogger<SimulatedSmsService>>();
            var emailLogger = new Mock<ILogger<SimulatedEmailService>>();
            var realSms = new SimulatedSmsService(smsLogger.Object, db);
            var realEmail = new SimulatedEmailService(emailLogger.Object, db);
            var dispLogger = new Mock<ILogger<NotificationDispatcherService>>();
            var sut = new NotificationDispatcherService(db, realEmail, realSms, dispLogger.Object);

            var role = new Role { Id = 1, Name = "Investor" };
            db.Roles.Add(role);
            db.Users.Add(new User { Id = 1, FullName = "T", Email = "", Phone = "01700000000", PasswordHash = "h", RoleId = 1, IsActive = true });
            await db.SaveChangesAsync();

            await sut.DispatchAsync(1, NotificationEventType.OrderFilled, "Sub", "Body");

            Assert.Empty(db.NotificationLogs.ToList());
        }

        [Fact]
        public async Task DispatchAsync_NoPhone_SkipsSms()
        {
            await SeedUserAsync(phone: "");
            await _sut.UpsertPreferenceAsync(1, NotificationEventType.OrderFilled, NotificationChannel.Sms, true);

            await _sut.DispatchAsync(1, NotificationEventType.OrderFilled, "Sub", "Body");

            _smsMock.Verify(s => s.SendAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task UpsertPreferenceAsync_DifferentEventTypes_CreatesMultiple()
        {
            await SeedUserAsync();

            await _sut.UpsertPreferenceAsync(1, NotificationEventType.PriceBreach, NotificationChannel.Email, true);
            await _sut.UpsertPreferenceAsync(1, NotificationEventType.RmsBreach, NotificationChannel.Sms, true);
            await _sut.UpsertPreferenceAsync(1, NotificationEventType.KycStatusChanged, NotificationChannel.Both, true);

            var prefs = await _db.NotificationPreferences.ToListAsync();
            Assert.Equal(3, prefs.Count());
        }

        public void Dispose() => _db.Dispose();
    }
}
