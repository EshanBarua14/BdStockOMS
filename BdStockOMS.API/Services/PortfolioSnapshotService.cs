using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class PortfolioSnapshotService : IPortfolioSnapshotService
    {
        private readonly AppDbContext _db;

        public PortfolioSnapshotService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<PortfolioSnapshot> CaptureSnapshotAsync(int userId, DateTime snapshotDate)
        {
            var user = await _db.Users.FindAsync(userId)
                       ?? throw new KeyNotFoundException($"User {userId} not found.");

            // Get all portfolio holdings for user
            var holdings = await _db.Portfolios
                .Where(p => p.InvestorId == userId && p.Quantity > 0)
                .Include(p => p.Stock)
                .ToListAsync();

            // Get realized PnL from closed trades
            var realizedPnL = await _db.Trades
                .Where(t => t.InvestorId == userId && t.Side == "Sell")
                .SumAsync(t => (decimal?)t.TotalValue) ?? 0m;

            var totalBuyCost = await _db.Trades
                .Where(t => t.InvestorId == userId && t.Side == "Buy")
                .SumAsync(t => (decimal?)t.TotalValue) ?? 0m;

            realizedPnL = realizedPnL - totalBuyCost;

            decimal totalInvested = holdings.Sum(p => p.AverageBuyPrice * p.Quantity);
            decimal currentValue  = holdings.Sum(p => p.Stock.LastTradePrice * p.Quantity);
            decimal unrealizedPnL = currentValue - totalInvested;
            decimal totalPnL      = unrealizedPnL + realizedPnL;
            decimal roiPercent    = totalInvested > 0
                                    ? Math.Round(totalPnL / totalInvested * 100, 6)
                                    : 0m;

            // Remove existing snapshot for same date if any
            var existing = await _db.PortfolioSnapshots
                .FirstOrDefaultAsync(s => s.UserId == userId &&
                                          s.SnapshotDate.Date == snapshotDate.Date);
            if (existing != null)
                _db.PortfolioSnapshots.Remove(existing);

            var snapshot = new PortfolioSnapshot
            {
                UserId           = userId,
                BrokerageHouseId = user.BrokerageHouseId,
                SnapshotDate     = snapshotDate.Date,
                TotalInvested    = totalInvested,
                CurrentValue     = currentValue,
                UnrealizedPnL    = unrealizedPnL,
                RealizedPnL      = realizedPnL,
                TotalPnL         = totalPnL,
                RoiPercent       = roiPercent,
                CashBalance      = 0m,
                TotalHoldings    = holdings.Count,
                CreatedAt        = DateTime.UtcNow
            };

            _db.PortfolioSnapshots.Add(snapshot);
            await _db.SaveChangesAsync();
            return snapshot;
        }

        public async Task<IEnumerable<PortfolioSnapshot>> GetSnapshotHistoryAsync(int userId, DateTime from, DateTime to)
        {
            return await _db.PortfolioSnapshots
                .Where(s => s.UserId == userId &&
                            s.SnapshotDate >= from.Date &&
                            s.SnapshotDate <= to.Date)
                .OrderBy(s => s.SnapshotDate)
                .ToListAsync();
        }

        public async Task<PortfolioSnapshot?> GetLatestSnapshotAsync(int userId)
        {
            return await _db.PortfolioSnapshots
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SnapshotDate)
                .FirstOrDefaultAsync();
        }

        public async Task<StockAnalytics> UpsertStockAnalyticsAsync(StockAnalyticsResult data)
        {
            var existing = await _db.StockAnalytics
                .FirstOrDefaultAsync(a => a.StockId == data.StockId && a.Exchange == data.Exchange);

            if (existing != null)
            {
                existing.Vwap          = data.Vwap;
                existing.High52W       = data.High52W;
                existing.Low52W        = data.Low52W;
                existing.Beta          = data.Beta;
                existing.AvgVolume30D  = data.AvgVolume30D;
                existing.CalculatedAt  = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return existing;
            }

            var analytics = new StockAnalytics
            {
                StockId       = data.StockId,
                Exchange      = data.Exchange,
                Vwap          = data.Vwap,
                High52W       = data.High52W,
                Low52W        = data.Low52W,
                Beta          = data.Beta,
                AvgVolume30D  = data.AvgVolume30D,
                CalculatedAt  = DateTime.UtcNow
            };

            _db.StockAnalytics.Add(analytics);
            await _db.SaveChangesAsync();
            return analytics;
        }

        public async Task<StockAnalytics?> GetStockAnalyticsAsync(int stockId, string exchange)
        {
            return await _db.StockAnalytics
                .Include(a => a.Stock)
                .FirstOrDefaultAsync(a => a.StockId == stockId && a.Exchange == exchange);
        }

        public async Task<IEnumerable<StockAnalytics>> GetAllAnalyticsAsync(string exchange)
        {
            return await _db.StockAnalytics
                .Include(a => a.Stock)
                .Where(a => a.Exchange == exchange)
                .OrderBy(a => a.StockId)
                .ToListAsync();
        }

        public async Task<decimal> CalculateRoiAsync(int userId)
        {
            var latest = await GetLatestSnapshotAsync(userId);
            return latest?.RoiPercent ?? 0m;
        }

        public async Task<int> CaptureAllSnapshotsAsync(DateTime snapshotDate)
        {
            var userIds = await _db.Portfolios
                .Where(p => p.Quantity > 0)
                .Select(p => p.InvestorId)
                .Distinct()
                .ToListAsync();

            int count = 0;
            foreach (var userId in userIds)
            {
                await CaptureSnapshotAsync(userId, snapshotDate);
                count++;
            }
            return count;
        }
    }
}
