using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;

namespace BdStockOMS.Tests.Unit
{
    public class AuditComplianceServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly AuditComplianceService _sut;

        public AuditComplianceServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _sut = new AuditComplianceService(_db);
        }

        private AuditLog MakeLog(int userId, string action, string entityType = "Order",
            int? entityId = null, string? ip = null, DateTime? createdAt = null)
        {
            return new AuditLog
            {
                UserId = userId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                IpAddress = ip,
                CreatedAt = createdAt ?? DateTime.UtcNow
            };
        }

        // --- GetLogsAsync filter tests ---

        [Fact]
        public async Task GetLogsAsync_NoFilter_ReturnsAllLogs()
        {
            _db.AuditLogs.AddRange(MakeLog(1, "OrderPlaced"), MakeLog(2, "OrderCancelled"), MakeLog(3, "LoginFailed"));
            await _db.SaveChangesAsync();

            var result = await _sut.GetLogsAsync(new AuditLogFilter { PageSize = 20 });

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task GetLogsAsync_FilterByUserId_ReturnsOnlyThatUser()
        {
            _db.AuditLogs.AddRange(MakeLog(1, "OrderPlaced"), MakeLog(2, "OrderPlaced"), MakeLog(1, "LoginFailed"));
            await _db.SaveChangesAsync();

            var result = await _sut.GetLogsAsync(new AuditLogFilter { UserId = 1, PageSize = 20 });

            Assert.Equal(2, result.Count());
            Assert.All(result, l => Assert.Equal(1, l.UserId));
        }

        [Fact]
        public async Task GetLogsAsync_FilterByAction_ReturnsMatchingLogs()
        {
            _db.AuditLogs.AddRange(MakeLog(1, "OrderPlaced"), MakeLog(2, "LoginFailed"), MakeLog(3, "OrderPlaced"));
            await _db.SaveChangesAsync();

            var result = await _sut.GetLogsAsync(new AuditLogFilter { Action = "OrderPlaced", PageSize = 20 });

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetLogsAsync_FilterByEntityType_ReturnsMatchingLogs()
        {
            _db.AuditLogs.AddRange(
                MakeLog(1, "Created", "Order"),
                MakeLog(2, "Created", "User"),
                MakeLog(3, "Created", "Order"));
            await _db.SaveChangesAsync();

            var result = await _sut.GetLogsAsync(new AuditLogFilter { EntityType = "Order", PageSize = 20 });

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetLogsAsync_FilterByIpAddress_ReturnsMatchingLogs()
        {
            _db.AuditLogs.AddRange(
                MakeLog(1, "Login", ip: "192.168.1.1"),
                MakeLog(2, "Login", ip: "10.0.0.1"),
                MakeLog(3, "Login", ip: "192.168.1.1"));
            await _db.SaveChangesAsync();

            var result = await _sut.GetLogsAsync(new AuditLogFilter { IpAddress = "192.168.1.1", PageSize = 20 });

            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetLogsAsync_FilterByDateRange_ReturnsLogsInRange()
        {
            var now = DateTime.UtcNow;
            _db.AuditLogs.AddRange(
                MakeLog(1, "A", createdAt: now.AddDays(-3)),
                MakeLog(2, "B", createdAt: now.AddDays(-1)),
                MakeLog(3, "C", createdAt: now.AddDays(-5)));
            await _db.SaveChangesAsync();

            var result = await _sut.GetLogsAsync(new AuditLogFilter
            {
                From = now.AddDays(-2),
                To = now,
                PageSize = 20
            });

            Assert.Single(result);
            Assert.Equal(2, result.First().UserId);
        }

        [Fact]
        public async Task GetLogsAsync_Pagination_ReturnsCorrectPage()
        {
            for (int i = 0; i < 25; i++)
                _db.AuditLogs.Add(MakeLog(i + 1, "Action"));
            await _db.SaveChangesAsync();

            var page1 = await _sut.GetLogsAsync(new AuditLogFilter { Page = 1, PageSize = 10 });
            var page2 = await _sut.GetLogsAsync(new AuditLogFilter { Page = 2, PageSize = 10 });
            var page3 = await _sut.GetLogsAsync(new AuditLogFilter { Page = 3, PageSize = 10 });

            Assert.Equal(10, page1.Count());
            Assert.Equal(10, page2.Count());
            Assert.Equal(5, page3.Count());
        }

        [Fact]
        public async Task GetLogsAsync_OrderedByCreatedAtDescending()
        {
            var now = DateTime.UtcNow;
            _db.AuditLogs.AddRange(
                MakeLog(1, "A", createdAt: now.AddMinutes(-10)),
                MakeLog(2, "B", createdAt: now.AddMinutes(-5)),
                MakeLog(3, "C", createdAt: now));
            await _db.SaveChangesAsync();

            var result = (await _sut.GetLogsAsync(new AuditLogFilter { PageSize = 20 })).ToList();

            Assert.Equal(3, result[0].UserId);
            Assert.Equal(2, result[1].UserId);
            Assert.Equal(1, result[2].UserId);
        }

        [Fact]
        public async Task GetLogsAsync_ActionContainsSearch_WorksPartialMatch()
        {
            _db.AuditLogs.AddRange(MakeLog(1, "OrderPlaced"), MakeLog(2, "OrderCancelled"), MakeLog(3, "LoginFailed"));
            await _db.SaveChangesAsync();

            var result = await _sut.GetLogsAsync(new AuditLogFilter { Action = "Order", PageSize = 20 });

            Assert.Equal(2, result.Count());
        }

        // --- CountLogsAsync tests ---

        [Fact]
        public async Task CountLogsAsync_NoFilter_ReturnsTotal()
        {
            _db.AuditLogs.AddRange(MakeLog(1, "A"), MakeLog(2, "B"), MakeLog(3, "C"));
            await _db.SaveChangesAsync();

            var count = await _sut.CountLogsAsync(new AuditLogFilter());

            Assert.Equal(3, count);
        }

        [Fact]
        public async Task CountLogsAsync_WithFilter_ReturnsFilteredCount()
        {
            _db.AuditLogs.AddRange(MakeLog(1, "LoginFailed"), MakeLog(1, "LoginFailed"), MakeLog(2, "OrderPlaced"));
            await _db.SaveChangesAsync();

            var count = await _sut.CountLogsAsync(new AuditLogFilter { Action = "LoginFailed" });

            Assert.Equal(2, count);
        }

        // --- ExportCsvAsync tests ---

        [Fact]
        public async Task ExportCsvAsync_ReturnsValidCsv()
        {
            _db.AuditLogs.AddRange(MakeLog(1, "OrderPlaced", "Order", 42, "127.0.0.1"));
            await _db.SaveChangesAsync();

            var csv = await _sut.ExportCsvAsync(new AuditLogFilter());

            Assert.Contains("Id,UserId,Action,EntityType,EntityId,IpAddress,CreatedAt", csv);
            Assert.Contains("OrderPlaced", csv);
            Assert.Contains("127.0.0.1", csv);
        }

        [Fact]
        public async Task ExportCsvAsync_EmptyDb_ReturnsHeaderOnly()
        {
            var csv = await _sut.ExportCsvAsync(new AuditLogFilter());

            var lines = csv.Trim().Split('\n');
            Assert.Single(lines);
            Assert.Contains("Id,UserId", lines[0]);
        }

        [Fact]
        public async Task ExportCsvAsync_MultipleRows_CorrectLineCount()
        {
            for (int i = 0; i < 5; i++)
                _db.AuditLogs.Add(MakeLog(i + 1, "Action"));
            await _db.SaveChangesAsync();

            var csv = await _sut.ExportCsvAsync(new AuditLogFilter());

            var lines = csv.Trim().Split('\n').Where(l => l.Length > 0).ToList();
            Assert.Equal(6, lines.Count); // header + 5 rows
        }

        [Fact]
        public async Task ExportCsvAsync_RespectsFilter()
        {
            _db.AuditLogs.AddRange(MakeLog(1, "LoginFailed"), MakeLog(2, "OrderPlaced"), MakeLog(3, "LoginFailed"));
            await _db.SaveChangesAsync();

            var csv = await _sut.ExportCsvAsync(new AuditLogFilter { Action = "LoginFailed" });

            var lines = csv.Trim().Split('\n').Where(l => l.Length > 0).ToList();
            Assert.Equal(3, lines.Count); // header + 2 rows
        }

        // --- DetectSuspiciousActivityAsync tests ---

        [Fact]
        public async Task DetectSuspicious_FailedLoginSpike_Detected()
        {
            var now = DateTime.UtcNow;
            for (int i = 0; i < 5; i++)
                _db.AuditLogs.Add(MakeLog(1, "LoginFailed", createdAt: now.AddMinutes(-5)));
            await _db.SaveChangesAsync();

            var results = await _sut.DetectSuspiciousActivityAsync();

            Assert.Contains(results, r => r.UserId == 1 && r.Reason.Contains("Failed login"));
        }

        [Fact]
        public async Task DetectSuspicious_FailedLoginBelowThreshold_NotDetected()
        {
            var now = DateTime.UtcNow;
            for (int i = 0; i < 4; i++)
                _db.AuditLogs.Add(MakeLog(1, "LoginFailed", createdAt: now.AddMinutes(-5)));
            await _db.SaveChangesAsync();

            var results = await _sut.DetectSuspiciousActivityAsync();

            Assert.DoesNotContain(results, r => r.UserId == 1 && r.Reason.Contains("Failed login"));
        }

        [Fact]
        public async Task DetectSuspicious_FailedLoginOutsideWindow_NotDetected()
        {
            for (int i = 0; i < 5; i++)
                _db.AuditLogs.Add(MakeLog(1, "LoginFailed", createdAt: DateTime.UtcNow.AddHours(-2)));
            await _db.SaveChangesAsync();

            var results = await _sut.DetectSuspiciousActivityAsync();

            Assert.DoesNotContain(results, r => r.UserId == 1 && r.Reason.Contains("Failed login"));
        }

        [Fact]
        public async Task DetectSuspicious_RapidCancellations_Detected()
        {
            var now = DateTime.UtcNow;
            for (int i = 0; i < 3; i++)
                _db.AuditLogs.Add(MakeLog(2, "OrderCancelled", createdAt: now.AddMinutes(-10)));
            await _db.SaveChangesAsync();

            var results = await _sut.DetectSuspiciousActivityAsync();

            Assert.Contains(results, r => r.UserId == 2 && r.Reason.Contains("cancellation"));
        }

        [Fact]
        public async Task DetectSuspicious_RapidCancellationsBelowThreshold_NotDetected()
        {
            var now = DateTime.UtcNow;
            for (int i = 0; i < 2; i++)
                _db.AuditLogs.Add(MakeLog(2, "OrderCancelled", createdAt: now.AddMinutes(-10)));
            await _db.SaveChangesAsync();

            var results = await _sut.DetectSuspiciousActivityAsync();

            Assert.DoesNotContain(results, r => r.UserId == 2 && r.Reason.Contains("cancellation"));
        }

        [Fact]
        public async Task DetectSuspicious_LargeOrder_Detected()
        {
            var role = new Role { Id = 1, Name = "Investor" };
            var bh = new BrokerageHouse { Id = 1, Name = "Test BH", LicenseNumber = "LIC001", IsActive = true };
            _db.Roles.Add(role);
            _db.BrokerageHouses.Add(bh);
            var user = new User { Id = 1, FullName = "T", Email = "t@t.com", PasswordHash = "h", RoleId = 1, BrokerageHouseId = 1, IsActive = true };
            _db.Users.Add(user);
            var stock = new Stock { Id = 1, TradingCode = "TEST", CompanyName = "Test Co", Exchange = "DSE", IsActive = true };
            _db.Stocks.Add(stock);
            _db.Orders.Add(new Order
            {
                InvestorId = 1,
                StockId = 1,
                BrokerageHouseId = 1,
                Quantity = 10000,
                PriceAtOrder = 200m,
                OrderType = OrderType.Buy,
                Status = OrderStatus.Pending
            });
            await _db.SaveChangesAsync();

            var results = await _sut.DetectSuspiciousActivityAsync();

            Assert.Contains(results, r => r.UserId == 1 && r.Reason.Contains("Large order"));
        }

        [Fact]
        public async Task DetectSuspicious_NormalOrder_NotDetected()
        {
            var role = new Role { Id = 1, Name = "Investor" };
            var bh = new BrokerageHouse { Id = 1, Name = "Test BH", LicenseNumber = "LIC001", IsActive = true };
            _db.Roles.Add(role);
            _db.BrokerageHouses.Add(bh);
            var user = new User { Id = 1, FullName = "T", Email = "t@t.com", PasswordHash = "h", RoleId = 1, BrokerageHouseId = 1, IsActive = true };
            _db.Users.Add(user);
            var stock = new Stock { Id = 1, TradingCode = "TEST", CompanyName = "Test Co", Exchange = "DSE", IsActive = true };
            _db.Stocks.Add(stock);
            _db.Orders.Add(new Order
            {
                InvestorId = 1,
                StockId = 1,
                BrokerageHouseId = 1,
                Quantity = 10,
                PriceAtOrder = 50m,
                OrderType = OrderType.Buy,
                Status = OrderStatus.Pending
            });
            await _db.SaveChangesAsync();

            var results = await _sut.DetectSuspiciousActivityAsync();

            Assert.DoesNotContain(results, r => r.Reason.Contains("Large order"));
        }

        [Fact]
        public async Task DetectSuspicious_NoActivity_ReturnsEmpty()
        {
            var results = await _sut.DetectSuspiciousActivityAsync();
            Assert.Empty(results);
        }

        [Fact]
        public async Task DetectSuspicious_MultipleUsers_EachDetectedSeparately()
        {
            var now = DateTime.UtcNow;
            for (int i = 0; i < 5; i++)
                _db.AuditLogs.Add(MakeLog(1, "LoginFailed", createdAt: now.AddMinutes(-5)));
            for (int i = 0; i < 5; i++)
                _db.AuditLogs.Add(MakeLog(2, "LoginFailed", createdAt: now.AddMinutes(-5)));
            await _db.SaveChangesAsync();

            var results = await _sut.DetectSuspiciousActivityAsync();

            Assert.Contains(results, r => r.UserId == 1);
            Assert.Contains(results, r => r.UserId == 2);
        }

        [Fact]
        public async Task GetLogsAsync_CombinedFilters_WorkCorrectly()
        {
            var now = DateTime.UtcNow;
            _db.AuditLogs.AddRange(
                MakeLog(1, "LoginFailed", ip: "1.1.1.1", createdAt: now.AddMinutes(-5)),
                MakeLog(1, "LoginFailed", ip: "2.2.2.2", createdAt: now.AddMinutes(-5)),
                MakeLog(2, "LoginFailed", ip: "1.1.1.1", createdAt: now.AddMinutes(-5)));
            await _db.SaveChangesAsync();

            var result = await _sut.GetLogsAsync(new AuditLogFilter
            {
                UserId = 1,
                IpAddress = "1.1.1.1",
                PageSize = 20
            });

            Assert.Single(result);
        }

        public void Dispose() => _db.Dispose();
    }
}
