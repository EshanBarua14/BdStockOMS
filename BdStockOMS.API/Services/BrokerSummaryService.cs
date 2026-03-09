using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;

namespace BdStockOMS.API.Services
{
    public class BrokerSummaryService : IBrokerSummaryService
    {
        private readonly AppDbContext _db;

        public BrokerSummaryService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<BrokerSummaryDto>> GetAllBrokerSummariesAsync(DateTime date)
        {
            var brokers = await _db.BrokerageHouses.Where(b => b.IsActive).ToListAsync();
            var results = new List<BrokerSummaryDto>();
            foreach (var broker in brokers)
                results.Add(await GetBrokerSummaryAsync(broker.Id, date));
            return results;
        }

        public async Task<BrokerSummaryDto> GetBrokerSummaryAsync(int brokerageHouseId, DateTime date)
        {
            var broker = await _db.BrokerageHouses.FindAsync(brokerageHouseId)
                         ?? throw new KeyNotFoundException($"Brokerage house {brokerageHouseId} not found.");

            var dateStart = date.Date;
            var dateEnd   = dateStart.AddDays(1);

            var todayOrders = await _db.Orders
                .Where(o => o.BrokerageHouseId == brokerageHouseId &&
                            o.CreatedAt >= dateStart && o.CreatedAt < dateEnd)
                .ToListAsync();

            var todayTrades = await _db.Trades
                .Where(t => t.BrokerageHouseId == brokerageHouseId &&
                            t.TradedAt >= dateStart && t.TradedAt < dateEnd)
                .ToListAsync();

            var totalInvestors = await _db.Users
                .CountAsync(u => u.BrokerageHouseId == brokerageHouseId &&
                                 u.Role != null && u.Role.Name == "Investor");

            var totalTraders = await _db.Users
                .CountAsync(u => u.BrokerageHouseId == brokerageHouseId &&
                                 u.Role != null && u.Role.Name == "Trader");

            var pendingKyc = await _db.KycDocuments
                .CountAsync(k => k.User.BrokerageHouseId == brokerageHouseId &&
                                 (k.Status == KycStatus.Pending || k.Status == KycStatus.UnderReview));

            var activeOrders = await _db.Orders
                .CountAsync(o => o.BrokerageHouseId == brokerageHouseId &&
                                 o.Status == OrderStatus.Pending);

            var commissions = await _db.CommissionLedgers
                .Where(c => c.BrokerageHouseId == brokerageHouseId &&
                            c.PostedAt >= dateStart && c.PostedAt < dateEnd)
                .SumAsync(c => (decimal?)c.TotalCharges) ?? 0m;

            var buyValue  = todayTrades.Where(t => t.Side == "Buy").Sum(t => t.TotalValue);
            var sellValue = todayTrades.Where(t => t.Side == "Sell").Sum(t => t.TotalValue);

            return new BrokerSummaryDto
            {
                BrokerageHouseId    = brokerageHouseId,
                BrokerName          = broker.Name,
                TotalInvestors      = totalInvestors,
                TotalTraders        = totalTraders,
                TotalOrdersToday    = todayOrders.Count,
                TotalBuyValueToday  = buyValue,
                TotalSellValueToday = sellValue,
                TotalTurnoverToday  = buyValue + sellValue,
                PendingKycCount     = pendingKyc,
                ActiveOrdersCount   = activeOrders,
                TotalCommissionToday = commissions
            };
        }

        public async Task<IEnumerable<TraderSummaryDto>> GetTopTradersByValueAsync(
            int brokerageHouseId, DateTime date, int top = 10)
        {
            var summaries = await GetTraderSummariesAsync(brokerageHouseId, date);
            return summaries.OrderByDescending(t => t.TotalValueToday).Take(top);
        }

        public async Task<IEnumerable<TraderSummaryDto>> GetTopTradersByBuyAsync(
            int brokerageHouseId, DateTime date, int top = 10)
        {
            var summaries = await GetTraderSummariesAsync(brokerageHouseId, date);
            return summaries.OrderByDescending(t => t.BuyValueToday).Take(top);
        }

        public async Task<IEnumerable<TraderSummaryDto>> GetTopTradersBySellAsync(
            int brokerageHouseId, DateTime date, int top = 10)
        {
            var summaries = await GetTraderSummariesAsync(brokerageHouseId, date);
            return summaries.OrderByDescending(t => t.SellValueToday).Take(top);
        }

