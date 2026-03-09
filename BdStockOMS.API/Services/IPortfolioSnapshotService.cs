using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class SnapshotResult
    {
        public int UserId { get; set; }
        public DateTime SnapshotDate { get; set; }
        public decimal TotalInvested { get; set; }
        public decimal CurrentValue { get; set; }
        public decimal UnrealizedPnL { get; set; }
        public decimal RealizedPnL { get; set; }
        public decimal TotalPnL { get; set; }
        public decimal RoiPercent { get; set; }
        public decimal CashBalance { get; set; }
        public int TotalHoldings { get; set; }
    }

    public class StockAnalyticsResult
    {
        public int StockId { get; set; }
        public string Exchange { get; set; } = string.Empty;
        public decimal Vwap { get; set; }
        public decimal High52W { get; set; }
        public decimal Low52W { get; set; }
        public decimal Beta { get; set; }
        public decimal AvgVolume30D { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public interface IPortfolioSnapshotService
    {
        Task<PortfolioSnapshot> CaptureSnapshotAsync(int userId, DateTime snapshotDate);
        Task<IEnumerable<PortfolioSnapshot>> GetSnapshotHistoryAsync(int userId, DateTime from, DateTime to);
        Task<PortfolioSnapshot?> GetLatestSnapshotAsync(int userId);
        Task<StockAnalytics> UpsertStockAnalyticsAsync(StockAnalyticsResult data);
        Task<StockAnalytics?> GetStockAnalyticsAsync(int stockId, string exchange);
        Task<IEnumerable<StockAnalytics>> GetAllAnalyticsAsync(string exchange);
        Task<decimal> CalculateRoiAsync(int userId);
        Task<int> CaptureAllSnapshotsAsync(DateTime snapshotDate);
    }
}
