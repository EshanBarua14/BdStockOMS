using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Repositories;

public class OrderRepository : BaseRepository<Order>, IOrderRepository
{
    public OrderRepository(AppDbContext db) : base(db) { }

    public async Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? investorId = null,
        int? brokerageHouseId = null, OrderStatus? status = null)
    {
        var query = _db.Orders
                       .Include(o => o.Stock)
                       .Include(o => o.Investor)
                       .AsQueryable();

        if (investorId.HasValue)
            query = query.Where(o => o.InvestorId == investorId.Value);

        if (brokerageHouseId.HasValue)
            query = query.Where(o => o.BrokerageHouseId == brokerageHouseId.Value);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IEnumerable<Order>> GetPendingOrdersAsync(int investorId) =>
        await _db.Orders
                 .Where(o => o.InvestorId == investorId &&
                             o.Status == OrderStatus.Pending)
                 .ToListAsync();
}
