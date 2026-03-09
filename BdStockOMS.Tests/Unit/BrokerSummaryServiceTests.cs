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
    public class BrokerSummaryServiceTests : IDisposable
    {
        private readonly AppDbContext _db;
        private readonly BrokerSummaryService _svc;
        private readonly DateTime _today = new DateTime(2026, 1, 15);

        public BrokerSummaryServiceTests()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _db = new AppDbContext(options);
            _svc = new BrokerSummaryService(_db);
            SeedData();
        }

        private void SeedData()
        {
            _db.Roles.AddRange(
                new Role { Id = 1, Name = "Investor" },
                new Role { Id = 2, Name = "Trader" }
            );
            _db.BrokerageHouses.AddRange(
                new BrokerageHouse { Id = 1, Name = "BH One", LicenseNumber = "LIC001", Email = "bh1@test.com", Phone = "01700000001", Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow },
                new BrokerageHouse { Id = 2, Name = "BH Two", LicenseNumber = "LIC002", Email = "bh2@test.com", Phone = "01700000002", Address = "Dhaka", IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            _db.Users.AddRange(
                new User { Id = 1, FullName = "Investor One",   Email = "inv1@test.com", PasswordHash = "h", RoleId = 1, BrokerageHouseId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Id = 2, FullName = "Investor Two",   Email = "inv2@test.com", PasswordHash = "h", RoleId = 1, BrokerageHouseId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Id = 3, FullName = "Trader One",     Email = "tr1@test.com",  PasswordHash = "h", RoleId = 2, BrokerageHouseId = 1, IsActive = true, CreatedAt = DateTime.UtcNow },
                new User { Id = 4, FullName = "Investor Three", Email = "inv3@test.com", PasswordHash = "h", RoleId = 1, BrokerageHouseId = 2, IsActive = true, CreatedAt = DateTime.UtcNow }
            );
            _db.Stocks.Add(new Stock { Id = 1, TradingCode = "GP", CompanyName = "Grameenphone", Exchange = "DSE", LastTradePrice = 400m, CircuitBreakerHigh = 440m, CircuitBreakerLow = 360m, IsActive = true, LastUpdatedAt = DateTime.UtcNow });
            _db.Orders.AddRange(
                new Order { Id = 1, InvestorId = 1, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Buy,  Quantity = 10, PriceAtOrder = 400m, Status = OrderStatus.Executed, CreatedAt = _today },
                new Order { Id = 2, InvestorId = 2, StockId = 1, BrokerageHouseId = 1, OrderType = OrderType.Sell, Quantity = 5,  PriceAtOrder = 400m, Status = OrderStatus.Pending,  CreatedAt = _today }
            );
            _db.Trades.AddRange(
                new Trade { Id = 1, OrderId = 1, StockId = 1, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy",  Quantity = 10, Price = 400m, TotalValue = 4000m, TradedAt = _today },
                new Trade { Id = 2, OrderId = 2, StockId = 1, InvestorId = 2, BrokerageHouseId = 1, Side = "Sell", Quantity = 5,  Price = 400m, TotalValue = 2000m, TradedAt = _today }
            );
            _db.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        // ── Broker Summary Tests ──────────────────────────

        [Fact]
        public async Task GetBrokerSummary_ValidBroker_ReturnsDto()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.NotNull(result);
            Assert.Equal(1, result.BrokerageHouseId);
            Assert.Equal("BH One", result.BrokerName);
        }

        [Fact]
        public async Task GetBrokerSummary_CountsInvestors()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.Equal(2, result.TotalInvestors);
        }

        [Fact]
        public async Task GetBrokerSummary_CountsTraders()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.Equal(1, result.TotalTraders);
        }

        [Fact]
        public async Task GetBrokerSummary_CountsOrdersToday()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.Equal(2, result.TotalOrdersToday);
        }

        [Fact]
        public async Task GetBrokerSummary_CalculatesBuyValue()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.Equal(4000m, result.TotalBuyValueToday);
        }

        [Fact]
        public async Task GetBrokerSummary_CalculatesSellValue()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.Equal(2000m, result.TotalSellValueToday);
        }

        [Fact]
        public async Task GetBrokerSummary_CalculatesTotalTurnover()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.Equal(6000m, result.TotalTurnoverToday);
        }

        [Fact]
        public async Task GetBrokerSummary_CountsActiveOrders()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.Equal(1, result.ActiveOrdersCount);
        }

        [Fact]
        public async Task GetBrokerSummary_NotFound_ThrowsKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.GetBrokerSummaryAsync(999, _today));
        }

        [Fact]
        public async Task GetBrokerSummary_DifferentDate_ReturnsZeroOrders()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today.AddDays(-1));
            Assert.Equal(0, result.TotalOrdersToday);
        }

        [Fact]
        public async Task GetAllBrokerSummaries_ReturnsAllActiveBrokers()
        {
            var result = await _svc.GetAllBrokerSummariesAsync(_today);
            Assert.Equal(2, result.Count());
        }

        [Fact]
        public async Task GetAllBrokerSummaries_BrokerIsolation()
        {
            var result = (await _svc.GetAllBrokerSummariesAsync(_today)).ToList();
            var bh2 = result.First(r => r.BrokerageHouseId == 2);
            Assert.Equal(0, bh2.TotalOrdersToday);
        }

        // ── Top Traders Tests ─────────────────────────────

        [Fact]
        public async Task GetTopTradersByValue_ReturnsTraders()
        {
            var result = await _svc.GetTopTradersByValueAsync(1, _today);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetTopTradersByBuy_ReturnsTraders()
        {
            var result = await _svc.GetTopTradersByBuyAsync(1, _today);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetTopTradersBySell_ReturnsTraders()
        {
            var result = await _svc.GetTopTradersBySellAsync(1, _today);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetTopTradersByValue_RespectsTopLimit()
        {
            var result = await _svc.GetTopTradersByValueAsync(1, _today, 1);
            Assert.Single(result);
        }

        // ── Client Activity Tests ─────────────────────────

        [Fact]
        public async Task GetClientActivity_ValidTrader_ReturnsClients()
        {
            var result = await _svc.GetClientActivityAsync(3, _today);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task GetClientActivity_TraderNotFound_ThrowsKeyNotFound()
        {
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _svc.GetClientActivityAsync(999, _today));
        }

        [Fact]
        public async Task GetClientActivity_ShowsKycStatus()
        {
            var result = await _svc.GetClientActivityAsync(3, _today);
            Assert.All(result, c => Assert.False(c.IsKycApproved));
        }

        [Fact]
        public async Task GetTopClientsByValue_ReturnsOrderedClients()
        {
            var result = (await _svc.GetTopClientsByValueAsync(1, _today)).ToList();
            Assert.NotEmpty(result);
            if (result.Count > 1)
                Assert.True(result[0].TotalValueToday >= result[1].TotalValueToday);
        }

        [Fact]
        public async Task GetTopClientsByValue_RespectsTopLimit()
        {
            var result = await _svc.GetTopClientsByValueAsync(1, _today, 1);
            Assert.Single(result);
        }

        [Fact]
        public async Task GetBrokerSummary_PendingKycCount_IsZeroWithNoKyc()
        {
            var result = await _svc.GetBrokerSummaryAsync(1, _today);
            Assert.Equal(0, result.PendingKycCount);
        }
    }
}
