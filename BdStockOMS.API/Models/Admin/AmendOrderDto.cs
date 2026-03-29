using BdStockOMS.API.Models;

namespace BdStockOMS.API.Models.Admin
{
    public class AmendOrderDto
    {
        public int Quantity { get; set; }
        public decimal? LimitPrice { get; set; }
        public TimeInForce? TimeInForce { get; set; }
        public int? MinQty { get; set; }
        public int? DisplayQty { get; set; }
        public string? Notes { get; set; }
    }
}
