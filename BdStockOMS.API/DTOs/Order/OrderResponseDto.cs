// DTOs/Order/OrderResponseDto.cs
using BdStockOMS.API.Models;

namespace BdStockOMS.API.DTOs.Order;

public class OrderResponseDto
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public int? TraderId { get; set; }
    public string? TraderName { get; set; }
    public int StockId { get; set; }
    public string TradingCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Exchange { get; set; } = string.Empty;
    public OrderType OrderType { get; set; }
    public OrderCategory OrderCategory { get; set; }
    public int Quantity { get; set; }
    public decimal PriceAtOrder { get; set; }
    public decimal? LimitPrice { get; set; }
    public decimal? ExecutionPrice { get; set; }
    public SettlementType SettlementType { get; set; }
    public PlacedByRole PlacedBy { get; set; }
    public OrderStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExecutedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal TotalValue { get; set; } // Quantity × PriceAtOrder
}
