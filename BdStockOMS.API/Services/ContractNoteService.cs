using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BdStockOMS.API.Services
{
    public class ContractNoteService : IContractNoteService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ContractNoteService> _logger;

        public ContractNoteService(AppDbContext db, ILogger<ContractNoteService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<ContractNoteResult> GenerateContractNoteAsync(int orderId)
        {
            var result = new ContractNoteResult();
            try
            {
                var existing = await _db.ContractNotes
                    .FirstOrDefaultAsync(c => c.OrderId == orderId && !c.IsVoid);
                if (existing != null)
                {
                    result.Success = true;
                    result.Message = "Contract note already exists for this order.";
                    result.ContractNote = MapToDto(existing);
                    return result;
                }

                var order = await _db.Orders
                    .Include(o => o.Stock)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    result.Success = false;
                    result.Message = $"Order {orderId} not found.";
                    return result;
                }

                if (order.Status != OrderStatus.Executed && order.Status != OrderStatus.Completed)
                {
                    result.Success = false;
                    result.Message = $"Cannot generate contract note for order in status: {order.Status}.";
                    return result;
                }

                var client = await _db.Users.FirstOrDefaultAsync(u => u.Id == order.InvestorId);
                var commRate = await _db.CommissionRates.FirstOrDefaultAsync();

                var executedPrice = order.ExecutionPrice ?? order.PriceAtOrder;
                var grossAmount = order.Quantity * executedPrice;
                var commissionRate = commRate?.BuyRate ?? 0.005m;
                var commissionAmount = grossAmount * commissionRate;
                var cdscFee = grossAmount * 0.0005m;
                var levyCharge = grossAmount * 0.0003m;
                var vatOnCommission = commissionAmount * 0.15m;
                var isBuy = order.OrderType == OrderType.Buy;
                var netAmount = isBuy
                    ? grossAmount + commissionAmount + cdscFee + levyCharge + vatOnCommission
                    : grossAmount - commissionAmount - cdscFee - levyCharge - vatOnCommission;

                string traderName = "N/A";
                if (order.TraderId.HasValue)
                {
                    var trader = await _db.Users.FindAsync(order.TraderId.Value);
                    traderName = trader?.FullName ?? "N/A";
                }

                var contractNote = new ContractNote
                {
                    ContractNoteNumber = GenerateContractNoteNumber(orderId),
                    OrderId = orderId,
                    ClientId = order.InvestorId,
                    TraderName = traderName,
                    BranchName = "Main Branch",
                    InstrumentCode = order.Stock?.TradingCode ?? "N/A",
                    InstrumentName = order.Stock?.CompanyName ?? "N/A",
                    Side = order.OrderType.ToString(),
                    Quantity = order.Quantity,
                    ExecutedPrice = executedPrice,
                    GrossAmount = grossAmount,
                    CommissionAmount = commissionAmount,
                    CdscFee = cdscFee,
                    LevyCharge = levyCharge,
                    VatOnCommission = vatOnCommission,
                    NetAmount = netAmount,
                    TradeDate = order.ExecutedAt ?? DateTime.UtcNow,
                    SettlementDate = (order.ExecutedAt ?? DateTime.UtcNow).AddDays(2),
                    GeneratedAt = DateTime.UtcNow,
                    Status = "Generated"
                };

                _db.ContractNotes.Add(contractNote);
                await _db.SaveChangesAsync();

                _logger.LogInformation("Contract note {Number} generated for order {OrderId}.",
                    contractNote.ContractNoteNumber, orderId);

                result.Success = true;
                result.Message = "Contract note generated successfully.";
                result.ContractNote = MapToDto(contractNote);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating contract note for order {OrderId}", orderId);
                result.Success = false;
                result.Message = ex.Message;
                result.Errors.Add(ex.ToString());
            }
            return result;
        }

        public async Task<ContractNoteResult> GetContractNoteAsync(int contractNoteId)
        {
            var result = new ContractNoteResult();
            var cn = await _db.ContractNotes
                .Include(c => c.Order).ThenInclude(o => o!.Stock)
                .Include(c => c.Client)
                .FirstOrDefaultAsync(c => c.Id == contractNoteId);
            if (cn == null)
            {
                result.Success = false;
                result.Message = $"Contract note {contractNoteId} not found.";
                return result;
            }
            result.Success = true;
            result.ContractNote = MapToDto(cn);
            return result;
        }

        public async Task<List<ContractNoteSummary>> GetContractNotesByClientAsync(
            int clientId, DateTime? from, DateTime? to)
        {
            var query = _db.ContractNotes
                .Include(c => c.Client)
                .Where(c => c.ClientId == clientId && !c.IsVoid);
            if (from.HasValue) query = query.Where(c => c.TradeDate >= from.Value);
            if (to.HasValue)   query = query.Where(c => c.TradeDate <= to.Value);
            return await query
                .OrderByDescending(c => c.TradeDate)
                .Select(c => new ContractNoteSummary
                {
                    Id = c.Id,
                    ContractNoteNumber = c.ContractNoteNumber,
                    OrderId = c.OrderId,
                    ClientName = c.Client != null ? c.Client.FullName : "N/A",
                    InstrumentCode = c.InstrumentCode,
                    Side = c.Side,
                    Quantity = c.Quantity,
                    NetAmount = c.NetAmount,
                    TradeDate = c.TradeDate,
                    Status = c.Status
                })
                .ToListAsync();
        }

        public async Task<List<ContractNoteSummary>> GetContractNotesByDateAsync(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);
            return await _db.ContractNotes
                .Include(c => c.Client)
                .Where(c => c.TradeDate >= start && c.TradeDate < end && !c.IsVoid)
                .OrderByDescending(c => c.TradeDate)
                .Select(c => new ContractNoteSummary
                {
                    Id = c.Id,
                    ContractNoteNumber = c.ContractNoteNumber,
                    OrderId = c.OrderId,
                    ClientName = c.Client != null ? c.Client.FullName : "N/A",
                    InstrumentCode = c.InstrumentCode,
                    Side = c.Side,
                    Quantity = c.Quantity,
                    NetAmount = c.NetAmount,
                    TradeDate = c.TradeDate,
                    Status = c.Status
                })
                .ToListAsync();
        }

        public async Task<ContractNoteResult> RegenerateContractNoteAsync(int orderId)
        {
            var existing = await _db.ContractNotes
                .FirstOrDefaultAsync(c => c.OrderId == orderId && !c.IsVoid);
            if (existing != null)
            {
                existing.IsVoid = true;
                existing.VoidedAt = DateTime.UtcNow;
                existing.VoidReason = "Regenerated by user request.";
                await _db.SaveChangesAsync();
            }
            return await GenerateContractNoteAsync(orderId);
        }

        public async Task<byte[]> ExportContractNotePdfAsync(int contractNoteId)
        {
            var cn = await _db.ContractNotes.FindAsync(contractNoteId);
            if (cn == null) return Array.Empty<byte>();
            var text = $"CONTRACT NOTE\n" +
                       $"Number: {cn.ContractNoteNumber}\n" +
                       $"Trade Date: {cn.TradeDate:yyyy-MM-dd}\n" +
                       $"Instrument: {cn.InstrumentCode} - {cn.InstrumentName}\n" +
                       $"Side: {cn.Side}\n" +
                       $"Quantity: {cn.Quantity}\n" +
                       $"Executed Price: {cn.ExecutedPrice:F2}\n" +
                       $"Gross Amount: {cn.GrossAmount:F2}\n" +
                       $"Commission: {cn.CommissionAmount:F2}\n" +
                       $"Net Amount: {cn.NetAmount:F2}\n" +
                       $"Settlement Date: {cn.SettlementDate:yyyy-MM-dd}\n";
            return System.Text.Encoding.UTF8.GetBytes(text);
        }

        private static string GenerateContractNoteNumber(int orderId)
        {
            var datePart = DateTime.UtcNow.ToString("yyyyMMdd");
            return $"CN-{datePart}-{orderId:D6}";
        }

        private static ContractNoteDto MapToDto(ContractNote cn) => new()
        {
            Id = cn.Id,
            ContractNoteNumber = cn.ContractNoteNumber,
            OrderId = cn.OrderId,
            OrderReference = $"ORD-{cn.OrderId:D6}",
            ClientId = cn.ClientId,
            ClientName = cn.Client?.FullName ?? "N/A",
            BoAccountNumber = cn.Client?.BONumber ?? "N/A",
            TraderName = cn.TraderName,
            BranchName = cn.BranchName,
            InstrumentCode = cn.InstrumentCode,
            InstrumentName = cn.InstrumentName,
            Side = cn.Side,
            Quantity = cn.Quantity,
            ExecutedPrice = cn.ExecutedPrice,
            GrossAmount = cn.GrossAmount,
            CommissionAmount = cn.CommissionAmount,
            CdscFee = cn.CdscFee,
            LevyCharge = cn.LevyCharge,
            VatOnCommission = cn.VatOnCommission,
            NetAmount = cn.NetAmount,
            TradeDate = cn.TradeDate,
            SettlementDate = cn.SettlementDate,
            GeneratedAt = cn.GeneratedAt,
            Status = cn.Status
        };
    }
}
