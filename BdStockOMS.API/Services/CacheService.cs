using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace BdStockOMS.API.Services
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key) where T : class;
        Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class;
        Task RemoveAsync(string key);
        Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class;
    }

    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;

        public CacheService(IDistributedCache cache)
        {
            _cache = cache;
        }

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var bytes = await _cache.GetAsync(key);
                if (bytes == null || bytes.Length == 0) return null;
                return JsonSerializer.Deserialize<T>(bytes);
            }
            catch
            {
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan ttl) where T : class
        {
            try
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(value);
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = ttl
                };
                await _cache.SetAsync(key, bytes, options);
            }
            catch
            {
                // Cache failures must never break the main flow
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch { }
        }

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan ttl) where T : class
        {
            var cached = await GetAsync<T>(key);
            if (cached != null) return cached;

            var value = await factory();
            await SetAsync(key, value, ttl);
            return value;
        }
    }

    // TTL constants for each data type
    public static class CacheTtl
    {
        public static readonly TimeSpan MarketData       = TimeSpan.FromSeconds(5);   // price changes every 5s
        public static readonly TimeSpan CommissionRates  = TimeSpan.FromHours(1);     // changes rarely
        public static readonly TimeSpan PortfolioSummary = TimeSpan.FromMinutes(1);   // changes on trade
        public static readonly TimeSpan StockList        = TimeSpan.FromMinutes(10);  // changes rarely
        public static readonly TimeSpan BrokerageSettings= TimeSpan.FromMinutes(30);  // changes rarely
        public static readonly TimeSpan UserSession      = TimeSpan.FromMinutes(15);  // matches JWT lifetime
    }

    // Cache key factory
    public static class CacheKeys
    {
        public static string MarketData(string tradingCode)       => $"market:{tradingCode}";
        public static string AllMarketData()                       => "market:all";
        public static string CommissionRates(int brokerageId)     => $"commission:{brokerageId}";
        public static string PortfolioSummary(int investorId)     => $"portfolio:summary:{investorId}";
        public static string StockList()                           => "stocks:all";
        public static string BrokerageSettings(int brokerageId)   => $"brokerage:settings:{brokerageId}";
        public static string UserSession(int userId)               => $"session:{userId}";
    }
}
