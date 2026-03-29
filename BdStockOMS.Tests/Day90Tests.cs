using System;
using System.Linq;
using System.Threading.Tasks;
using BdStockOMS.API.Data;
using BdStockOMS.API.DTOs.IPO;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests
{
    public class Day90IPOTests
    {
        private AppDbContext CreateDb() =>
            new(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        private IPOService CreateSvc(AppDbContext db) => new(db);

        private IPO MakeIPO(AppDbContext db, IPOStatus status = IPOStatus.Open,
            int totalShares = 1000, decimal offerPrice = 100m)
        {
            var ipo = new IPO
            {
                StockId = 1, CompanyName = "Test Corp", TradingCode = "TST",
                OfferPrice = offerPrice, TotalShares = totalShares,
                SharesRemaining = totalShares, MinInvestment = 1000m,
                MaxInvestment = 500000m, OpenDate = DateTime.UtcNow.AddDays(-1),
                CloseDate = DateTime.UtcNow.AddDays(7), Status = status
            };
            db.IPOs.Add(ipo);
            return ipo;
        }

        private ApplyIPODto MakeApplyDto(int ipoId, int investorId = 1, int shares = 10) =>
            new() { IPOId = ipoId, InvestorId = investorId, BrokerageHouseId = 1, AppliedShares = shares };

        // ── CreateIPO ────────────────────────────────────────────────

        [Fact]
        public async Task CreateIPO_Valid_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var dto = new CreateIPODto
            {
                StockId = 1, CompanyName = "ACME", TradingCode = "ACM",
                OfferPrice = 50m, TotalShares = 1000,
                MinInvestment = 500m, MaxInvestment = 100000m,
                OpenDate = DateTime.UtcNow, CloseDate = DateTime.UtcNow.AddDays(7)
            };
            var result = await svc.CreateIPOAsync(dto);
            Assert.True(result.IsSuccess);
            Assert.Equal("Upcoming", result.Value!.Status);
            Assert.Equal(1000, result.Value.SharesRemaining);
        }

        [Fact]
        public async Task CreateIPO_CloseDateBeforeOpenDate_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var dto = new CreateIPODto
            {
                StockId = 1, CompanyName = "X", TradingCode = "X",
                OfferPrice = 10m, TotalShares = 100,
                MinInvestment = 100m, MaxInvestment = 1000m,
                OpenDate = DateTime.UtcNow.AddDays(7),
                CloseDate = DateTime.UtcNow
            };
            var result = await svc.CreateIPOAsync(dto);
            Assert.False(result.IsSuccess);
            Assert.Contains("CloseDate", result.Error);
        }

        [Fact]
        public async Task CreateIPO_MaxLessThanMin_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var dto = new CreateIPODto
            {
                StockId = 1, CompanyName = "X", TradingCode = "X",
                OfferPrice = 10m, TotalShares = 100,
                MinInvestment = 5000m, MaxInvestment = 1000m,
                OpenDate = DateTime.UtcNow, CloseDate = DateTime.UtcNow.AddDays(7)
            };
            var result = await svc.CreateIPOAsync(dto);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task CreateIPO_SharesRemainingEqualsTotalShares()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var dto = new CreateIPODto
            {
                StockId = 1, CompanyName = "X", TradingCode = "X",
                OfferPrice = 10m, TotalShares = 500,
                MinInvestment = 100m, MaxInvestment = 50000m,
                OpenDate = DateTime.UtcNow, CloseDate = DateTime.UtcNow.AddDays(7)
            };
            var result = await svc.CreateIPOAsync(dto);
            Assert.Equal(500, result.Value!.TotalShares);
            Assert.Equal(500, result.Value.SharesRemaining);
        }

        // ── GetIPO ───────────────────────────────────────────────────

        [Fact]
        public async Task GetIPO_Exists_ReturnsDto()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db); await db.SaveChangesAsync();
            var result = await svc.GetIPOAsync(ipo.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(ipo.Id, result.Value!.Id);
        }

        [Fact]
        public async Task GetIPO_NotFound_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.GetIPOAsync(9999);
            Assert.False(result.IsSuccess);
        }

        // ── GetAllIPOs ───────────────────────────────────────────────

        [Fact]
        public async Task GetAllIPOs_NoFilter_ReturnsAll()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeIPO(db, IPOStatus.Open); MakeIPO(db, IPOStatus.Closed);
            await db.SaveChangesAsync();
            var result = await svc.GetAllIPOsAsync(null);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task GetAllIPOs_FilterByStatus_ReturnsSubset()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            MakeIPO(db, IPOStatus.Open); MakeIPO(db, IPOStatus.Closed);
            await db.SaveChangesAsync();
            var result = await svc.GetAllIPOsAsync("Open");
            Assert.Single(result.Value!);
            Assert.All(result.Value!, r => Assert.Equal("Open", r.Status));
        }

        // ── CloseIPO ─────────────────────────────────────────────────

        [Fact]
        public async Task CloseIPO_OpenIPO_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            var result = await svc.CloseIPOAsync(ipo.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(IPOStatus.Closed, (await db.IPOs.FindAsync(ipo.Id))!.Status);
        }

        [Fact]
        public async Task CloseIPO_AlreadyClosed_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed); await db.SaveChangesAsync();
            var result = await svc.CloseIPOAsync(ipo.Id);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task CloseIPO_NotFound_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.CloseIPOAsync(9999);
            Assert.False(result.IsSuccess);
        }

        // ── Apply ────────────────────────────────────────────────────

        [Fact]
        public async Task Apply_ValidApplication_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            var result = await svc.ApplyAsync(MakeApplyDto(ipo.Id, shares: 10));
            Assert.True(result.IsSuccess);
            Assert.Equal(1000m, result.Value!.AppliedAmount); // 10 * 100
            Assert.Equal("Pending", result.Value.Status);
        }

        [Fact]
        public async Task Apply_IPONotOpen_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed); await db.SaveChangesAsync();
            var result = await svc.ApplyAsync(MakeApplyDto(ipo.Id));
            Assert.False(result.IsSuccess);
            Assert.Contains("not open", result.Error);
        }

        [Fact]
        public async Task Apply_BelowMinInvestment_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            // 1 share * 100 = 100, but minInvestment = 1000
            var result = await svc.ApplyAsync(MakeApplyDto(ipo.Id, shares: 1));
            Assert.False(result.IsSuccess);
            Assert.Contains("minimum", result.Error);
        }

        [Fact]
        public async Task Apply_AboveMaxInvestment_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            // 6000 shares * 100 = 600000, max = 500000
            var result = await svc.ApplyAsync(MakeApplyDto(ipo.Id, shares: 6000));
            Assert.False(result.IsSuccess);
            Assert.Contains("maximum", result.Error);
        }

        [Fact]
        public async Task Apply_DuplicateApplication_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            await svc.ApplyAsync(MakeApplyDto(ipo.Id, investorId: 1, shares: 10));
            var result = await svc.ApplyAsync(MakeApplyDto(ipo.Id, investorId: 1, shares: 10));
            Assert.False(result.IsSuccess);
            Assert.Contains("already applied", result.Error);
        }

        [Fact]
        public async Task Apply_DifferentInvestors_BothSucceed()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            var r1 = await svc.ApplyAsync(MakeApplyDto(ipo.Id, investorId: 1, shares: 10));
            var r2 = await svc.ApplyAsync(MakeApplyDto(ipo.Id, investorId: 2, shares: 20));
            Assert.True(r1.IsSuccess);
            Assert.True(r2.IsSuccess);
        }

        // ── AllocateAsync ─────────────────────────────────────────────

        [Fact]
        public async Task Allocate_ExactSubscription_AllGetFullAllocation()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed, totalShares: 100);
            await db.SaveChangesAsync();
            // investor applies for all 100 shares
            db.IPOApplications.Add(new IPOApplication { IPOId = ipo.Id, InvestorId = 1, BrokerageHouseId = 1, AppliedShares = 100, AppliedAmount = 10000m, Status = IPOApplicationStatus.Pending });
            await db.SaveChangesAsync();

            var result = await svc.AllocateAsync(ipo.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(100, (await db.IPOApplications.FirstAsync()).AllocatedShares);
            Assert.False(result.Value!.IsOversubscribed);
        }

        [Fact]
        public async Task Allocate_Oversubscribed_ProRataAllocation()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed, totalShares: 100);
            await db.SaveChangesAsync();
            // 2 investors each apply for 100 shares = 200 total, only 100 available
            db.IPOApplications.Add(new IPOApplication { IPOId = ipo.Id, InvestorId = 1, BrokerageHouseId = 1, AppliedShares = 100, AppliedAmount = 10000m, Status = IPOApplicationStatus.Pending });
            db.IPOApplications.Add(new IPOApplication { IPOId = ipo.Id, InvestorId = 2, BrokerageHouseId = 1, AppliedShares = 100, AppliedAmount = 10000m, Status = IPOApplicationStatus.Pending });
            await db.SaveChangesAsync();

            var result = await svc.AllocateAsync(ipo.Id);
            Assert.True(result.IsSuccess);
            Assert.True(result.Value!.IsOversubscribed);
            Assert.Equal(0.5m, result.Value.SubscriptionRatio);
            var apps = await db.IPOApplications.ToListAsync();
            Assert.All(apps, a => Assert.Equal(50, a.AllocatedShares));
        }

        [Fact]
        public async Task Allocate_SetsIPOStatusToAllocated()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed, totalShares: 100);
            await db.SaveChangesAsync();
            db.IPOApplications.Add(new IPOApplication { IPOId = ipo.Id, InvestorId = 1, BrokerageHouseId = 1, AppliedShares = 50, AppliedAmount = 5000m, Status = IPOApplicationStatus.Pending });
            await db.SaveChangesAsync();

            await svc.AllocateAsync(ipo.Id);
            Assert.Equal(IPOStatus.Allocated, (await db.IPOs.FindAsync(ipo.Id))!.Status);
        }

        [Fact]
        public async Task Allocate_NotClosed_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            var result = await svc.AllocateAsync(ipo.Id);
            Assert.False(result.IsSuccess);
            Assert.Contains("closed", result.Error);
        }

        [Fact]
        public async Task Allocate_NoApplications_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed); await db.SaveChangesAsync();
            var result = await svc.AllocateAsync(ipo.Id);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task Allocate_RefundCalculated_ForOversubscription()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed, totalShares: 100, offerPrice: 100m);
            await db.SaveChangesAsync();
            db.IPOApplications.Add(new IPOApplication { IPOId = ipo.Id, InvestorId = 1, BrokerageHouseId = 1, AppliedShares = 200, AppliedAmount = 20000m, Status = IPOApplicationStatus.Pending });
            await db.SaveChangesAsync();

            var result = await svc.AllocateAsync(ipo.Id);
            Assert.Equal(10000m, result.Value!.TotalRefundAmount); // allocated 100*100=10000, applied 20000, refund 10000
        }

        // ── ProcessRefunds ────────────────────────────────────────────

        [Fact]
        public async Task ProcessRefunds_SetsRefundedAt()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed, totalShares: 100, offerPrice: 100m);
            await db.SaveChangesAsync();
            db.IPOApplications.Add(new IPOApplication { IPOId = ipo.Id, InvestorId = 1, BrokerageHouseId = 1, AppliedShares = 200, AppliedAmount = 20000m, Status = IPOApplicationStatus.Pending });
            await db.SaveChangesAsync();
            await svc.AllocateAsync(ipo.Id);

            var result = await svc.ProcessRefundsAsync(ipo.Id);
            Assert.True(result.IsSuccess);
            Assert.True(result.Value > 0);
            var app = await db.IPOApplications.FirstAsync();
            Assert.NotNull(app.RefundedAt);
        }

        [Fact]
        public async Task ProcessRefunds_NotAllocated_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed); await db.SaveChangesAsync();
            var result = await svc.ProcessRefundsAsync(ipo.Id);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ProcessRefunds_SetsIPOStatusToRefunded()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Closed, totalShares: 100, offerPrice: 100m);
            await db.SaveChangesAsync();
            db.IPOApplications.Add(new IPOApplication { IPOId = ipo.Id, InvestorId = 1, BrokerageHouseId = 1, AppliedShares = 200, AppliedAmount = 20000m, Status = IPOApplicationStatus.Pending });
            await db.SaveChangesAsync();
            await svc.AllocateAsync(ipo.Id);
            await svc.ProcessRefundsAsync(ipo.Id);
            Assert.Equal(IPOStatus.Refunded, (await db.IPOs.FindAsync(ipo.Id))!.Status);
        }

        // ── GetApplications ───────────────────────────────────────────

        [Fact]
        public async Task GetApplications_ReturnsAllForIPO()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            await svc.ApplyAsync(MakeApplyDto(ipo.Id, investorId: 1, shares: 10));
            await svc.ApplyAsync(MakeApplyDto(ipo.Id, investorId: 2, shares: 20));
            var result = await svc.GetApplicationsAsync(ipo.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(2, result.Value!.Count);
        }

        [Fact]
        public async Task GetApplications_IPONotFound_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.GetApplicationsAsync(9999);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task GetApplication_ById_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var ipo = MakeIPO(db, IPOStatus.Open); await db.SaveChangesAsync();
            var app = await svc.ApplyAsync(MakeApplyDto(ipo.Id, shares: 10));
            var result = await svc.GetApplicationAsync(app.Value!.Id);
            Assert.True(result.IsSuccess);
            Assert.Equal(app.Value.Id, result.Value!.Id);
        }

        [Fact]
        public async Task GetApplication_NotFound_Fails()
        {
            var db = CreateDb(); var svc = CreateSvc(db);
            var result = await svc.GetApplicationAsync(9999);
            Assert.False(result.IsSuccess);
        }

        // ── Full workflow ─────────────────────────────────────────────

        [Fact]
        public async Task FullWorkflow_CreateApplyCloseAllocateRefund_Succeeds()
        {
            var db = CreateDb(); var svc = CreateSvc(db);

            // Create
            var createDto = new CreateIPODto
            {
                StockId = 1, CompanyName = "Flow Corp", TradingCode = "FLW",
                OfferPrice = 50m, TotalShares = 200,
                MinInvestment = 500m, MaxInvestment = 50000m,
                OpenDate = DateTime.UtcNow.AddDays(-1),
                CloseDate = DateTime.UtcNow.AddDays(7)
            };
            var created = await svc.CreateIPOAsync(createDto);
            var ipoId = created.Value!.Id;

            // Open it manually
            var ipo = await db.IPOs.FindAsync(ipoId);
            ipo!.Status = IPOStatus.Open;
            await db.SaveChangesAsync();

            // Apply (300 total = oversubscribed by 100)
            await svc.ApplyAsync(new ApplyIPODto { IPOId = ipoId, InvestorId = 1, BrokerageHouseId = 1, AppliedShares = 150 });
            await svc.ApplyAsync(new ApplyIPODto { IPOId = ipoId, InvestorId = 2, BrokerageHouseId = 1, AppliedShares = 150 });

            // Close
            await svc.CloseIPOAsync(ipoId);

            // Allocate
            var allocation = await svc.AllocateAsync(ipoId);
            Assert.True(allocation.IsSuccess);
            Assert.True(allocation.Value!.IsOversubscribed);

            // Refund
            var refunds = await svc.ProcessRefundsAsync(ipoId);
            Assert.True(refunds.IsSuccess);
            Assert.Equal(IPOStatus.Refunded, (await db.IPOs.FindAsync(ipoId))!.Status);
        }
    }
}
