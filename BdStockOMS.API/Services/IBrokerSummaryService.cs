using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BdStockOMS.API.Services
{
    public class BrokerSummaryDto
    {
        public int BrokerageHouseId { get; set; }
        public string BrokerName { get; set; } = string.Empty;
        public int TotalInvestors { get; set; }
        public int TotalTraders { get; set; }
        public int TotalOrdersToday { get; set; }
        public decimal TotalBuyValueToday { get; set; }
        public decimal TotalSellValueToday { get; set; }
        public decimal TotalTurnoverToday { get; set; }
        public int PendingKycCount { get; set; }
        public int ActiveOrdersCount { get; set; }
        public decimal TotalCommissionToday { get; set; }
    }

    public class TraderSummaryDto
    {
        public int TraderId { get; set; }
        public string TraderName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int TotalClients { get; set; }
        public int OrdersToday { get; set; }
        public decimal BuyValueToday { get; set; }
        public decimal SellValueToday { get; set; }
        public decimal TotalValueToday { get; set; }
    }

    public class ClientActivityDto
    {
        public int InvestorId { get; set; }
        public string InvestorName { get; set; } = string.Empty;
        public int OrdersToday { get; set; }
        public decimal BuyValueToday { get; set; }
        public decimal SellValueToday { get; set; }
        public decimal TotalValueToday { get; set; }
        public bool IsKycApproved { get; set; }
    }

    public interface IBrokerSummaryService
    {
        Task<IEnumerable<BrokerSummaryDto>> GetAllBrokerSummariesAsync(DateTime date);
        Task<BrokerSummaryDto> GetBrokerSummaryAsync(int brokerageHouseId, DateTime date);
        Task<IEnumerable<TraderSummaryDto>> GetTopTradersByValueAsync(int brokerageHouseId, DateTime date, int top = 10);
        Task<IEnumerable<TraderSummaryDto>> GetTopTradersByBuyAsync(int brokerageHouseId, DateTime date, int top = 10);
        Task<IEnumerable<TraderSummaryDto>> GetTopTradersBySellAsync(int brokerageHouseId, DateTime date, int top = 10);
        Task<IEnumerable<ClientActivityDto>> GetClientActivityAsync(int traderId, DateTime date);
        Task<IEnumerable<ClientActivityDto>> GetTopClientsByValueAsync(int brokerageHouseId, DateTime date, int top = 10);
    }
}
