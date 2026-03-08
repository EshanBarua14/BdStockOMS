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
    public int? NewQuantity { get; set; }
    public decimal? NewPrice { get; set; }
    public string? Reason { get; set; }
}
