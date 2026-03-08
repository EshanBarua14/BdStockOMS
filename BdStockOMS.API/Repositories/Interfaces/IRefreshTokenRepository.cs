using BdStockOMS.API.Models;

namespace BdStockOMS.API.Repositories.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetActiveTokenAsync(string token);
    Task RevokeAllUserTokensAsync(int userId, string revokedByIp);
}
