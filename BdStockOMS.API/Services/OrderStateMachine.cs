using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
namespace BdStockOMS.API.Services;

public class OrderStateMachine : IOrderStateMachine
{
    private readonly AppDbContext _db;

    // Valid transitions: key = from, value = allowed destinations
    private static readonly Dictionary<OrderStatus, OrderStatus[]> _transitions = new()
    {
        [OrderStatus.Pending]     = [OrderStatus.Executed, OrderStatus.Cancelled, OrderStatus.Rejected],
        [OrderStatus.Executed]    = [OrderStatus.Completed, OrderStatus.Cancelled],
        [OrderStatus.Completed]   = [],
        [OrderStatus.Cancelled]   = [],
        [OrderStatus.Rejected]    = [],
    };

    public OrderStateMachine(AppDbContext db) => _db = db;

    public bool CanTransition(OrderStatus from, OrderStatus to)
        => _transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public OrderStatus[] GetAllowedTransitions(OrderStatus current)
        => _transitions.TryGetValue(current, out var allowed) ? allowed : [];

    public async Task<bool> TransitionAsync(Order order, OrderStatus to,
        string? reason = null, string? triggeredBy = null)
    {
        if (!CanTransition(order.Status, to))
            return false;

        var evt = new OrderEvent
        {
            OrderId     = order.Id,
            FromStatus  = order.Status.ToString(),
            ToStatus    = to.ToString(),
            Reason      = reason,
            TriggeredBy = triggeredBy ?? "System",
            OccurredAt  = DateTime.UtcNow,
        };

        order.Status = to;

        // Update timestamps
        switch (to)
        {
            case OrderStatus.Executed:  order.ExecutedAt  = DateTime.UtcNow; break;
            case OrderStatus.Completed: order.CompletedAt = DateTime.UtcNow; break;
            case OrderStatus.Cancelled: order.CancelledAt = DateTime.UtcNow; break;
            case OrderStatus.Rejected:
                order.RejectionReason = reason;
                order.CancelledAt     = DateTime.UtcNow;
                break;
        }

        _db.Set<OrderEvent>().Add(evt);
        await _db.SaveChangesAsync();
        return true;
    }
}
