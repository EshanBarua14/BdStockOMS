using BdStockOMS.API.Models;
namespace BdStockOMS.API.Services;

public interface ISettlementService
{
    Task<SettlementBatch> CreateBatchAsync(int brokerageHouseId, string exchange, DateTime tradeDate);
    Task<SettlementBatch> ProcessBatchAsync(int batchId);
    Task<List<SettlementBatch>> GetPendingBatchesAsync();
    Task<List<SettlementItem>> GetBatchItemsAsync(int batchId);
    DateTime CalculateSettlementDate(DateTime tradeDate, SettlementType type);
}
