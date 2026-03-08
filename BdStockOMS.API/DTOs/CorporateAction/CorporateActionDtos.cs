using System.ComponentModel.DataAnnotations;

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
    [Range(1, int.MaxValue, ErrorMessage = "StockId must be a positive integer.")]
    public int StockId { get; set; }

    [Required(ErrorMessage = "Type is required.")]
    [StringLength(50, ErrorMessage = "Type cannot exceed 50 characters.")]
    public string Type { get; set; } = string.Empty;

    [Range(0.0001, double.MaxValue, ErrorMessage = "Value must be greater than zero.")]
    public decimal Value { get; set; }

    public DateTime RecordDate { get; set; }
    public DateTime? PaymentDate { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }
}

public class UpdateCorporateActionDto
{
    [Range(0.0001, double.MaxValue, ErrorMessage = "Value must be greater than zero.")]
    public decimal Value { get; set; }

    public DateTime RecordDate { get; set; }
    public DateTime? PaymentDate { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters.")]
    public string? Description { get; set; }

    public bool IsProcessed { get; set; }
}
