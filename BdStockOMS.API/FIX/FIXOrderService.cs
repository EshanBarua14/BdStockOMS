using BdStockOMS.API.Data;
using BdStockOMS.API.FIX;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.FIX;

public interface IFIXOrderService
{
    Task<FIXSendResult> PlaceViaFIXAsync(int orderId, CancellationToken ct = default);
    Task<FIXSendResult> CancelViaFIXAsync(int orderId, CancellationToken ct = default);
    Task<FIXSendResult> AmendViaFIXAsync(int orderId, int newQty, decimal? newPrice, CancellationToken ct = default);
}

public class FIXOrderService : IFIXOrderService
{
    private readonly AppDbContext _db;
    private readonly IFIXConnectorFactory _factory;
    private readonly ILogger<FIXOrderService> _logger;

    public FIXOrderService(AppDbContext db, IFIXConnectorFactory factory,
        ILogger<FIXOrderService> logger)
    {
        _db      = db;
        _factory = factory;
        _logger  = logger;
    }

    public async Task<FIXSendResult> PlaceViaFIXAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders.Include(o => o.Stock)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order == null) return new FIXSendResult { Success = false, Message = "Order not found." };

        var connector = _factory.GetConnector(order.ExchangeId.ToString());
        if (connector.SessionState != FIXSessionState.Active)
            await connector.ConnectAsync(ct);

        var req = new FIXOrderRequest
        {
            ClOrdID          = order.ClOrdID ?? Guid.NewGuid().ToString("N")[..20],
            Symbol           = order.Stock?.TradingCode ?? $"STK{order.StockId}",
            StockId          = order.StockId,
            OrderType        = order.OrderType,
            Category         = order.OrderCategory,
            TimeInForce      = order.TimeInForce,
            Quantity         = order.Quantity,
            Price            = order.LimitPrice,
            InvestorId       = order.InvestorId,
            BrokerageHouseId = order.BrokerageHouseId,
            Exchange         = order.ExchangeId,
            Board            = order.Board,
            MinQty           = order.MinQty,
            DisplayQty       = order.DisplayQty,
            IsPrivate        = order.IsPrivate,
        };

        var result = await connector.SendNewOrderAsync(req, ct);
        if (result.Success)
        {
            order.ClOrdID   = result.ClOrdID;
            order.Status    = OrderStatus.Submitted;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return result;
    }

    public async Task<FIXSendResult> CancelViaFIXAsync(int orderId, CancellationToken ct = default)
    {
        var order = await _db.Orders.Include(o => o.Stock)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order == null) return new FIXSendResult { Success = false, Message = "Order not found." };

        var newClOrdId = Guid.NewGuid().ToString("N")[..20];
        var connector  = _factory.GetConnector(order.ExchangeId.ToString());
        if (connector.SessionState != FIXSessionState.Active)
            await connector.ConnectAsync(ct);

        var result = await connector.SendCancelAsync(
            newClOrdId, order.ClOrdID ?? string.Empty,
            order.Stock?.TradingCode ?? string.Empty, ct);

        if (result.Success)
        {
            order.Status          = OrderStatus.CancelRequested;
            order.OrigClOrdID     = order.ClOrdID;
            order.ClOrdID         = newClOrdId;
            order.UpdatedAt       = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return result;
    }

    public async Task<FIXSendResult> AmendViaFIXAsync(int orderId, int newQty,
        decimal? newPrice, CancellationToken ct = default)
    {
        var order = await _db.Orders.Include(o => o.Stock)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);
        if (order == null) return new FIXSendResult { Success = false, Message = "Order not found." };

        var newClOrdId = Guid.NewGuid().ToString("N")[..20];
        var connector  = _factory.GetConnector(order.ExchangeId.ToString());
        if (connector.SessionState != FIXSessionState.Active)
            await connector.ConnectAsync(ct);

        var req = new FIXOrderRequest
        {
            ClOrdID          = newClOrdId,
            OrigClOrdID      = order.ClOrdID,
            Symbol           = order.Stock?.TradingCode ?? string.Empty,
            StockId          = order.StockId,
            OrderType        = order.OrderType,
            Category         = order.OrderCategory,
            TimeInForce      = order.TimeInForce,
            Quantity         = newQty,
            Price            = newPrice ?? order.LimitPrice,
            InvestorId       = order.InvestorId,
            BrokerageHouseId = order.BrokerageHouseId,
            Exchange         = order.ExchangeId,
            Board            = order.Board,
        };

        var result = await connector.SendAmendAsync(req, ct);
        if (result.Success)
        {
            order.OrigClOrdID = order.ClOrdID;
            order.ClOrdID     = newClOrdId;
            order.Quantity    = newQty;
            if (newPrice.HasValue) order.LimitPrice = newPrice;
            order.Status      = OrderStatus.EditRequested;
            order.UpdatedAt   = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return result;
    }
}
