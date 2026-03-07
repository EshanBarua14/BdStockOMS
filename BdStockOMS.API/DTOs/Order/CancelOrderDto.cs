// DTOs/Order/CancelOrderDto.cs
using System.ComponentModel.DataAnnotations;

namespace BdStockOMS.API.DTOs.Order;

public class CancelOrderDto
{
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}
