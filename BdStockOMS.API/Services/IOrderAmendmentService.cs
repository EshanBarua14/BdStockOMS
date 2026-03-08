using BdStockOMS.API.Common;
using BdStockOMS.API.DTOs.OrderAmendment;

namespace BdStockOMS.API.Services;

public interface IOrderAmendmentService
{
    Task<Result<OrderAmendmentResponseDto>> AmendOrderAsync(int orderId, int amendedByUserId, AmendOrderDto dto);
    Task<Result<List<OrderAmendmentResponseDto>>> GetByOrderAsync(int orderId);
    Task<Result<List<OrderAmendmentResponseDto>>> GetByUserAsync(int userId);
}
