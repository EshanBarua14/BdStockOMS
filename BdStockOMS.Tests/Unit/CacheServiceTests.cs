using System;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using BdStockOMS.API.Services;
using Xunit;

namespace BdStockOMS.Tests.Unit
{
    public class CacheServiceTests
    {
        private ICacheService CreateService()
        {
            var opts = Options.Create(new MemoryDistributedCacheOptions());
            var cache = new MemoryDistributedCache(opts);
            return new CacheService(cache);
        }

        public class TestDto
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public decimal Value { get; set; }
        }

        // ── GetAsync / SetAsync ───────────────────────────────────────────

        [Fact]
        public async Task SetAsync_ThenGetAsync_ReturnsSameValue()
        {
            var svc = CreateService();
            var dto = new TestDto { Id = 1, Name = "Grameenphone", Value = 350.50m };
            await svc.SetAsync("test:1", dto, TimeSpan.FromMinutes(5));
            var result = await svc.GetAsync<TestDto>("test:1");
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Grameenphone", result.Name);
            Assert.Equal(350.50m, result.Value);
        }

        [Fact]
        public async Task GetAsync_MissingKey_ReturnsNull()
        {
            var svc = CreateService();
            var result = await svc.GetAsync<TestDto>("nonexistent:key");
            Assert.Null(result);
        }

        [Fact]
        public async Task SetAsync_OverwritesExistingKey()
        {
            var svc = CreateService();
            await svc.SetAsync("key:1", new TestDto { Id = 1, Name = "Old" }, TimeSpan.FromMinutes(5));
            await svc.SetAsync("key:1", new TestDto { Id = 1, Name = "New" }, TimeSpan.FromMinutes(5));
            var result = await svc.GetAsync<TestDto>("key:1");
            Assert.Equal("New", result!.Name);
        }

        [Fact]
        public async Task SetAsync_ExpiredTtl_ReturnsNull()
        {
            var svc = CreateService();
            await svc.SetAsync("expire:1", new TestDto { Id = 1, Name = "Temp" }, TimeSpan.FromMilliseconds(50));
            await Task.Delay(200);
            var result = await svc.GetAsync<TestDto>("expire:1");
            Assert.Null(result);
        }

        // ── RemoveAsync ───────────────────────────────────────────────────

        [Fact]
        public async Task RemoveAsync_ExistingKey_ReturnsNullAfter()
        {
            var svc = CreateService();
            await svc.SetAsync("remove:1", new TestDto { Id = 1, Name = "ToRemove" }, TimeSpan.FromMinutes(5));
            await svc.RemoveAsync("remove:1");
            var result = await svc.GetAsync<TestDto>("remove:1");
            Assert.Null(result);
        }

        [Fact]
        public async Task RemoveAsync_NonExistentKey_DoesNotThrow()
        {
            var svc = CreateService();
            var ex = await Record.ExceptionAsync(() => svc.RemoveAsync("ghost:key"));
            Assert.Null(ex);
        }

        // ── GetOrSetAsync ─────────────────────────────────────────────────

        [Fact]
        public async Task GetOrSetAsync_CacheMiss_CallsFactory()
        {
            var svc = CreateService();
            var factoryCalled = false;
            var result = await svc.GetOrSetAsync("miss:1", async () =>
            {
                factoryCalled = true;
                return new TestDto { Id = 99, Name = "FromFactory" };
            }, TimeSpan.FromMinutes(5));
            Assert.True(factoryCalled);
            Assert.Equal(99, result.Id);
        }

        [Fact]
        public async Task GetOrSetAsync_CacheHit_DoesNotCallFactory()
        {
            var svc = CreateService();
            await svc.SetAsync("hit:1", new TestDto { Id = 1, Name = "Cached" }, TimeSpan.FromMinutes(5));
            var factoryCalled = false;
            var result = await svc.GetOrSetAsync("hit:1", async () =>
            {
                factoryCalled = true;
                return new TestDto { Id = 2, Name = "FromFactory" };
            }, TimeSpan.FromMinutes(5));
            Assert.False(factoryCalled);
            Assert.Equal(1, result.Id);
            Assert.Equal("Cached", result.Name);
        }

