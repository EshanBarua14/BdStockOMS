using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.TraderReassignment;

public class TraderReassignmentResponseDto
{
    public int Id { get; set; }
    public int InvestorId { get; set; }
    public string InvestorName { get; set; } = string.Empty;
    public int? OldTraderId { get; set; }
    public string? OldTraderName { get; set; }
    public int NewTraderId { get; set; }
    public string NewTraderName { get; set; } = string.Empty;
    public int ReassignedByUserId { get; set; }
    public string ReassignedByName { get; set; } = string.Empty;
    public int BrokerageHouseId { get; set; }
    public string BrokerageHouseName { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateTraderReassignmentDto
{
    [Range(1, int.MaxValue, ErrorMessage = "InvestorId must be a positive integer.")]
    public int InvestorId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "NewTraderId must be a positive integer.")]
    public int NewTraderId { get; set; }

    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string? Reason { get; set; }
}
