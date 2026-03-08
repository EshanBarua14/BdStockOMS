using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(AppDbContext db) : base(db) { }

    public async Task<RefreshToken?> GetActiveTokenAsync(string token) =>
        await _db.RefreshTokens
                 .Include(r => r.User)
                 .FirstOrDefaultAsync(r =>
                     r.Token == token &&
                     r.RevokedAt == null &&
                     r.ExpiresAt > DateTime.UtcNow);

    public async Task RevokeAllUserTokensAsync(int userId, string revokedByIp)
    {
        var tokens = await _db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync();

        foreach (var t in tokens)
        {
            t.RevokedAt    = DateTime.UtcNow;
            t.RevokedByIp  = revokedByIp;
        }
        await _db.SaveChangesAsync();
    }
}