        [Fact]
        public async Task GetOrSetAsync_SubsequentCall_ReturnsCachedValue()
        {
            var svc = CreateService();
            var callCount = 0;
            await svc.GetOrSetAsync("count:1", async () => { callCount++; return new TestDto { Id = callCount }; }, TimeSpan.FromMinutes(5));
            await svc.GetOrSetAsync("count:1", async () => { callCount++; return new TestDto { Id = callCount }; }, TimeSpan.FromMinutes(5));
            await svc.GetOrSetAsync("count:1", async () => { callCount++; return new TestDto { Id = callCount }; }, TimeSpan.FromMinutes(5));
            Assert.Equal(1, callCount);
        }

        // ── CacheKeys ─────────────────────────────────────────────────────

        [Fact]
        public void CacheKeys_MarketData_ContainsTradingCode()
        {
            var key = CacheKeys.MarketData("GP");
            Assert.Contains("GP", key);
        }

        [Fact]
        public void CacheKeys_CommissionRates_ContainsBrokerageId()
        {
            var key = CacheKeys.CommissionRates(5);
            Assert.Contains("5", key);
        }

        [Fact]
        public void CacheKeys_PortfolioSummary_ContainsInvestorId()
        {
            var key = CacheKeys.PortfolioSummary(42);
            Assert.Contains("42", key);
        }

        [Fact]
        public void CacheKeys_DifferentEntities_ProduceDifferentKeys()
        {
            Assert.NotEqual(CacheKeys.MarketData("GP"), CacheKeys.CommissionRates(1));
            Assert.NotEqual(CacheKeys.PortfolioSummary(1), CacheKeys.UserSession(1));
            Assert.NotEqual(CacheKeys.StockList(), CacheKeys.AllMarketData());
        }

        [Fact]
        public void CacheKeys_SameEntity_SameInput_ProducesSameKey()
        {
            Assert.Equal(CacheKeys.MarketData("BRAC"), CacheKeys.MarketData("BRAC"));
            Assert.Equal(CacheKeys.CommissionRates(3), CacheKeys.CommissionRates(3));
        }

        // ── CacheTtl ──────────────────────────────────────────────────────

        [Fact]
        public void CacheTtl_MarketData_Is5Seconds()
        {
            Assert.Equal(TimeSpan.FromSeconds(5), CacheTtl.MarketData);
        }

        [Fact]
        public void CacheTtl_CommissionRates_Is1Hour()
        {
            Assert.Equal(TimeSpan.FromHours(1), CacheTtl.CommissionRates);
        }

        [Fact]
        public void CacheTtl_PortfolioSummary_Is1Minute()
        {
            Assert.Equal(TimeSpan.FromMinutes(1), CacheTtl.PortfolioSummary);
        }

        [Fact]
        public void CacheTtl_MarketData_ShorterThan_CommissionRates()
        {
            Assert.True(CacheTtl.MarketData < CacheTtl.CommissionRates);
        }

        [Fact]
        public void CacheTtl_UserSession_Matches_JwtLifetime()
        {
            Assert.Equal(TimeSpan.FromMinutes(15), CacheTtl.UserSession);
        }

        // ── Multiple Keys ─────────────────────────────────────────────────

        [Fact]
        public async Task MultipleKeys_IndependentOfEachOther()
        {
            var svc = CreateService();
            await svc.SetAsync("k1", new TestDto { Id = 1, Name = "One" }, TimeSpan.FromMinutes(5));
            await svc.SetAsync("k2", new TestDto { Id = 2, Name = "Two" }, TimeSpan.FromMinutes(5));
            await svc.SetAsync("k3", new TestDto { Id = 3, Name = "Three" }, TimeSpan.FromMinutes(5));

            await svc.RemoveAsync("k2");

            Assert.NotNull(await svc.GetAsync<TestDto>("k1"));
            Assert.Null(await svc.GetAsync<TestDto>("k2"));
            Assert.NotNull(await svc.GetAsync<TestDto>("k3"));
        }
    }
}
