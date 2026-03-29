using System;
using System.Linq;
using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.TBond;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests
{
    public class Day91TBondTests
    {
        private AppDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private TBondService CreateSvc(AppDbContext db) => new(db);

        private CreateTBondDto ValidDto(string isin = "BD0001234567") => new()
        {
            ISIN = isin, Name = "GOB 8% 2030",
            FaceValue = 100m, CouponRate = 0.08m,
            CouponFrequency = "SemiAnnual",
            IssueDate = new DateTime(2024, 1, 1),
            MaturityDate = new DateTime(2030, 1, 1),
            TotalIssueSize = 1_000_000m,
            Description = "Government bond"
        };

        private async Task<TBond> SeedBond(AppDbContext db, TBondStatus status = TBondStatus.Active,
            DateTime? maturity = null)
        {
            var bond = new TBond
            {
                ISIN = "BD" + Guid.NewGuid().ToString("N")[..10],
                Name = "Test Bond", FaceValue = 100m,
                CouponRate = 0.08m, CouponFrequency = CouponFrequency.SemiAnnual,
                IssueDate = DateTime.UtcNow.AddYears(-1),
                MaturityDate = maturity ?? DateTime.UtcNow.AddYears(5),
                TotalIssueSize = 1_000_000m, OutstandingSize = 1_000_000m,
                Status = status
            };
            db.TBonds.Add(bond);
            await db.SaveChangesAsync();
            return bond;
        }

        private async Task<TBondHolding> SeedHolding(AppDbContext db, int bondId, int investorId = 1,
            decimal faceHeld = 10000m)
        {
            var h = new TBondHolding
            {
                TBondId = bondId, InvestorId = investorId,
                BrokerageHouseId = 1, FaceValueHeld = faceHeld, AverageCost = 98m
            };
            db.TBondHoldings.Add(h);
            await db.SaveChangesAsync();
            return h;
        }

        // ── Create ───────────────────────────────────────────────────

        [Fact]
        public async Task Create_Valid_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.CreateAsync(ValidDto());
            Assert.True(result.IsSuccess);
            Assert.Equal("Active", result.Value!.Status);
        }

        [Fact]
        public async Task Create_MaturityBeforeIssue_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var dto = ValidDto();
            dto.MaturityDate = dto.IssueDate.AddDays(-1);
            var result = await svc.CreateAsync(dto);
            Assert.False(result.IsSuccess);
            Assert.Contains("MaturityDate", result.Error);
        }

        [Fact]
        public async Task Create_DuplicateISIN_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            await svc.CreateAsync(ValidDto("BD1234567890"));
            var result = await svc.CreateAsync(ValidDto("BD1234567890"));
            Assert.False(result.IsSuccess);
            Assert.Contains("ISIN", result.Error);
        }

        [Fact]
        public async Task Create_InvalidFrequency_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var dto = ValidDto(); dto.CouponFrequency = "WEEKLY";
            var result = await svc.CreateAsync(dto);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Create_OutstandingSizeEqualsTotalIssue()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.CreateAsync(ValidDto());
            Assert.Equal(result.Value!.TotalIssueSize, result.Value.OutstandingSize);
        }

        // ── GetAll ───────────────────────────────────────────────────

        [Fact]
        public async Task GetAll_FilterByStatus_ReturnsSubset()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            await SeedBond(db, TBondStatus.Active);
            await SeedBond(db, TBondStatus.Matured);
            var result = await svc.GetAllAsync("Active");
            Assert.Single(result.Value!);
        }

        [Fact]
        public async Task GetAll_NoFilter_ReturnsAll()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            await SeedBond(db); await SeedBond(db);
            var result = await svc.GetAllAsync(null);
            Assert.Equal(2, result.Value!.Count);
        }

        // ── PlaceOrder ───────────────────────────────────────────────

        [Fact]
        public async Task PlaceOrder_Buy_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var dto = new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 100m, Price = 98m };
            var result = await svc.PlaceOrderAsync(dto);
            Assert.True(result.IsSuccess);
            Assert.Equal("Pending", result.Value!.Status);
        }

        [Fact]
        public async Task PlaceOrder_InvalidSide_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var dto = new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Hold", Quantity = 100m, Price = 98m };
            var result = await svc.PlaceOrderAsync(dto);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task PlaceOrder_InactiveBond_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db, TBondStatus.Matured);
            var dto = new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 100m, Price = 98m };
            var result = await svc.PlaceOrderAsync(dto);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task PlaceOrder_TotalAmountCalculatedCorrectly()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db); // FaceValue=100
            var dto = new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 100m, Price = 98m };
            var result = await svc.PlaceOrderAsync(dto);
            // total = 100 * (98/100) * 100 = 9800
            Assert.Equal(9800m, result.Value!.TotalAmount);
        }

        // ── ExecuteOrder ─────────────────────────────────────────────

        [Fact]
        public async Task ExecuteOrder_Buy_CreatesHolding()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var dto = new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 100m, Price = 98m };
            var order = await svc.PlaceOrderAsync(dto);
            await svc.ExecuteOrderAsync(order.Value!.Id);
            Assert.True(await db.TBondHoldings.AnyAsync(h => h.InvestorId == 1 && h.TBondId == bond.Id));
        }

        [Fact]
        public async Task ExecuteOrder_Buy_AccumulatesHolding()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var o1 = await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 100m, Price = 98m });
            var o2 = await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 100m, Price = 102m });
            await svc.ExecuteOrderAsync(o1.Value!.Id);
            await svc.ExecuteOrderAsync(o2.Value!.Id);
            var holding = await db.TBondHoldings.FirstAsync(h => h.InvestorId == 1);
            Assert.Equal(200m, holding.FaceValueHeld);
            Assert.Equal(100m, holding.AverageCost); // (100*98 + 100*102)/200 = 100
        }

        [Fact]
        public async Task ExecuteOrder_Sell_DeductsHolding()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            await SeedHolding(db, bond.Id, investorId: 2, faceHeld: 200m);
            var dto = new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 2, BrokerageHouseId = 1, Side = "Sell", Quantity = 50m, Price = 99m };
            var order = await svc.PlaceOrderAsync(dto);
            await svc.ExecuteOrderAsync(order.Value!.Id);
            var holding = await db.TBondHoldings.FirstAsync(h => h.InvestorId == 2);
            Assert.Equal(150m, holding.FaceValueHeld);
        }

        [Fact]
        public async Task ExecuteOrder_Sell_InsufficientHolding_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            await SeedHolding(db, bond.Id, investorId: 3, faceHeld: 10m);
            var dto = new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 3, BrokerageHouseId = 1, Side = "Sell", Quantity = 100m, Price = 99m };
            var order = await svc.PlaceOrderAsync(dto);
            var result = await svc.ExecuteOrderAsync(order.Value!.Id);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ExecuteOrder_AlreadyExecuted_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var o = await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 10m, Price = 98m });
            await svc.ExecuteOrderAsync(o.Value!.Id);
            var result = await svc.ExecuteOrderAsync(o.Value.Id);
            Assert.False(result.IsSuccess);
        }

        // ── SettleOrder ──────────────────────────────────────────────

        [Fact]
        public async Task SettleOrder_Executed_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var o = await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 10m, Price = 98m });
            await svc.ExecuteOrderAsync(o.Value!.Id);
            var result = await svc.SettleOrderAsync(o.Value.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal("Settled", result.Value!.Status);
        }

        [Fact]
        public async Task SettleOrder_Pending_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var o = await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 10m, Price = 98m });
            var result = await svc.SettleOrderAsync(o.Value!.Id);
            Assert.False(result.IsSuccess);
        }

        // ── CancelOrder ──────────────────────────────────────────────

        [Fact]
        public async Task CancelOrder_Pending_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var o = await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 10m, Price = 98m });
            var result = await svc.CancelOrderAsync(o.Value!.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal("Cancelled", result.Value!.Status);
        }

        [Fact]
        public async Task CancelOrder_Executed_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var o = await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 10m, Price = 98m });
            await svc.ExecuteOrderAsync(o.Value!.Id);
            var result = await svc.CancelOrderAsync(o.Value.Id);
            Assert.False(result.IsSuccess);
        }

        // ── Coupons ──────────────────────────────────────────────────

        [Fact]
        public async Task GenerateCoupons_SemiAnnual_CreatesCorrectPeriods()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            bond.IssueDate = new DateTime(2024, 1, 1);
            bond.MaturityDate = new DateTime(2025, 1, 1); // 1 year = 2 semi-annual periods
            await db.SaveChangesAsync();
            await SeedHolding(db, bond.Id);

            var result = await svc.GenerateCouponsAsync(bond.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task GenerateCoupons_CouponAmountCorrect()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            bond.IssueDate    = new DateTime(2024, 1, 1);
            bond.MaturityDate = new DateTime(2024, 7, 1); // 1 period
            bond.CouponRate   = 0.08m;
            await db.SaveChangesAsync();
            await SeedHolding(db, bond.Id, faceHeld: 10000m);

            var result = await svc.GenerateCouponsAsync(bond.Id);
            Assert.True(result.IsSuccess);
            // periodRate = 0.08/2 = 0.04, coupon = 10000 * 0.04 = 400
            Assert.Equal(400m, result.Value![0].CouponAmount);
        }

        [Fact]
        public async Task GenerateCoupons_NoHoldings_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            var result = await svc.GenerateCouponsAsync(bond.Id);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task GenerateCoupons_Idempotent_NoDuplicates()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            bond.IssueDate    = new DateTime(2024, 1, 1);
            bond.MaturityDate = new DateTime(2024, 7, 1);
            await db.SaveChangesAsync();
            await SeedHolding(db, bond.Id);

            await svc.GenerateCouponsAsync(bond.Id);
            await svc.GenerateCouponsAsync(bond.Id); // second call — no duplicates
            Assert.Equal(1, await db.CouponPayments.CountAsync());
        }

        [Fact]
        public async Task PayCoupons_MarksAsPaid()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            bond.IssueDate    = new DateTime(2024, 1, 1);
            bond.MaturityDate = new DateTime(2025, 1, 1);
            await db.SaveChangesAsync();
            await SeedHolding(db, bond.Id);
            await svc.GenerateCouponsAsync(bond.Id);

            var result = await svc.PayCouponsAsync(bond.Id, DateTime.UtcNow.AddYears(10));
            Assert.True(result.IsSuccess);
            Assert.True(result.Value > 0);
            Assert.All(await db.CouponPayments.ToListAsync(), cp => Assert.True(cp.IsPaid));
        }

        [Fact]
        public async Task PayCoupons_OnlyPaysUpToDate()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            bond.IssueDate    = new DateTime(2024, 1, 1);
            bond.MaturityDate = new DateTime(2025, 1, 1);
            await db.SaveChangesAsync();
            await SeedHolding(db, bond.Id);
            await svc.GenerateCouponsAsync(bond.Id);

            // Pay only coupons up to July 2024 (first period)
            var result = await svc.PayCouponsAsync(bond.Id, new DateTime(2024, 7, 1));
            Assert.Equal(1, result.Value);
        }

        // ── ProcessMaturity ───────────────────────────────────────────

        [Fact]
        public async Task ProcessMaturity_MaturedBond_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db, maturity: DateTime.UtcNow.AddDays(-1));
            await SeedHolding(db, bond.Id, faceHeld: 5000m);

            var result = await svc.ProcessMaturityAsync(bond.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(1, result.Value!.HoldingsSettled);
            Assert.Equal(TBondStatus.Matured, (await db.TBonds.FindAsync(bond.Id))!.Status);
        }

        [Fact]
        public async Task ProcessMaturity_NotYetMatured_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db, maturity: DateTime.UtcNow.AddYears(5));
            var result = await svc.ProcessMaturityAsync(bond.Id);
            Assert.False(result.IsSuccess);
            Assert.Contains("not yet matured", result.Error);
        }

        [Fact]
        public async Task ProcessMaturity_HoldingsFaceValueZeroedOut()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db, maturity: DateTime.UtcNow.AddDays(-1));
            var holding = await SeedHolding(db, bond.Id, faceHeld: 1000m);

            await svc.ProcessMaturityAsync(bond.Id);
            var updated = await db.TBondHoldings.FindAsync(holding.Id);
            Assert.Equal(0m, updated!.FaceValueHeld);
        }

        [Fact]
        public async Task ProcessMaturity_AlreadyMatured_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db, TBondStatus.Matured, maturity: DateTime.UtcNow.AddDays(-1));
            var result = await svc.ProcessMaturityAsync(bond.Id);
            Assert.False(result.IsSuccess);
        }

        // ── GetHoldings ───────────────────────────────────────────────

        [Fact]
        public async Task GetHoldings_ReturnsForInvestor()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var b1 = await SeedBond(db); var b2 = await SeedBond(db);
            await SeedHolding(db, b1.Id, investorId: 10);
            await SeedHolding(db, b2.Id, investorId: 10);
            await SeedHolding(db, b1.Id, investorId: 99); // different investor

            var result = await svc.GetHoldingsAsync(10);
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
            Assert.All(result.Value, h => Assert.Equal(10, h.InvestorId));
        }

        [Fact]
        public async Task GetHoldings_Empty_ReturnsEmptyList()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.GetHoldingsAsync(999);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Value!);
        }

        // ── GetOrders filter ──────────────────────────────────────────

        [Fact]
        public async Task GetOrders_FilterByInvestor_ReturnsSubset()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var bond = await SeedBond(db);
            await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 1, BrokerageHouseId = 1, Side = "Buy", Quantity = 10m, Price = 98m });
            await svc.PlaceOrderAsync(new PlaceTBondOrderDto { TBondId = bond.Id, InvestorId = 2, BrokerageHouseId = 1, Side = "Buy", Quantity = 10m, Price = 98m });

            var result = await svc.GetOrdersAsync(investorId: 1, null);
            Assert.Single(result.Value!);
        }
    }
}
