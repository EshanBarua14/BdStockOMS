using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.OrderAmendment;

public class OrderAmendmentResponseDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string TradingCode { get; set; } = string.Empty;
    public int AmendedByUserId { get; set; }
    public string AmendedByName { get; set; } = string.Empty;
    public int? OldQuantity { get; set; }
    public int? NewQuantity { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AmendOrderDto
{
    [Range(1, int.MaxValue, ErrorMessage = "New quantity must be greater than zero.")]
    public int? NewQuantity { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "New price must be greater than zero.")]
    public decimal? NewPrice { get; set; }

    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }
}
