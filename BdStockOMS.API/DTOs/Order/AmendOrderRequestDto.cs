namespace BdStockOMS.API.DTOs.Order;

public record AmendOrderRequestDto(int? Quantity, decimal? LimitPrice, string? Notes);
