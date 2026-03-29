using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BdStockOMS.API.Common;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.TBond;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BdStockOMS.API.Services
{
    public class TBondService : ITBondService
    {
        private readonly AppDbContext _db;
        public TBondService(AppDbContext db) => _db = db;

        public async Task<Result<TBondResponseDto>> CreateAsync(CreateTBondDto dto, CancellationToken ct = default)
        {
            if (dto.MaturityDate <= dto.IssueDate)
                return Result<TBondResponseDto>.Failure("MaturityDate must be after IssueDate.");
            if (!Enum.TryParse<CouponFrequency>(dto.CouponFrequency, true, out var freq))
                return Result<TBondResponseDto>.Failure("Invalid CouponFrequency: " + dto.CouponFrequency);
            if (await _db.TBonds.AnyAsync(b => b.ISIN == dto.ISIN, ct))
                return Result<TBondResponseDto>.Failure("ISIN already exists.");

            var bond = new TBond
            {
                ISIN = dto.ISIN, Name = dto.Name,
                FaceValue = dto.FaceValue, CouponRate = dto.CouponRate,
                CouponFrequency = freq, IssueDate = dto.IssueDate,
                MaturityDate = dto.MaturityDate, TotalIssueSize = dto.TotalIssueSize,
                OutstandingSize = dto.TotalIssueSize, Description = dto.Description,
                Status = TBondStatus.Active
            };
            _db.TBonds.Add(bond);
            await _db.SaveChangesAsync(ct);
            return Result<TBondResponseDto>.Success(ToDto(bond));
        }

        public async Task<Result<TBondResponseDto>> GetAsync(int id, CancellationToken ct = default)
        {
            var bond = await _db.TBonds.FirstOrDefaultAsync(b => b.Id == id, ct);
            return bond == null
                ? Result<TBondResponseDto>.Failure("T-bond not found.")
                : Result<TBondResponseDto>.Success(ToDto(bond));
        }

        public async Task<Result<List<TBondResponseDto>>> GetAllAsync(string? status, CancellationToken ct = default)
        {
            var q = _db.TBonds.AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<TBondStatus>(status, true, out var s))
                q = q.Where(b => b.Status == s);
            var list = await q.OrderBy(b => b.MaturityDate).ToListAsync(ct);
            return Result<List<TBondResponseDto>>.Success(list.Select(ToDto).ToList());
        }

        public async Task<Result<TBondOrderResponseDto>> PlaceOrderAsync(PlaceTBondOrderDto dto, CancellationToken ct = default)
        {
            var bond = await _db.TBonds.FirstOrDefaultAsync(b => b.Id == dto.TBondId, ct);
            if (bond == null) return Result<TBondOrderResponseDto>.Failure("T-bond not found.");
            if (bond.Status != TBondStatus.Active) return Result<TBondOrderResponseDto>.Failure("T-bond is not active.");

            var side = dto.Side.Trim().ToLower();
            if (side != "buy" && side != "sell")
                return Result<TBondOrderResponseDto>.Failure("Side must be Buy or Sell.");

            var total = dto.Quantity * (dto.Price / 100m) * bond.FaceValue;

            var order = new TBondOrder
            {
                TBondId = dto.TBondId, InvestorId = dto.InvestorId,
                BrokerageHouseId = dto.BrokerageHouseId,
                Side = char.ToUpper(dto.Side[0]) + dto.Side.Substring(1).ToLower(),
                Quantity = dto.Quantity, Price = dto.Price,
                TotalAmount = total, Notes = dto.Notes,
                Status = TBondOrderStatus.Pending
            };
            _db.TBondOrders.Add(order);
            await _db.SaveChangesAsync(ct);
            return Result<TBondOrderResponseDto>.Success(ToOrderDto(order, bond));
        }

        public async Task<Result<TBondOrderResponseDto>> ExecuteOrderAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _db.TBondOrders.Include(o => o.TBond).FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order == null) return Result<TBondOrderResponseDto>.Failure("Order not found.");
            if (order.Status != TBondOrderStatus.Pending) return Result<TBondOrderResponseDto>.Failure("Only pending orders can be executed.");

            order.Status = TBondOrderStatus.Executed;
            order.ExecutedAt = DateTime.UtcNow;

            // Update or create holding
            var holding = await _db.TBondHoldings
                .FirstOrDefaultAsync(h => h.TBondId == order.TBondId && h.InvestorId == order.InvestorId, ct);

            if (order.Side == "Buy")
            {
                if (holding == null)
                {
                    _db.TBondHoldings.Add(new TBondHolding
                    {
                        TBondId = order.TBondId, InvestorId = order.InvestorId,
                        BrokerageHouseId = order.BrokerageHouseId,
                        FaceValueHeld = order.Quantity, AverageCost = order.Price
                    });
                }
                else
                {
                    var totalFace = holding.FaceValueHeld + order.Quantity;
                    holding.AverageCost = ((holding.FaceValueHeld * holding.AverageCost)
                                         + (order.Quantity * order.Price)) / totalFace;
                    holding.FaceValueHeld = totalFace;
                    holding.LastUpdatedAt = DateTime.UtcNow;
                }
            }
            else // Sell
            {
                if (holding == null || holding.FaceValueHeld < order.Quantity)
                    return Result<TBondOrderResponseDto>.Failure("Insufficient holding to sell.");
                holding.FaceValueHeld -= order.Quantity;
                holding.LastUpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            return Result<TBondOrderResponseDto>.Success(ToOrderDto(order, order.TBond));
        }

        public async Task<Result<TBondOrderResponseDto>> SettleOrderAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _db.TBondOrders.Include(o => o.TBond).FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order == null) return Result<TBondOrderResponseDto>.Failure("Order not found.");
            if (order.Status != TBondOrderStatus.Executed) return Result<TBondOrderResponseDto>.Failure("Only executed orders can be settled.");
            order.Status = TBondOrderStatus.Settled;
            order.SettledAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return Result<TBondOrderResponseDto>.Success(ToOrderDto(order, order.TBond));
        }

        public async Task<Result<TBondOrderResponseDto>> CancelOrderAsync(int orderId, CancellationToken ct = default)
        {
            var order = await _db.TBondOrders.Include(o => o.TBond).FirstOrDefaultAsync(o => o.Id == orderId, ct);
            if (order == null) return Result<TBondOrderResponseDto>.Failure("Order not found.");
            if (order.Status != TBondOrderStatus.Pending) return Result<TBondOrderResponseDto>.Failure("Only pending orders can be cancelled.");
            order.Status = TBondOrderStatus.Cancelled;
            await _db.SaveChangesAsync(ct);
            return Result<TBondOrderResponseDto>.Success(ToOrderDto(order, order.TBond));
        }

        public async Task<Result<List<TBondOrderResponseDto>>> GetOrdersAsync(int? investorId, int? tbondId, CancellationToken ct = default)
        {
            var q = _db.TBondOrders.Include(o => o.TBond).AsQueryable();
            if (investorId.HasValue) q = q.Where(o => o.InvestorId == investorId.Value);
            if (tbondId.HasValue)    q = q.Where(o => o.TBondId == tbondId.Value);
            var list = await q.OrderByDescending(o => o.OrderedAt).ToListAsync(ct);
            return Result<List<TBondOrderResponseDto>>.Success(list.Select(o => ToOrderDto(o, o.TBond)).ToList());
        }

        public async Task<Result<List<CouponPaymentResponseDto>>> GenerateCouponsAsync(int tbondId, CancellationToken ct = default)
        {
            var bond = await _db.TBonds.FirstOrDefaultAsync(b => b.Id == tbondId, ct);
            if (bond == null) return Result<List<CouponPaymentResponseDto>>.Failure("T-bond not found.");
            if (bond.Status != TBondStatus.Active) return Result<List<CouponPaymentResponseDto>>.Failure("T-bond is not active.");

            var holdings = await _db.TBondHoldings.Where(h => h.TBondId == tbondId).ToListAsync(ct);
            if (!holdings.Any()) return Result<List<CouponPaymentResponseDto>>.Failure("No holdings found for this bond.");

            var periodsPerYear = bond.CouponFrequency switch
            {
                CouponFrequency.Monthly    => 12,
                CouponFrequency.Quarterly  => 4,
                CouponFrequency.SemiAnnual => 2,
                CouponFrequency.Annual     => 1,
                _ => 2
            };

            var periodMonths = 12 / periodsPerYear;
            var periodRate   = bond.CouponRate / periodsPerYear;
            var created      = new List<CouponPayment>();
            var now          = DateTime.UtcNow;
            var periodStart  = bond.IssueDate;

            while (periodStart < bond.MaturityDate)
            {
                var periodEnd   = periodStart.AddMonths(periodMonths);
                if (periodEnd > bond.MaturityDate) periodEnd = bond.MaturityDate;
                var paymentDate = periodEnd;

                foreach (var holding in holdings)
                {
                    var exists = await _db.CouponPayments.AnyAsync(
                        cp => cp.TBondId == tbondId
                           && cp.InvestorId == holding.InvestorId
                           && cp.PeriodEnd == periodEnd, ct);
                    if (exists) continue;

                    var coupon = new CouponPayment
                    {
                        TBondId = tbondId, InvestorId = holding.InvestorId,
                        BrokerageHouseId = holding.BrokerageHouseId,
                        HoldingFaceValue = holding.FaceValueHeld,
                        CouponRate = periodRate,
                        CouponAmount = holding.FaceValueHeld * periodRate,
                        PeriodStart = periodStart, PeriodEnd = periodEnd,
                        PaymentDate = paymentDate, IsPaid = false
                    };
                    created.Add(coupon);
                }
                periodStart = periodEnd;
            }

            if (created.Any())
            {
                _db.CouponPayments.AddRange(created);
                await _db.SaveChangesAsync(ct);
            }

            return Result<List<CouponPaymentResponseDto>>.Success(created.Select(c => ToCouponDto(c, bond.Name)).ToList());
        }

        public async Task<Result<int>> PayCouponsAsync(int tbondId, DateTime upTo, CancellationToken ct = default)
        {
            var bond = await _db.TBonds.FirstOrDefaultAsync(b => b.Id == tbondId, ct);
            if (bond == null) return Result<int>.Failure("T-bond not found.");

            var due = await _db.CouponPayments
                .Where(cp => cp.TBondId == tbondId && !cp.IsPaid && cp.PaymentDate <= upTo)
                .ToListAsync(ct);

            foreach (var cp in due) { cp.IsPaid = true; cp.PaidAt = DateTime.UtcNow; }
            await _db.SaveChangesAsync(ct);
            return Result<int>.Success(due.Count);
        }

        public async Task<Result<List<CouponPaymentResponseDto>>> GetCouponsAsync(int tbondId, CancellationToken ct = default)
        {
            var bond = await _db.TBonds.FirstOrDefaultAsync(b => b.Id == tbondId, ct);
            if (bond == null) return Result<List<CouponPaymentResponseDto>>.Failure("T-bond not found.");

            var list = await _db.CouponPayments
                .Where(cp => cp.TBondId == tbondId)
                .OrderBy(cp => cp.PeriodEnd)
                .ToListAsync(ct);

            return Result<List<CouponPaymentResponseDto>>.Success(list.Select(c => ToCouponDto(c, bond.Name)).ToList());
        }

        public async Task<Result<MaturityProcessResultDto>> ProcessMaturityAsync(int tbondId, CancellationToken ct = default)
        {
            var bond = await _db.TBonds.FirstOrDefaultAsync(b => b.Id == tbondId, ct);
            if (bond == null) return Result<MaturityProcessResultDto>.Failure("T-bond not found.");
            if (bond.Status != TBondStatus.Active) return Result<MaturityProcessResultDto>.Failure("T-bond is not active.");
            if (DateTime.UtcNow < bond.MaturityDate) return Result<MaturityProcessResultDto>.Failure("T-bond has not yet matured.");

            var holdings = await _db.TBondHoldings.Where(h => h.TBondId == tbondId).ToListAsync(ct);
            decimal totalPaid = 0;

            foreach (var h in holdings)
            {
                totalPaid += h.FaceValueHeld * bond.FaceValue / 100m;
                h.FaceValueHeld = 0;
                h.LastUpdatedAt = DateTime.UtcNow;
            }

            bond.Status = TBondStatus.Matured;
            bond.OutstandingSize = 0;
            await _db.SaveChangesAsync(ct);

            return Result<MaturityProcessResultDto>.Success(new MaturityProcessResultDto
            {
                TBondId = bond.Id, BondName = bond.Name,
                HoldingsSettled = holdings.Count, TotalPaidOut = totalPaid
            });
        }

        public async Task<Result<List<TBondHoldingResponseDto>>> GetHoldingsAsync(int investorId, CancellationToken ct = default)
        {
            var list = await _db.TBondHoldings
                .Include(h => h.TBond)
                .Where(h => h.InvestorId == investorId)
                .ToListAsync(ct);

            return Result<List<TBondHoldingResponseDto>>.Success(list.Select(h => new TBondHoldingResponseDto
            {
                Id = h.Id, TBondId = h.TBondId,
                BondName = h.TBond?.Name ?? string.Empty,
                ISIN = h.TBond?.ISIN ?? string.Empty,
                InvestorId = h.InvestorId,
                FaceValueHeld = h.FaceValueHeld,
                AverageCost = h.AverageCost,
                CurrentValue = h.FaceValueHeld * (h.TBond?.FaceValue ?? 0) / 100m,
                LastUpdatedAt = h.LastUpdatedAt
            }).ToList());
        }

        private static TBondResponseDto ToDto(TBond b) => new()
        {
            Id = b.Id, ISIN = b.ISIN, Name = b.Name,
            FaceValue = b.FaceValue, CouponRate = b.CouponRate,
            CouponFrequency = b.CouponFrequency.ToString(),
            IssueDate = b.IssueDate, MaturityDate = b.MaturityDate,
            TotalIssueSize = b.TotalIssueSize, OutstandingSize = b.OutstandingSize,
            Status = b.Status.ToString(), Description = b.Description,
            DaysToMaturity = Math.Max(0, (int)(b.MaturityDate - DateTime.UtcNow).TotalDays),
            CreatedAt = b.CreatedAt
        };

        private static TBondOrderResponseDto ToOrderDto(TBondOrder o, TBond bond) => new()
        {
            Id = o.Id, TBondId = o.TBondId,
            BondName = bond?.Name ?? string.Empty, ISIN = bond?.ISIN ?? string.Empty,
            InvestorId = o.InvestorId, Side = o.Side,
            Quantity = o.Quantity, Price = o.Price, TotalAmount = o.TotalAmount,
            Status = o.Status.ToString(), OrderedAt = o.OrderedAt,
            ExecutedAt = o.ExecutedAt, SettledAt = o.SettledAt, Notes = o.Notes
        };

        private static CouponPaymentResponseDto ToCouponDto(CouponPayment c, string bondName) => new()
        {
            Id = c.Id, TBondId = c.TBondId, BondName = bondName,
            InvestorId = c.InvestorId, HoldingFaceValue = c.HoldingFaceValue,
            CouponRate = c.CouponRate, CouponAmount = c.CouponAmount,
            PeriodStart = c.PeriodStart, PeriodEnd = c.PeriodEnd,
            PaymentDate = c.PaymentDate, IsPaid = c.IsPaid, PaidAt = c.PaidAt
        };
    }
}
