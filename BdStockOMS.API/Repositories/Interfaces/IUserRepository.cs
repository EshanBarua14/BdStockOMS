using BdStockOMS.API.Models;

namespace BdStockOMS.API.Repositories.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdWithRoleAsync(int id);
    Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? roleId = null, int? brokerageHouseId = null);
}
