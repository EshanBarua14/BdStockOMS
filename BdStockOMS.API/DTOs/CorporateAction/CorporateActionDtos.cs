namespace BdStockOMS.API.DTOs.CorporateAction;

public class CorporateActionResponseDto
{
    public int Id { get; set; }
    public int StockId { get; set; }
    public string TradingCode { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime RecordDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Description { get; set; }
    public bool IsProcessed { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCorporateActionDto
{
    public int StockId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public DateTime RecordDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Description { get; set; }
}

public class UpdateCorporateActionDto
{
    public decimal Value { get; set; }
    public DateTime RecordDate { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string? Description { get; set; }
    public bool IsProcessed { get; set; }
}
