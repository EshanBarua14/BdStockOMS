using BdStockOMS.API.Models;

namespace BdStockOMS.API.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    Task<(IEnumerable<Order> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? investorId = null,
        int? brokerageHouseId = null, OrderStatus? status = null);
    Task<IEnumerable<Order>> GetPendingOrdersAsync(int investorId);
}
