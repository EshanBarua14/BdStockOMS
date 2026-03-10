using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface IUserPermissionService
{
    Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(int userId);
    Task<bool> HasPermissionAsync(int userId, string permission);
    Task<UserPermission> GrantPermissionAsync(int userId, string permission, string module, int grantedBy, DateTime? expiresAt = null);
    Task<bool> RevokePermissionAsync(int userId, string permission);
    Task<IEnumerable<UserPermission>> GetModulePermissionsAsync(int userId, string module);
}

public class UserPermissionService : IUserPermissionService
{
    private readonly AppDbContext _db;

    public UserPermissionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<UserPermission>> GetUserPermissionsAsync(int userId) =>
        await _db.UserPermissions
            .Where(p => p.UserId == userId && p.IsGranted &&
                        (!p.ExpiresAt.HasValue || p.ExpiresAt > DateTime.UtcNow))
            .ToListAsync();

    public async Task<bool> HasPermissionAsync(int userId, string permission) =>
        await _db.UserPermissions
            .AnyAsync(p => p.UserId == userId
                        && p.Permission == permission
                        && p.IsGranted
                        && (!p.ExpiresAt.HasValue || p.ExpiresAt > DateTime.UtcNow));

    public async Task<UserPermission> GrantPermissionAsync(int userId, string permission,
        string module, int grantedBy, DateTime? expiresAt = null)
    {
        // Upsert — revoke existing then re-grant
        var existing = await _db.UserPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Permission == permission);

        if (existing != null)
        {
            existing.IsGranted      = true;
            existing.GrantedByUserId = grantedBy;
            existing.GrantedAt      = DateTime.UtcNow;
            existing.ExpiresAt      = expiresAt;
            await _db.SaveChangesAsync();
            return existing;
        }

        var perm = new UserPermission
        {
            UserId          = userId,
            Permission      = permission,
            Module          = module,
            IsGranted       = true,
            GrantedByUserId = grantedBy,
            GrantedAt       = DateTime.UtcNow,
            ExpiresAt       = expiresAt
        };
        _db.UserPermissions.Add(perm);
        await _db.SaveChangesAsync();
        return perm;
    }

    public async Task<bool> RevokePermissionAsync(int userId, string permission)
    {
        var perm = await _db.UserPermissions
            .FirstOrDefaultAsync(p => p.UserId == userId && p.Permission == permission);
        if (perm == null) return false;
        perm.IsGranted = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<UserPermission>> GetModulePermissionsAsync(int userId, string module) =>
        await _db.UserPermissions
            .Where(p => p.UserId == userId && p.Module == module && p.IsGranted &&
                        (!p.ExpiresAt.HasValue || p.ExpiresAt > DateTime.UtcNow))
            .ToListAsync();
}