        private async Task<List<TraderSummaryDto>> GetTraderSummariesAsync(
            int brokerageHouseId, DateTime date)
        {
            var dateStart = date.Date;
            var dateEnd   = dateStart.AddDays(1);

            var traders = await _db.Users
                .Include(u => u.Role)
                .Where(u => u.BrokerageHouseId == brokerageHouseId &&
                            u.Role != null && u.Role.Name == "Trader")
                .ToListAsync();

            var result = new List<TraderSummaryDto>();
            foreach (var trader in traders)
            {
                var clientIds = await _db.Users
                    .Where(u => u.BrokerageHouseId == brokerageHouseId &&
                                u.Role != null && u.Role.Name == "Investor")
                    .Select(u => u.Id)
                    .ToListAsync();

                var trades = await _db.Trades
                    .Where(t => clientIds.Contains(t.InvestorId) &&
                                t.TradedAt >= dateStart && t.TradedAt < dateEnd)
                    .ToListAsync();

                result.Add(new TraderSummaryDto
                {
                    TraderId       = trader.Id,
                    TraderName     = trader.FullName,
                    Email          = trader.Email,
                    TotalClients   = clientIds.Count,
                    OrdersToday    = trades.Count,
                    BuyValueToday  = trades.Where(t => t.Side == "Buy").Sum(t => t.TotalValue),
                    SellValueToday = trades.Where(t => t.Side == "Sell").Sum(t => t.TotalValue),
                    TotalValueToday = trades.Sum(t => t.TotalValue)
                });
            }
            return result;
        }

        public async Task<IEnumerable<ClientActivityDto>> GetClientActivityAsync(
            int traderId, DateTime date)
        {
            var dateStart = date.Date;
            var dateEnd   = dateStart.AddDays(1);

            var trader = await _db.Users.FindAsync(traderId)
                         ?? throw new KeyNotFoundException($"Trader {traderId} not found.");

            var clients = await _db.Users
                .Where(u => u.BrokerageHouseId == trader.BrokerageHouseId &&
                            u.Role != null && u.Role.Name == "Investor")
                .ToListAsync();

            var result = new List<ClientActivityDto>();
            foreach (var client in clients)
            {
                var trades = await _db.Trades
                    .Where(t => t.InvestorId == client.Id &&
                                t.TradedAt >= dateStart && t.TradedAt < dateEnd)
                    .ToListAsync();

                var isKycApproved = await _db.KycDocuments
                    .AnyAsync(k => k.UserId == client.Id && k.Status == KycStatus.Approved);

                result.Add(new ClientActivityDto
                {
                    InvestorId      = client.Id,
                    InvestorName    = client.FullName,
                    OrdersToday     = trades.Count,
                    BuyValueToday   = trades.Where(t => t.Side == "Buy").Sum(t => t.TotalValue),
                    SellValueToday  = trades.Where(t => t.Side == "Sell").Sum(t => t.TotalValue),
                    TotalValueToday = trades.Sum(t => t.TotalValue),
                    IsKycApproved   = isKycApproved
                });
            }
            return result;
        }

        public async Task<IEnumerable<ClientActivityDto>> GetTopClientsByValueAsync(
            int brokerageHouseId, DateTime date, int top = 10)
        {
            var dateStart = date.Date;
            var dateEnd   = dateStart.AddDays(1);

            var clients = await _db.Users
                .Where(u => u.BrokerageHouseId == brokerageHouseId &&
                            u.Role != null && u.Role.Name == "Investor")
                .ToListAsync();

            var result = new List<ClientActivityDto>();
            foreach (var client in clients)
            {
                var trades = await _db.Trades
                    .Where(t => t.InvestorId == client.Id &&
                                t.TradedAt >= dateStart && t.TradedAt < dateEnd)
                    .ToListAsync();

                var isKycApproved = await _db.KycDocuments
                    .AnyAsync(k => k.UserId == client.Id && k.Status == KycStatus.Approved);

                result.Add(new ClientActivityDto
                {
                    InvestorId      = client.Id,
                    InvestorName    = client.FullName,
                    OrdersToday     = trades.Count,
                    BuyValueToday   = trades.Where(t => t.Side == "Buy").Sum(t => t.TotalValue),
                    SellValueToday  = trades.Where(t => t.Side == "Sell").Sum(t => t.TotalValue),
                    TotalValueToday = trades.Sum(t => t.TotalValue),
                    IsKycApproved   = isKycApproved
                });
            }
            return result.OrderByDescending(c => c.TotalValueToday).Take(top).ToList();
        }
    }
}
