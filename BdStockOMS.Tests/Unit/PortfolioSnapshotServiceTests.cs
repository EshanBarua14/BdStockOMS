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
    public class PortfolioSnapshotServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly PortfolioSnapshotService _svc;

        public PortfolioSnapshotServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _svc = new PortfolioSnapshotService(_db);
            SeedData();
        }

        private void SeedData()
        {
            _db.Roles.Add(new Role { Id = 1, Name = "Investor" });
            _db.BrokerageHouses.Add(new BrokerageHouse
            {
                Id = 1, Name = "Test BH", LicenseNumber = "LIC001",
                Email = "bh@test.com", Phone = "01700000000",
                Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow
            });
            _db.Users.AddRange(
                new User { Id = 1, FullName = "Investor One", Email = "inv1@test.com", PasswordHash = "hash", RoleId = 1, BrokerageHouseId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Id = 2, FullName = "Investor Two", Email = "inv2@test.com", PasswordHash = "hash", RoleId = 1, BrokerageHouseId = 1, IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            _db.Stocks.AddRange(
                new Stock { Id = 1, TradingCode = "GP",   CompanyName = "Grameenphone", Exchange = "DSE", LastTradePrice = 400m, CircuitBreakerHigh = 440m, CircuitBreakerLow = 360m, IsActive = true, LastUpdatedAt = DateTime.UtcNow },
                new Stock { Id = 2, TradingCode = "BRAC", CompanyName = "BRAC Bank",    Exchange = "DSE", LastTradePrice = 50m,  CircuitBreakerHigh = 55m,  CircuitBreakerLow = 45m,  IsActive = true, LastUpdatedAt = DateTime.UtcNow }
            );
            _db.Portfolios.AddRange(
                new Portfolio { Id = 1, InvestorId = 1, StockId = 1, BrokerageHouseId = 1, Quantity = 10, AverageBuyPrice = 380m, LastUpdatedAt = DateTime.UtcNow },
                new Portfolio { Id = 2, InvestorId = 1, StockId = 2, BrokerageHouseId = 1, Quantity = 20, AverageBuyPrice = 45m,  LastUpdatedAt = DateTime.UtcNow },
                new Portfolio { Id = 3, InvestorId = 2, StockId = 1, BrokerageHouseId = 1, Quantity = 5,  AverageBuyPrice = 390m, LastUpdatedAt = DateTime.UtcNow }
            );
            _db.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        // ── Snapshot Tests ────────────────────────────────

        [Fact]
        public async Task CaptureSnapshot_ValidUser_ReturnsSnapshot()
        {
            var result = await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
        }

        [Fact]
        public async Task CaptureSnapshot_CalculatesTotalInvested()
        {
            // User 1: 10 * 380 + 20 * 45 = 3800 + 900 = 4700
            var result = await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            Assert.Equal(4700m, result.TotalInvested);
        }

        [Fact]
        public async Task CaptureSnapshot_CalculatesCurrentValue()
        {
            // User 1: 10 * 400 + 20 * 50 = 4000 + 1000 = 5000
            var result = await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            Assert.Equal(5000m, result.CurrentValue);
        }

        [Fact]
        public async Task CaptureSnapshot_CalculatesUnrealizedPnL()
        {
            // 5000 - 4700 = 300
            var result = await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            Assert.Equal(300m, result.UnrealizedPnL);
        }

        [Fact]
        public async Task CaptureSnapshot_CalculatesRoiPercent()
        {
            var result = await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            Assert.True(result.RoiPercent != 0);
        }

        [Fact]
        public async Task CaptureSnapshot_SetsCorrectHoldingsCount()
        {
            var result = await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            Assert.Equal(2, result.TotalHoldings);
        }

        [Fact]
        public async Task CaptureSnapshot_UserNotFound_ThrowsKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.CaptureSnapshotAsync(999, DateTime.UtcNow));
        }

        [Fact]
        public async Task CaptureSnapshot_SameDateTwice_ReplacesExisting()
        {
            var date = new DateTime(2026, 1, 1);
            await _svc.CaptureSnapshotAsync(1, date);
            await _svc.CaptureSnapshotAsync(1, date);
            var history = await _svc.GetSnapshotHistoryAsync(1, date.AddDays(-1), date.AddDays(1));
            Assert.Single(history);
        }

        [Fact]
        public async Task CaptureSnapshot_SavedToDatabase()
        {
            var result = await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            var fromDb = await _db.PortfolioSnapshots.FindAsync(result.Id);
            Assert.NotNull(fromDb);
        }

        [Fact]
        public async Task CaptureSnapshot_SetsBrokerageHouseId()
        {
            var result = await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            Assert.Equal(1, result.BrokerageHouseId);
        }

        // ── History Tests ─────────────────────────────────

        [Fact]
        public async Task GetSnapshotHistory_ReturnsInDateRange()
        {
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 1));
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 2));
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 3));

            var history = await _svc.GetSnapshotHistoryAsync(1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
            Assert.Equal(2, history.Count());
        }

        [Fact]
        public async Task GetSnapshotHistory_EmptyForNoSnapshots_ReturnsEmpty()
        {
            var history = await _svc.GetSnapshotHistoryAsync(1, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
            Assert.Empty(history);
        }

        [Fact]
        public async Task GetSnapshotHistory_OrderedByDate()
        {
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 3));
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 1));
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 2));

            var history = (await _svc.GetSnapshotHistoryAsync(1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 3))).ToList();
            Assert.True(history[0].SnapshotDate < history[1].SnapshotDate);
        }

        [Fact]
        public async Task GetLatestSnapshot_ReturnsNewest()
        {
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 1));
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 5));
            await _svc.CaptureSnapshotAsync(1, new DateTime(2026, 1, 3));

            var latest = await _svc.GetLatestSnapshotAsync(1);
            Assert.Equal(new DateTime(2026, 1, 5), latest!.SnapshotDate);
        }

        [Fact]
        public async Task GetLatestSnapshot_NoSnapshots_ReturnsNull()
        {
            var result = await _svc.GetLatestSnapshotAsync(99);
            Assert.Null(result);
        }

        // ── ROI Tests ─────────────────────────────────────

        [Fact]
        public async Task CalculateRoi_NoSnapshots_ReturnsZero()
        {
            var roi = await _svc.CalculateRoiAsync(99);
            Assert.Equal(0m, roi);
        }

        [Fact]
        public async Task CalculateRoi_WithSnapshot_ReturnsRoiPercent()
        {
            await _svc.CaptureSnapshotAsync(1, DateTime.UtcNow);
            var roi = await _svc.CalculateRoiAsync(1);
            Assert.NotEqual(0m, roi);
        }

        // ── StockAnalytics Tests ──────────────────────────

        [Fact]
        public async Task UpsertStockAnalytics_NewRecord_CreatesAnalytics()
        {
            var data = new StockAnalyticsResult
            {
                StockId = 1, Exchange = "DSE",
                Vwap = 395m, High52W = 450m, Low52W = 320m,
                Beta = 1.2m, AvgVolume30D = 50000m
            };

            var result = await _svc.UpsertStockAnalyticsAsync(data);
            Assert.NotNull(result);
            Assert.Equal(395m, result.Vwap);
        }

        [Fact]
        public async Task UpsertStockAnalytics_ExistingRecord_Updates()
        {
            var data = new StockAnalyticsResult { StockId = 1, Exchange = "DSE", Vwap = 395m, High52W = 450m, Low52W = 320m, Beta = 1.2m, AvgVolume30D = 50000m };
            await _svc.UpsertStockAnalyticsAsync(data);

            data.Vwap = 410m;
            var result = await _svc.UpsertStockAnalyticsAsync(data);
            Assert.Equal(410m, result.Vwap);
        }

        [Fact]
        public async Task UpsertStockAnalytics_NoDuplicates()
        {
            var data = new StockAnalyticsResult { StockId = 1, Exchange = "DSE", Vwap = 395m, High52W = 450m, Low52W = 320m, Beta = 1.2m, AvgVolume30D = 50000m };
            await _svc.UpsertStockAnalyticsAsync(data);
            await _svc.UpsertStockAnalyticsAsync(data);

            var count = await _db.StockAnalytics.CountAsync(a => a.StockId == 1 && a.Exchange == "DSE");
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetStockAnalytics_ExistingRecord_ReturnsAnalytics()
        {
            var data = new StockAnalyticsResult { StockId = 1, Exchange = "DSE", Vwap = 395m, High52W = 450m, Low52W = 320m, Beta = 1.2m, AvgVolume30D = 50000m };
            await _svc.UpsertStockAnalyticsAsync(data);

            var result = await _svc.GetStockAnalyticsAsync(1, "DSE");
            Assert.NotNull(result);
            Assert.Equal(1, result!.StockId);
        }

        [Fact]
        public async Task GetStockAnalytics_NonExistent_ReturnsNull()
        {
            var result = await _svc.GetStockAnalyticsAsync(999, "DSE");
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAnalytics_ReturnsForExchange()
        {
            await _svc.UpsertStockAnalyticsAsync(new StockAnalyticsResult { StockId = 1, Exchange = "DSE", Vwap = 395m, High52W = 450m, Low52W = 320m, Beta = 1.2m, AvgVolume30D = 50000m });
            await _svc.UpsertStockAnalyticsAsync(new StockAnalyticsResult { StockId = 2, Exchange = "DSE", Vwap = 48m,  High52W = 60m,  Low52W = 35m,  Beta = 0.9m, AvgVolume30D = 30000m });

            var all = await _svc.GetAllAnalyticsAsync("DSE");
            Assert.Equal(2, all.Count());
        }

        [Fact]
        public async Task CaptureAllSnapshots_ReturnsCountOfUsersWithHoldings()
        {
            var count = await _svc.CaptureAllSnapshotsAsync(DateTime.UtcNow);
            Assert.Equal(2, count);
        }
    }
}
