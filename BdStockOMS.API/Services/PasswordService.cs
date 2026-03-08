using System.Text.RegularExpressions;
using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface IPasswordService
{
    Task<Result> ChangePasswordAsync(int userId, string currentPassword,
                                     string newPassword, string ip);
    Task<Result> ValidatePasswordStrengthAsync(string password);
    Task<bool> IsPasswordReusedAsync(int userId, string newPassword);
    Task SavePasswordHistoryAsync(int userId, string passwordHash);
    Task<bool> IsPasswordExpiredAsync(int userId);
}

public class PasswordService : IPasswordService
{
    private readonly AppDbContext _db;
    private readonly IAuditService _audit;
    private const int PasswordHistoryCount = 5;
    private const int PasswordExpiryDays   = 90;

    public PasswordService(AppDbContext db, IAuditService audit)
    {
        _db    = db;
        _audit = audit;
    }

    public async Task<Result> ChangePasswordAsync(int userId, string currentPassword,
                                                   string newPassword, string ip)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return Result.Failure("User not found.", "USER_NOT_FOUND");

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            return Result.Failure("Current password is incorrect.", "WRONG_PASSWORD");

        var strengthResult = await ValidatePasswordStrengthAsync(newPassword);
        if (!strengthResult.IsSuccess) return strengthResult;

        if (await IsPasswordReusedAsync(userId, newPassword))
            return Result.Failure(
                $"Cannot reuse your last {PasswordHistoryCount} passwords.",
                "PASSWORD_REUSED");

        await SavePasswordHistoryAsync(userId, user.PasswordHash);

        user.PasswordHash        = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.PasswordChangedAt   = DateTime.UtcNow;
        user.ForcePasswordChange = false;
        await _db.SaveChangesAsync();

        await _audit.LogAsync(userId, "PASSWORD_CHANGED", "User", userId, null, null, ip);
        return Result.Success();
    }

    public Task<Result> ValidatePasswordStrengthAsync(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return Task.FromResult(Result.Failure(
                "Password must be at least 8 characters.", "PASSWORD_TOO_SHORT"));

        if (!Regex.IsMatch(password, @"[A-Z]"))
            return Task.FromResult(Result.Failure(
                "Password must contain at least one uppercase letter.", "PASSWORD_NO_UPPER"));

        if (!Regex.IsMatch(password, @"[a-z]"))
            return Task.FromResult(Result.Failure(
                "Password must contain at least one lowercase letter.", "PASSWORD_NO_LOWER"));

        if (!Regex.IsMatch(password, @"[0-9]"))
            return Task.FromResult(Result.Failure(
                "Password must contain at least one digit.", "PASSWORD_NO_DIGIT"));

        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?"":{}|<>]"))
            return Task.FromResult(Result.Failure(
                "Password must contain at least one special character.", "PASSWORD_NO_SPECIAL"));

        return Task.FromResult(Result.Success());
    }

    public async Task<bool> IsPasswordReusedAsync(int userId, string newPassword)
    {
        var history = await _db.PasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(PasswordHistoryCount)
            .ToListAsync();

        return history.Any(h => BCrypt.Net.BCrypt.Verify(newPassword, h.PasswordHash));
    }

    public async Task SavePasswordHistoryAsync(int userId, string passwordHash)
    {
        _db.PasswordHistories.Add(new PasswordHistory
        {
            UserId       = userId,
            PasswordHash = passwordHash,
            CreatedAt    = DateTime.UtcNow
        });

        var old = await _db.PasswordHistories
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(PasswordHistoryCount)
            .ToListAsync();

        if (old.Any()) _db.PasswordHistories.RemoveRange(old);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> IsPasswordExpiredAsync(int userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user?.PasswordChangedAt == null) return false;
        return (DateTime.UtcNow - user.PasswordChangedAt.Value).TotalDays > PasswordExpiryDays;
    }
}
