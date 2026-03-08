using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.OrderAmendment;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services;

public class OrderAmendmentService : IOrderAmendmentService
{
    private readonly AppDbContext _context;

    public OrderAmendmentService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<OrderAmendmentResponseDto>> AmendOrderAsync(int orderId, int amendedByUserId, AmendOrderDto dto)
    {
        var order = await _context.Orders
            .Include(o => o.Stock)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return Result<OrderAmendmentResponseDto>.Failure("Order not found.");

        if (order.Status != OrderStatus.Pending)
            return Result<OrderAmendmentResponseDto>.Failure("Only pending orders can be amended.");

        if (dto.NewQuantity == null && dto.NewPrice == null)
            return Result<OrderAmendmentResponseDto>.Failure("At least one of NewQuantity or NewPrice must be provided.");

        if (dto.NewQuantity.HasValue && dto.NewQuantity.Value <= 0)
            return Result<OrderAmendmentResponseDto>.Failure("New quantity must be greater than zero.");

        if (dto.NewPrice.HasValue && dto.NewPrice.Value <= 0)
            return Result<OrderAmendmentResponseDto>.Failure("New price must be greater than zero.");

        if (dto.NewPrice.HasValue && order.OrderCategory != OrderCategory.Limit)
            return Result<OrderAmendmentResponseDto>.Failure("Price can only be amended on Limit orders.");

        var amendedBy = await _context.Users.FindAsync(amendedByUserId);
        if (amendedBy == null)
            return Result<OrderAmendmentResponseDto>.Failure("Amending user not found.");

        var amendment = new OrderAmendment
        {
            OrderId = orderId,
            AmendedByUserId = amendedByUserId,
            OldQuantity = dto.NewQuantity.HasValue ? order.Quantity : null,
            NewQuantity = dto.NewQuantity,
            OldPrice = dto.NewPrice.HasValue ? order.LimitPrice : null,
            NewPrice = dto.NewPrice,
            Reason = dto.Reason,
            CreatedAt = DateTime.UtcNow
        };

        if (dto.NewQuantity.HasValue)
            order.Quantity = dto.NewQuantity.Value;

        if (dto.NewPrice.HasValue)
            order.LimitPrice = dto.NewPrice.Value;

        _context.OrderAmendments.Add(amendment);
        await _context.SaveChangesAsync();

        amendment.Order = order;
        amendment.AmendedBy = amendedBy;

        return Result<OrderAmendmentResponseDto>.Success(ToDto(amendment));
    }

    public async Task<Result<List<OrderAmendmentResponseDto>>> GetByOrderAsync(int orderId)
    {
        var order = await _context.Orders.FindAsync(orderId);
        if (order == null)
            return Result<List<OrderAmendmentResponseDto>>.Failure("Order not found.");

        var items = await _context.OrderAmendments
            .Include(a => a.Order).ThenInclude(o => o.Stock)
            .Include(a => a.AmendedBy)
            .Where(a => a.OrderId == orderId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a))
            .ToListAsync();

        return Result<List<OrderAmendmentResponseDto>>.Success(items);
    }

    public async Task<Result<List<OrderAmendmentResponseDto>>> GetByUserAsync(int userId)
    {
        var items = await _context.OrderAmendments
            .Include(a => a.Order).ThenInclude(o => o.Stock)
            .Include(a => a.AmendedBy)
            .Where(a => a.AmendedByUserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToDto(a))
            .ToListAsync();

        return Result<List<OrderAmendmentResponseDto>>.Success(items);
    }

    private static OrderAmendmentResponseDto ToDto(OrderAmendment a) => new()
    {
        Id = a.Id,
        OrderId = a.OrderId,
        TradingCode = a.Order?.Stock?.TradingCode ?? string.Empty,
        AmendedByUserId = a.AmendedByUserId,
        AmendedByName = a.AmendedBy?.FullName ?? string.Empty,
        OldQuantity = a.OldQuantity,
        NewQuantity = a.NewQuantity,
        OldPrice = a.OldPrice,
        NewPrice = a.NewPrice,
        Reason = a.Reason,
        CreatedAt = a.CreatedAt
    };
}
