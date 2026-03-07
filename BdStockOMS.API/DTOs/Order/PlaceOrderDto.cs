// DTOs/Order/PlaceOrderDto.cs
using System.ComponentModel.DataAnnotations;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.DTOs.Order;

public class PlaceOrderDto
{
    [Required]
    public int StockId { get; set; }

    [Required]
    public OrderType OrderType { get; set; }      // Buy or Sell

    [Required]
    public OrderCategory OrderCategory { get; set; } // Market or Limit

    [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
    public int Quantity { get; set; }

    // Required only for Limit orders
    public decimal? LimitPrice { get; set; }

    // Trader fills this when placing on behalf of an investor
    // Null when investor places their own order
    public int? InvestorId { get; set; }
}
