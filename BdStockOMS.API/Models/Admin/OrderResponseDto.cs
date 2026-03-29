using BdStockOMS.API.Models;

namespace BdStockOMS.API.Models.Admin
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int BrokerageHouseId { get; set; }
        public int InvestorId { get; set; }
        public int? TraderId { get; set; }
        public int StockId { get; set; }
        public string? StockCode { get; set; }
        public OrderType OrderType { get; set; }
        public OrderCategory OrderCategory { get; set; }
        public TimeInForce TimeInForce { get; set; }
        public ExchangeId ExchangeId { get; set; }
        public Board Board { get; set; }
        public ExecInstruction ExecInstruction { get; set; }
        public int Quantity { get; set; }
        public int ExecutedQuantity { get; set; }
        public int? MinQty { get; set; }
        public int? DisplayQty { get; set; }
        public decimal PriceAtOrder { get; set; }
        public decimal? LimitPrice { get; set; }
        public decimal? ExecutionPrice { get; set; }
        public decimal? GrossTradeAmt { get; set; }
        public OrderStatus Status { get; set; }
        public bool IsPrivate { get; set; }
        public string? ClOrdID { get; set; }
        public string? OrigClOrdID { get; set; }
        public string? TrdMatchID { get; set; }
        public string? SettlDate { get; set; }
        public AggressorSide AggressorIndicator { get; set; }
        public PlacedByRole PlacedByRole { get; set; }
        public SettlementType SettlementType { get; set; }
        public DateTime PlacedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public string? Notes { get; set; }
        public string? RejectionReason { get; set; }
    }
}
