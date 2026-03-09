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
    public class BrokerageSettingsServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly BrokerageSettingsService _svc;

        public BrokerageSettingsServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _svc = new BrokerageSettingsService(_db);
            SeedData();
        }

        private void SeedData()
        {
            _db.BrokerageHouses.AddRange(
                new BrokerageHouse { Id = 1, Name = "BH One", LicenseNumber = "LIC001", Email = "bh1@test.com", Phone = "01700000001", Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow },
                new BrokerageHouse { Id = 2, Name = "BH Two", LicenseNumber = "LIC002", Email = "bh2@test.com", Phone = "01700000002", Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            _db.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private UpdateSettingsRequest DefaultUpdateRequest() => new UpdateSettingsRequest
        {
            MaxSingleOrderValue     = 500_000m,
            MaxDailyTurnover        = 5_000_000m,
            MarginRatio             = 1.5m,
            MinCashBalance          = 1000m,
            IsMarginTradingEnabled  = true,
            IsShortSellingEnabled   = false,
            IsSmsAlertEnabled       = true,
            IsEmailAlertEnabled     = true,
            IsAutoSettlementEnabled = true,
            IsTwoFactorRequired     = false,
            TradingStartMinutes     = 570,
            TradingEndMinutes       = 870
        };

        private CreateBranchRequest DefaultBranchRequest(int bhId = 1) => new CreateBranchRequest
        {
            BrokerageHouseId = bhId,
            Name             = "Main Branch",
            BranchCode       = "MB001",
            Address          = "Motijheel, Dhaka",
            Phone            = "01700000001",
            Email            = "branch@test.com",
            ManagerName      = "John Doe"
        };

        // ── Settings Tests ────────────────────────────────

        [Fact]
        public async Task GetOrCreateSettings_NewBroker_CreatesWithDefaults()
        {
            var settings = await _svc.GetOrCreateSettingsAsync(1);
            Assert.NotNull(settings);
            Assert.Equal(1, settings.BrokerageHouseId);
        }

        [Fact]
        public async Task GetOrCreateSettings_ExistingBroker_ReturnsExisting()
        {
            await _svc.GetOrCreateSettingsAsync(1);
            await _svc.GetOrCreateSettingsAsync(1);
            var count = await _db.BrokerageSettings.CountAsync(s => s.BrokerageHouseId == 1);
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task GetOrCreateSettings_DefaultMaxSingleOrderValue()
        {
            var settings = await _svc.GetOrCreateSettingsAsync(1);
            Assert.Equal(1_000_000m, settings.MaxSingleOrderValue);
        }

        [Fact]
        public async Task GetOrCreateSettings_DefaultTradingStartMinutes()
        {
            var settings = await _svc.GetOrCreateSettingsAsync(1);
            Assert.Equal(570, settings.TradingStartMinutes);
        }

        [Fact]
        public async Task UpdateSettings_UpdatesAllFields()
        {
            var updated = await _svc.UpdateSettingsAsync(1, DefaultUpdateRequest());
            Assert.Equal(500_000m, updated.MaxSingleOrderValue);
            Assert.Equal(5_000_000m, updated.MaxDailyTurnover);
            Assert.True(updated.IsMarginTradingEnabled);
        }

        [Fact]
        public async Task UpdateSettings_PersistsToDatabase()
        {
            await _svc.UpdateSettingsAsync(1, DefaultUpdateRequest());
            var fromDb = await _db.BrokerageSettings.FirstAsync(s => s.BrokerageHouseId == 1);
            Assert.Equal(500_000m, fromDb.MaxSingleOrderValue);
        }

        [Fact]
        public async Task UpdateSettings_UpdatesTimestamp()
        {
            var before = DateTime.UtcNow.AddSeconds(-1);
            var updated = await _svc.UpdateSettingsAsync(1, DefaultUpdateRequest());
            Assert.True(updated.UpdatedAt >= before);
        }

        [Fact]
        public async Task UpdateSettings_DifferentBrokers_Independent()
        {
            var req1 = DefaultUpdateRequest();
            req1.MaxSingleOrderValue = 100_000m;
            var req2 = DefaultUpdateRequest();
            req2.MaxSingleOrderValue = 200_000m;

            await _svc.UpdateSettingsAsync(1, req1);
            await _svc.UpdateSettingsAsync(2, req2);

            var s1 = await _svc.GetOrCreateSettingsAsync(1);
            var s2 = await _svc.GetOrCreateSettingsAsync(2);
            Assert.Equal(100_000m, s1.MaxSingleOrderValue);
            Assert.Equal(200_000m, s2.MaxSingleOrderValue);
        }

        // ── Feature Toggle Tests ──────────────────────────

        [Fact]
        public async Task IsFeatureEnabled_MarginTrading_ReturnsCorrectValue()
        {
            var req = DefaultUpdateRequest();
            req.IsMarginTradingEnabled = true;
            await _svc.UpdateSettingsAsync(1, req);
            var result = await _svc.IsFeatureEnabledAsync(1, "MarginTrading");
            Assert.True(result);
        }

        [Fact]
        public async Task IsFeatureEnabled_ShortSelling_DefaultFalse()
        {
            await _svc.GetOrCreateSettingsAsync(1);
            var result = await _svc.IsFeatureEnabledAsync(1, "ShortSelling");
            Assert.False(result);
        }

        [Fact]
        public async Task IsFeatureEnabled_SmsAlert_DefaultTrue()
        {
            await _svc.GetOrCreateSettingsAsync(1);
            var result = await _svc.IsFeatureEnabledAsync(1, "SmsAlert");
            Assert.True(result);
        }

        [Fact]
        public async Task IsFeatureEnabled_UnknownFeature_ReturnsFalse()
        {
            await _svc.GetOrCreateSettingsAsync(1);
            var result = await _svc.IsFeatureEnabledAsync(1, "NonExistentFeature");
            Assert.False(result);
        }

        [Fact]
        public async Task IsFeatureEnabled_TwoFactor_CanBeEnabled()
        {
            var req = DefaultUpdateRequest();
            req.IsTwoFactorRequired = true;
            await _svc.UpdateSettingsAsync(1, req);
            var result = await _svc.IsFeatureEnabledAsync(1, "TwoFactor");
            Assert.True(result);
        }

        // ── Branch Tests ──────────────────────────────────

        [Fact]
        public async Task CreateBranch_ValidRequest_ReturnsBranch()
        {
            var branch = await _svc.CreateBranchAsync(DefaultBranchRequest());
            Assert.NotNull(branch);
            Assert.Equal("MB001", branch.BranchCode);
        }

        [Fact]
        public async Task CreateBranch_IsActiveByDefault()
        {
            var branch = await _svc.CreateBranchAsync(DefaultBranchRequest());
            Assert.True(branch.IsActive);
        }

        [Fact]
        public async Task CreateBranch_DuplicateCode_ThrowsInvalidOperation()
        {
            await _svc.CreateBranchAsync(DefaultBranchRequest());
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _svc.CreateBranchAsync(DefaultBranchRequest()));
        }

        [Fact]
        public async Task CreateBranch_DifferentBrokers_SameCodeAllowed()
        {
            var b1 = await _svc.CreateBranchAsync(DefaultBranchRequest(1));
            var b2 = await _svc.CreateBranchAsync(DefaultBranchRequest(2));
            Assert.NotEqual(b1.Id, b2.Id);
        }

        [Fact]
        public async Task UpdateBranch_UpdatesFields()
        {
            var branch = await _svc.CreateBranchAsync(DefaultBranchRequest());
            var req = DefaultBranchRequest();
            req.Name = "Updated Branch";
            req.ManagerName = "Jane Doe";
            var updated = await _svc.UpdateBranchAsync(branch.Id, req);
            Assert.Equal("Updated Branch", updated.Name);
            Assert.Equal("Jane Doe", updated.ManagerName);
        }

        [Fact]
        public async Task UpdateBranch_NotFound_ThrowsKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.UpdateBranchAsync(9999, DefaultBranchRequest()));
        }

        [Fact]
        public async Task DeactivateBranch_SetsIsActiveFalse()
        {
            var branch = await _svc.CreateBranchAsync(DefaultBranchRequest());
            var result = await _svc.DeactivateBranchAsync(branch.Id);
            Assert.True(result);
            var fromDb = await _db.BranchOffices.FindAsync(branch.Id);
            Assert.False(fromDb!.IsActive);
        }

        [Fact]
        public async Task DeactivateBranch_NonExistent_ReturnsFalse()
        {
            var result = await _svc.DeactivateBranchAsync(9999);
            Assert.False(result);
        }

        [Fact]
        public async Task GetBranches_ReturnsAllForBroker()
        {
            var req1 = DefaultBranchRequest(); req1.BranchCode = "B001";
            var req2 = DefaultBranchRequest(); req2.BranchCode = "B002";
            await _svc.CreateBranchAsync(req1);
            await _svc.CreateBranchAsync(req2);
            var branches = await _svc.GetBranchesAsync(1);
            Assert.Equal(2, branches.Count());
        }

        [Fact]
        public async Task GetBranches_EmptyForNoBranches_ReturnsEmpty()
        {
            var branches = await _svc.GetBranchesAsync(99);
            Assert.Empty(branches);
        }

        [Fact]
        public async Task GetBranchById_ExistingId_ReturnsBranch()
        {
            var branch = await _svc.CreateBranchAsync(DefaultBranchRequest());
            var result = await _svc.GetBranchByIdAsync(branch.Id);
            Assert.NotNull(result);
            Assert.Equal(branch.Id, result!.Id);
        }

        [Fact]
        public async Task GetBranchById_NonExistent_ReturnsNull()
        {
            var result = await _svc.GetBranchByIdAsync(9999);
            Assert.Null(result);
        }
    }
}
