using System.ComponentModel.DataAnnotations;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.DTOs.Order;

public class PlaceOrderDto
{
    [Required] public int StockId { get; set; }
    [Required] public OrderType OrderType { get; set; }
    [Required] public OrderCategory OrderCategory { get; set; }
    [Required, Range(1, int.MaxValue)] public int Quantity { get; set; }
    public decimal? LimitPrice { get; set; }
    public int? InvestorId { get; set; }
    public TimeInForce TimeInForce { get; set; } = TimeInForce.Day;
    public ExchangeId ExchangeId { get; set; } = ExchangeId.DSE;
    public Board Board { get; set; } = Board.Public;
    public ExecInstruction ExecInstruction { get; set; } = ExecInstruction.None;
    public int? MinQty { get; set; }
    public int? DisplayQty { get; set; }
    public bool IsPrivate { get; set; } = false;
    public string? ClOrdID { get; set; }
}
