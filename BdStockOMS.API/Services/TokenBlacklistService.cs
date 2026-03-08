using StackExchange.Redis;

namespace BdStockOMS.API.Services;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string jti, TimeSpan ttl);
    Task BlacklistAllUserTokensAsync(int userId);
    Task<bool> IsBlacklistedAsync(string jti);
    Task<bool> IsUserBlacklistedAsync(int userId);
}

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public TokenBlacklistService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db    = redis.GetDatabase();
    }

    public async Task BlacklistTokenAsync(string jti, TimeSpan ttl)
    {
        await _db.StringSetAsync($"blacklist:token:{jti}", "1", ttl);
    }

    public async Task BlacklistAllUserTokensAsync(int userId)
    {
        // Set a user-level blacklist flag — all tokens issued before this time are invalid
        await _db.StringSetAsync(
            $"blacklist:user:{userId}",
            DateTime.UtcNow.Ticks.ToString(),
            TimeSpan.FromDays(7));
    }

    public async Task<bool> IsBlacklistedAsync(string jti)
    {
        return await _db.KeyExistsAsync($"blacklist:token:{jti}");
    }

    public async Task<bool> IsUserBlacklistedAsync(int userId)
    {
        return await _db.KeyExistsAsync($"blacklist:user:{userId}");
    }
}
