// Services/OrderService.cs
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.Order;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public interface IOrderService
{
    Task<(OrderResponseDto? Order, string? Error)> PlaceOrderAsync(
        PlaceOrderDto dto, int placedByUserId, string placedByRole);
    Task<List<OrderResponseDto>> GetOrdersAsync(int userId, string role);
    Task<OrderResponseDto?> GetOrderByIdAsync(int orderId, int userId, string role);
    Task<(OrderResponseDto? Order, string? Error)> ExecuteOrderAsync(
        int orderId, int traderId);
    Task<(OrderResponseDto? Order, string? Error)> CancelOrderAsync(
        int orderId, int userId, string role, string reason);
    Task<List<PortfolioResponseDto>> GetPortfolioAsync(int investorId);
    Task<(OrderResponseDto? Order, string? Error)> AmendOrderAsync(int orderId, int userId, string role, AmendOrderRequestDto dto);
}

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────────────
    // PLACE ORDER — core validation logic
    // ─────────────────────────────────────────────────────────────────
    public async Task<(OrderResponseDto? Order, string? Error)> PlaceOrderAsync(
        PlaceOrderDto dto, int placedByUserId, string placedByRole)
    {
        // ── Determine investor ────────────────────
        int investorId;
        int? traderId = null;
        PlacedByRole placedBy;

        if (placedByRole == "Trader")
        {
            // Trader must specify which investor
            if (dto.InvestorId == null)
                return (null, "Trader must specify InvestorId.");

            // Verify investor is assigned to this trader
            var investor = await _db.Users.FirstOrDefaultAsync(u =>
                u.Id == dto.InvestorId &&
                u.AssignedTraderId == placedByUserId &&
                u.IsActive);

            if (investor == null)
                return (null, "Investor not found or not assigned to you.");

            investorId = dto.InvestorId.Value;
            traderId = placedByUserId;
            placedBy = PlacedByRole.Trader;
        }
        else // Investor placing their own order
        {
            investorId = placedByUserId;
            placedBy = PlacedByRole.Investor;
        }

        // ── Load investor with BO account ─────────
        var investorUser = await _db.Users
            .Include(u => u.BrokerageHouse)
            .FirstOrDefaultAsync(u => u.Id == investorId && u.IsActive);

        if (investorUser == null)
            return (null, "Investor not found.");

        // ── Validation 1: BO account must be active
        if (!investorUser.IsBOAccountActive)
            return (null, "Investor BO account is not active. Contact CCD.");

        // ── Load stock ────────────────────────────
        var stock = await _db.Stocks
            .FirstOrDefaultAsync(s => s.Id == dto.StockId && s.IsActive);

        if (stock == null)
            return (null, "Stock not found or inactive.");

        // ── Validation 2: Limit order must have LimitPrice
        if (dto.OrderCategory == OrderCategory.Limit && dto.LimitPrice == null)
            return (null, "Limit orders must specify a LimitPrice.");

        // ── Determine order price ─────────────────
        decimal orderPrice = dto.OrderCategory == OrderCategory.Market
            ? stock.LastTradePrice
            : dto.LimitPrice!.Value;

        // ── Validation 3: Circuit breaker check ───
        if (orderPrice > stock.CircuitBreakerHigh)
            return (null, $"Order price {orderPrice} exceeds circuit breaker high of {stock.CircuitBreakerHigh}.");

        if (orderPrice < stock.CircuitBreakerLow)
            return (null, $"Order price {orderPrice} is below circuit breaker low of {stock.CircuitBreakerLow}.");

        // ── Validation 4: Board lot size ──────────
        if (dto.Quantity < stock.BoardLotSize)
            return (null, $"Minimum order quantity is {stock.BoardLotSize} shares.");

        // ── Determine settlement type ─────────────
        var settlementType = (stock.Category == StockCategory.Z ||
                              stock.Category == StockCategory.Spot)
            ? SettlementType.T0
            : SettlementType.T2;

        // ── BUY order validations ─────────────────
        if (dto.OrderType == OrderType.Buy)
        {
            decimal totalCost = orderPrice * dto.Quantity;

            // Validation 5: Z/Spot category — cash only, no margin
            if (stock.Category == StockCategory.Z || stock.Category == StockCategory.Spot)
            {
                if (investorUser.CashBalance < totalCost)
                    return (null, $"Insufficient cash. Required: {totalCost:F2}, Available: {investorUser.CashBalance:F2}. Z/Spot category requires full cash payment.");
            }
            else
            {
                // Normal stocks — cash + available margin
                decimal availableMargin = investorUser.MarginLimit - investorUser.MarginUsed;
                decimal totalAvailable = investorUser.CashBalance + availableMargin;

                if (totalAvailable < totalCost)
                    return (null, $"Insufficient funds. Required: {totalCost:F2}, Available cash: {investorUser.CashBalance:F2}, Available margin: {availableMargin:F2}.");
            }
        }

        // ── SELL order validations ────────────────
        if (dto.OrderType == OrderType.Sell)
        {
            // Validation 6: Must own enough shares
            var portfolio = await _db.Portfolios
                .FirstOrDefaultAsync(p =>
                    p.InvestorId == investorId &&
                    p.StockId == dto.StockId);

            if (portfolio == null || portfolio.Quantity < dto.Quantity)
            {
                int held = portfolio?.Quantity ?? 0;
                return (null, $"Insufficient shares. Trying to sell {dto.Quantity}, but only holds {held}.");
            }
        }

        // ── All validations passed — create order ─
        var order = new Order
        {
            InvestorId = investorId,
            TraderId = traderId,
            StockId = dto.StockId,
            BrokerageHouseId = investorUser.BrokerageHouseId,
            OrderType = dto.OrderType,
            OrderCategory = dto.OrderCategory,
            Quantity = dto.Quantity,
            PriceAtOrder = stock.LastTradePrice,
            LimitPrice = dto.LimitPrice,
            SettlementType = settlementType,
            PlacedBy = placedBy,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);

        // ── Reserve funds for BUY orders ──────────
        if (dto.OrderType == OrderType.Buy)
        {
            decimal totalCost = orderPrice * dto.Quantity;

            if (investorUser.CashBalance >= totalCost)
            {
                investorUser.CashBalance -= totalCost;
            }
            else
            {
                // Use cash first, then margin
                decimal remaining = totalCost - investorUser.CashBalance;
                investorUser.CashBalance = 0;
                investorUser.MarginUsed += remaining;
            }
        }

        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(order).Reference(o => o.Stock).LoadAsync();
        await _db.Entry(order).Reference(o => o.Investor).LoadAsync();

        return (MapToDto(order), null);
    }

    // ─────────────────────────────────────────────────────────────────
    // EXECUTE ORDER — Trader executes a pending order
    // ─────────────────────────────────────────────────────────────────
    public async Task<(OrderResponseDto? Order, string? Error)> ExecuteOrderAsync(
        int orderId, int traderId)
    {
        var order = await _db.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return (null, "Order not found.");

        if (order.Status != OrderStatus.Pending)
            return (null, $"Order cannot be executed. Current status: {order.Status}.");

        // Execute at current market price
        order.Status = OrderStatus.Filled;
        order.ExecutionPrice = order.Stock.LastTradePrice;
        order.ExecutedAt = DateTime.UtcNow;
        order.TraderId ??= traderId; // assign trader if not already set

        await _db.SaveChangesAsync();
        return (MapToDto(order), null);
    }

    // ─────────────────────────────────────────────────────────────────
    // CANCEL ORDER
    // ─────────────────────────────────────────────────────────────────
    public async Task<(OrderResponseDto? Order, string? Error)> CancelOrderAsync(
        int orderId, int userId, string role, string reason)
    {
        var order = await _db.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return (null, "Order not found.");

        var cancellable = new[] {
            OrderStatus.Pending, OrderStatus.Queued, OrderStatus.Submitted,
            OrderStatus.Waiting, OrderStatus.Open, OrderStatus.CancelRequested
        };
        if (!cancellable.Contains(order.Status))
            return (null, $"Cannot cancel order in status {order.Status}.");

        // Investor can only cancel their own orders
        if (role == "Investor" && order.InvestorId != userId)
            return (null, "You can only cancel your own orders.");
        // SuperAdmin and Admin can cancel any order

        order.Status = OrderStatus.Cancelled;
        order.RejectionReason = reason;
        order.CancelledAt = DateTime.UtcNow;

        // ── Refund reserved funds ─────────────────
        var investor = await _db.Users.FindAsync(order.InvestorId);
        if (investor != null && order.OrderType == OrderType.Buy)
        {
            decimal totalCost = order.PriceAtOrder * order.Quantity;

            // Refund margin first, then cash
            if (investor.MarginUsed > 0)
            {
                decimal marginRefund = Math.Min(investor.MarginUsed, totalCost);
                investor.MarginUsed -= marginRefund;
                investor.CashBalance += (totalCost - marginRefund);
            }
            else
            {
                investor.CashBalance += totalCost;
            }
        }

        await _db.SaveChangesAsync();
        return (MapToDto(order), null);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET ORDERS — role scoped
    // ─────────────────────────────────────────────────────────────────
    public async Task<List<OrderResponseDto>> GetOrdersAsync(int userId, string role)
    {
        var query = _db.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .Include(o => o.Trader)
            .AsQueryable();

        query = role switch
        {
            "Investor" => query.Where(o => o.InvestorId == userId),
            "Trader"   => query.Where(o => o.TraderId == userId || o.Investor.AssignedTraderId == userId),
            _          => query // Admin, CCD, BrokerageHouse see all
        };

        return await query
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => MapToDto(o))
            .ToListAsync();
    }

    // ─────────────────────────────────────────────────────────────────
    // GET ORDER BY ID
    // ─────────────────────────────────────────────────────────────────
    public async Task<OrderResponseDto?> GetOrderByIdAsync(
        int orderId, int userId, string role)
    {
        var order = await _db.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .Include(o => o.Trader)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return null;

        // Scope check
        if (role == "Investor" && order.InvestorId != userId) return null;
        if (role == "Trader" && order.TraderId != userId &&
            order.Investor.AssignedTraderId != userId) return null;

        return MapToDto(order);
    }

    // ─────────────────────────────────────────────────────────────────
    // GET PORTFOLIO
    // ─────────────────────────────────────────────────────────────────
    public async Task<List<PortfolioResponseDto>> GetPortfolioAsync(int investorId)
    {
        return await _db.Portfolios
            .Include(p => p.Stock)
            .Include(p => p.Investor)
            .Where(p => p.InvestorId == investorId && p.Quantity > 0)
            .Select(p => new PortfolioResponseDto
            {
                Id = p.Id,
                InvestorId = p.InvestorId,
                InvestorName = p.Investor.FullName,
                StockId = p.StockId,
                TradingCode = p.Stock.TradingCode,
                CompanyName = p.Stock.CompanyName,
                Exchange = p.Stock.Exchange,
                Quantity = p.Quantity,
                AverageBuyPrice = p.AverageBuyPrice,
                CurrentMarketPrice = p.Stock.LastTradePrice,
                TotalInvestment = p.Quantity * p.AverageBuyPrice,
                CurrentValue = p.Quantity * p.Stock.LastTradePrice,
                ProfitLoss = (p.Quantity * p.Stock.LastTradePrice) - (p.Quantity * p.AverageBuyPrice),
                ProfitLossPercent = p.AverageBuyPrice == 0 ? 0 :
                    ((p.Stock.LastTradePrice - p.AverageBuyPrice) / p.AverageBuyPrice) * 100,
                LastUpdatedAt = p.LastUpdatedAt
            })
            .ToListAsync();
    }

    // ─────────────────────────────────────────────────────────────────
    // MAPPER
    // ─────────────────────────────────────────────────────────────────
    private static OrderResponseDto MapToDto(Order o) => new()
    {
        Id = o.Id,
        InvestorId = o.InvestorId,
        InvestorName = o.Investor?.FullName ?? "",
        TraderId = o.TraderId,
        TraderName = o.Trader?.FullName,
        StockId = o.StockId,
        TradingCode = o.Stock?.TradingCode ?? "",
        CompanyName = o.Stock?.CompanyName ?? "",
        Exchange = o.Stock?.Exchange ?? "",
        OrderType = o.OrderType,
        OrderCategory = o.OrderCategory,
        Quantity = o.Quantity,
        PriceAtOrder = o.PriceAtOrder,
        LimitPrice = o.LimitPrice,
        ExecutionPrice = o.ExecutionPrice,
        SettlementType = o.SettlementType,
        PlacedBy = o.PlacedBy,
        Status = o.Status,
        RejectionReason = o.RejectionReason,
        Notes = o.Notes,
        CreatedAt = o.CreatedAt,
        ExecutedAt = o.ExecutedAt,
        CompletedAt = o.CompletedAt,
        CancelledAt = o.CancelledAt,
        TotalValue = o.PriceAtOrder * o.Quantity
    };
    public async Task<(OrderResponseDto? Order, string? Error)> AmendOrderAsync(
        int orderId, int userId, string role, AmendOrderRequestDto dto)
    {
        var order = await _db.Orders
            .Include(o => o.Stock)
            .Include(o => o.Investor)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null) return (null, "Order not found.");

        var ok = new[] {
            OrderStatus.Pending, OrderStatus.Queued, OrderStatus.Submitted,
            OrderStatus.Waiting, OrderStatus.Open
        };
        if (!ok.Contains(order.Status))
            return (null, $"Cannot amend order in status {order.Status}.");

        if (role == "Investor" && order.InvestorId != userId)
            return (null, "You can only amend your own orders.");

        if (dto.Quantity.HasValue && dto.Quantity.Value > 0)     order.Quantity   = dto.Quantity.Value;
        if (dto.LimitPrice.HasValue && dto.LimitPrice.Value > 0) order.LimitPrice = dto.LimitPrice.Value;
        if (!string.IsNullOrEmpty(dto.Notes))                    order.Notes      = dto.Notes;

        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (MapToDto(order), null);
    }

}