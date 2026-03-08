using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Repositories;

public class UserRepository : BaseRepository<User>, IUserRepository
{
    public UserRepository(AppDbContext db) : base(db) { }

    public async Task<User?> GetByEmailAsync(string email) =>
        await _db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> GetByIdWithRoleAsync(int id) =>
        await _db.Users
                 .Include(u => u.Role)
                 .FirstOrDefaultAsync(u => u.Id == id);

    public async Task<(IEnumerable<User> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? roleId = null, int? brokerageHouseId = null)
    {
        var query = _db.Users.Include(u => u.Role).AsQueryable();

        if (roleId.HasValue)
            query = query.Where(u => u.RoleId == roleId.Value);

        if (brokerageHouseId.HasValue)
            query = query.Where(u => u.BrokerageHouseId == brokerageHouseId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }
}
