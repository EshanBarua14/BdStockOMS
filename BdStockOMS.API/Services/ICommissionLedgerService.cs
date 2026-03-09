using BdStockOMS.API.Models;
namespace BdStockOMS.API.Services;

public interface ICommissionLedgerService
{
    Task<CommissionLedger> PostTradeCommissionAsync(Trade trade, string exchange);
    Task<List<CommissionLedger>> GetInvestorLedgerAsync(int investorId, DateTime? from, DateTime? to);
    Task<decimal> GetTotalCommissionAsync(int brokerageHouseId, DateTime? from, DateTime? to);
}
