using BdStockOMS.API.Models;

namespace BdStockOMS.API.Repositories.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    // Get all orders for a specific investor
    Task<IEnumerable<Order>> GetByInvestorAsync(int investorId);

    // Get all orders assigned to a specific trader
    Task<IEnumerable<Order>> GetByTraderAsync(int traderId);

    // Get all orders for a brokerage firm
    Task<IEnumerable<Order>> GetByBrokerageHouseAsync(int brokerageHouseId);

    // Get orders by status — pending, approved etc
    Task<IEnumerable<Order>> GetByStatusAsync(OrderStatus status);

    // Get orders pending execution for a trader
    Task<IEnumerable<Order>> GetPendingExecutionAsync(int traderId);

    // Update just the status of an order
    Task<bool> UpdateStatusAsync(int orderId, OrderStatus status);
}