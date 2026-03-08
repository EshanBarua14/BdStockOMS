using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace BdStockOMS.API.Services;

public interface ITwoFactorService
{
    Task<string> GenerateOtpAsync(int userId, string purpose);
    Task<Result> ValidateOtpAsync(int userId, string otpCode, string purpose);
    Task<bool> IsTrustedDeviceAsync(int userId, string deviceToken);
    Task<string> AddTrustedDeviceAsync(int userId, string deviceName, string ip);
    Task RevokeAllTrustedDevicesAsync(int userId);
}

public class TwoFactorService : ITwoFactorService
{
    private readonly AppDbContext _db;
    private readonly IConnectionMultiplexer _redis;
    private const int OtpExpiryMinutes  = 5;
    private const int TrustedDeviceDays = 30;

    public TwoFactorService(AppDbContext db, IConnectionMultiplexer redis)
    {
        _db    = db;
        _redis = redis;
    }

    public async Task<string> GenerateOtpAsync(int userId, string purpose)
    {
        var existing = await _db.TwoFactorOtps
            .Where(o => o.UserId == userId && o.Purpose == purpose && !o.IsUsed)
            .ToListAsync();
        foreach (var o in existing) o.IsUsed = true;
        await _db.SaveChangesAsync();

        var otp = Random.Shared.Next(100000, 999999).ToString();

        _db.TwoFactorOtps.Add(new TwoFactorOtp
        {
            UserId    = userId,
            OtpCode   = BCrypt.Net.BCrypt.HashPassword(otp),
            Purpose   = purpose,
            ExpiresAt = DateTime.UtcNow.AddMinutes(OtpExpiryMinutes),
            IsUsed    = false,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        try
        {
            var redisDb = _redis.GetDatabase();
            await redisDb.StringSetAsync(
                $"otp:{userId}:{purpose}", otp,
                TimeSpan.FromMinutes(OtpExpiryMinutes));
        }
        catch { /* Redis unavailable — DB fallback still works */ }

        return otp;
    }

    public async Task<Result> ValidateOtpAsync(int userId, string otpCode, string purpose)
    {
        try
        {
            var redisDb   = _redis.GetDatabase();
            var cachedOtp = await redisDb.StringGetAsync($"otp:{userId}:{purpose}");

            if (!cachedOtp.IsNullOrEmpty)
            {
                if (cachedOtp != otpCode)
                    return Result.Failure("Invalid OTP code.", "INVALID_OTP");

                await redisDb.KeyDeleteAsync($"otp:{userId}:{purpose}");
                await MarkOtpUsedAsync(userId, purpose);
                return Result.Success();
            }
        }
        catch { /* fallback to DB */ }

        var record = await _db.TwoFactorOtps
            .Where(o => o.UserId == userId && o.Purpose == purpose &&
                        !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (record == null)
            return Result.Failure("OTP expired or not found.", "OTP_EXPIRED");

        if (!BCrypt.Net.BCrypt.Verify(otpCode, record.OtpCode))
            return Result.Failure("Invalid OTP code.", "INVALID_OTP");

        record.IsUsed = true;
        await _db.SaveChangesAsync();
        return Result.Success();
    }

    public async Task<bool> IsTrustedDeviceAsync(int userId, string deviceToken) =>
        await _db.TrustedDevices.AnyAsync(d =>
            d.UserId == userId &&
            d.DeviceToken == deviceToken &&
            !d.IsRevoked &&
            d.ExpiresAt > DateTime.UtcNow);

    public async Task<string> AddTrustedDeviceAsync(int userId, string deviceName, string ip)
    {
        var token = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        _db.TrustedDevices.Add(new TrustedDevice
        {
            UserId      = userId,
            DeviceToken = token,
            DeviceName  = deviceName,
            IpAddress   = ip,
            CreatedAt   = DateTime.UtcNow,
            ExpiresAt   = DateTime.UtcNow.AddDays(TrustedDeviceDays),
            IsRevoked   = false
        });
        await _db.SaveChangesAsync();
        return token;
    }

    public async Task RevokeAllTrustedDevicesAsync(int userId)
    {
        var devices = await _db.TrustedDevices
            .Where(d => d.UserId == userId && !d.IsRevoked)
            .ToListAsync();
        foreach (var d in devices) d.IsRevoked = true;
        await _db.SaveChangesAsync();
    }

    private async Task MarkOtpUsedAsync(int userId, string purpose)
    {
        var dbOtp = await _db.TwoFactorOtps
            .Where(o => o.UserId == userId && o.Purpose == purpose &&
                        !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (dbOtp != null)
        {
            dbOtp.IsUsed = true;
            await _db.SaveChangesAsync();
        }
    }
}
