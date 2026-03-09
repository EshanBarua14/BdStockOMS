using BdStockOMS.API.Models;
namespace BdStockOMS.API.Services;

public interface IOrderStateMachine
{
    bool CanTransition(OrderStatus from, OrderStatus to);
    OrderStatus[] GetAllowedTransitions(OrderStatus current);
    Task<bool> TransitionAsync(Order order, OrderStatus to,
        string? reason = null, string? triggeredBy = null);
}
