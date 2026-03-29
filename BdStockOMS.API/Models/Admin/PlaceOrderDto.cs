using BdStockOMS.API.Models;

namespace BdStockOMS.API.Models.Admin
{
    public class PlaceOrderDto
    {
        public int StockId { get; set; }
        public OrderType OrderType { get; set; }
        public OrderCategory OrderCategory { get; set; }
        public TimeInForce TimeInForce { get; set; } = TimeInForce.Day;
        public ExchangeId ExchangeId { get; set; } = ExchangeId.DSE;
        public Board Board { get; set; } = Board.Public;
        public ExecInstruction ExecInstruction { get; set; } = ExecInstruction.None;
        public int Quantity { get; set; }
        public decimal? LimitPrice { get; set; }
        public int? MinQty { get; set; }
        public int? DisplayQty { get; set; }
        public bool IsPrivate { get; set; } = false;
        public SettlementType SettlementType { get; set; } = SettlementType.T2;
        public string? Notes { get; set; }
        public string? ClOrdID { get; set; }
    }
}
