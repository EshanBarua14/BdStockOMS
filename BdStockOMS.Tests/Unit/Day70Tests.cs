using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BdStockOMS.Tests.Unit;

// ============================================================
//  Day 70 — FundRequest Entity + Workflow Tests
// ============================================================

public class Day70FundRequestTests
{
    private AppDbContext CreateDb()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts);
    }

    private void SeedBrokerage(AppDbContext db, int id = 1)
    {
        db.BrokerageHouses.Add(new BrokerageHouse { Id=id, Name="Pioneer",
            LicenseNumber="DSE-TM-0001", Email="info@pioneer.com",
            Phone="01700000000", Address="Dhaka", IsActive=true, CreatedAt=DateTime.UtcNow });
        db.SaveChanges();
    }

    private void SeedUser(AppDbContext db, int id = 1, int brokerageId = 1)
    {
        db.Users.Add(new User { Id=id, FullName="Test Investor", Email="inv@test.com",
            PasswordHash="hash", BrokerageHouseId=brokerageId,
            IsActive=true, CreatedAt=DateTime.UtcNow });
        db.SaveChanges();
    }

    private FundRequest MakeRequest(int investorId=1, int brokerageId=1,
        decimal amount=50000m, PaymentMethod method=PaymentMethod.BEFTN)
    => new FundRequest {
        InvestorId=investorId, BrokerageHouseId=brokerageId,
        Amount=amount, PaymentMethod=method,
        Status=FundRequestStatus.Pending, CreatedAt=DateTime.UtcNow
    };

    // ── Entity defaults ──────────────────────────────────────

    [Fact]
    public void FundRequest_DefaultStatus_IsPending()
        => Assert.Equal(FundRequestStatus.Pending, new FundRequest().Status);

    [Fact]
    public void FundRequestStatus_Completed_IsFour()
        => Assert.Equal(4, (int)FundRequestStatus.Completed);

    [Fact]
    public void FundRequestStatus_Rejected_IsThree()
        => Assert.Equal(3, (int)FundRequestStatus.Rejected);

    [Fact]
    public void PaymentMethod_Cash_IsZero()
        => Assert.Equal(0, (int)PaymentMethod.Cash);

    [Fact]
    public void PaymentMethod_BEFTN_IsTwo()
        => Assert.Equal(2, (int)PaymentMethod.BEFTN);

    [Fact]
    public void PaymentMethod_bKash_IsFour()
        => Assert.Equal(4, (int)PaymentMethod.bKash);

    // ── DB persistence ───────────────────────────────────────

    [Fact]
    public async Task FundRequest_CanBeSaved()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        db.FundRequests.Add(MakeRequest());
        await db.SaveChangesAsync();
        Assert.Equal(1, db.FundRequests.Count());
    }

    [Fact]
    public async Task FundRequest_Amount_Persists()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        db.FundRequests.Add(MakeRequest(amount: 75000m));
        await db.SaveChangesAsync();
        Assert.Equal(75000m, db.FundRequests.First().Amount);
    }

    [Fact]
    public async Task FundRequest_PaymentMethod_Persists()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        db.FundRequests.Add(MakeRequest(method: PaymentMethod.bKash));
        await db.SaveChangesAsync();
        Assert.Equal(PaymentMethod.bKash, db.FundRequests.First().PaymentMethod);
    }

    // ── Workflow state transitions ────────────────────────────

    [Fact]
    public async Task FundRequest_ApproveByTrader_UpdatesStatus()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        var req = MakeRequest();
        db.FundRequests.Add(req);
        await db.SaveChangesAsync();
        req.Status = FundRequestStatus.ApprovedByTrader;
        req.TraderId = 1;
        req.ApprovedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.Equal(FundRequestStatus.ApprovedByTrader, db.FundRequests.First().Status);
    }

    [Fact]
    public async Task FundRequest_ApproveByCCD_UpdatesStatus()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        var req = MakeRequest();
        req.Status = FundRequestStatus.ApprovedByTrader;
        db.FundRequests.Add(req);
        await db.SaveChangesAsync();
        req.Status = FundRequestStatus.ApprovedByCCD;
        req.CCDUserId = 1;
        await db.SaveChangesAsync();
        Assert.Equal(FundRequestStatus.ApprovedByCCD, db.FundRequests.First().Status);
    }

    [Fact]
    public async Task FundRequest_Complete_SetsCompletedAt()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        var req = MakeRequest();
        req.Status = FundRequestStatus.ApprovedByCCD;
        db.FundRequests.Add(req);
        await db.SaveChangesAsync();
        req.Status = FundRequestStatus.Completed;
        req.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        Assert.NotNull(db.FundRequests.First().CompletedAt);
    }

    [Fact]
    public async Task FundRequest_Reject_SetsRejectionReason()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        var req = MakeRequest();
        db.FundRequests.Add(req);
        await db.SaveChangesAsync();
        req.Status = FundRequestStatus.Rejected;
        req.RejectionReason = "Insufficient documentation";
        await db.SaveChangesAsync();
        Assert.Equal("Insufficient documentation", db.FundRequests.First().RejectionReason);
    }

    [Fact]
    public async Task FundRequest_FilterByStatus_ReturnsPending()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        var r1 = MakeRequest(); r1.Status = FundRequestStatus.Pending;
        var r2 = MakeRequest(); r2.Status = FundRequestStatus.Completed;
        var r3 = MakeRequest(); r3.Status = FundRequestStatus.Rejected;
        db.FundRequests.AddRange(r1, r2, r3);
        await db.SaveChangesAsync();
        var pending = db.FundRequests.Where(r => r.Status == FundRequestStatus.Pending).ToList();
        Assert.Single(pending);
    }

    [Fact]
    public async Task FundRequest_FilterByBrokerage_ReturnsCorrect()
    {
        var db = CreateDb();
        SeedBrokerage(db, 1); SeedBrokerage(db, 2);
        SeedUser(db, 1, 1); SeedUser(db, 2, 2);
        db.FundRequests.Add(MakeRequest(investorId:1, brokerageId:1));
        db.FundRequests.Add(MakeRequest(investorId:2, brokerageId:2));
        await db.SaveChangesAsync();
        Assert.Single(db.FundRequests.Where(r => r.BrokerageHouseId == 1));
    }

    [Fact]
    public async Task FundRequest_ReferenceNumber_IsOptional()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        var req = MakeRequest(); req.ReferenceNumber = null;
        db.FundRequests.Add(req);
        await db.SaveChangesAsync();
        Assert.Null(db.FundRequests.First().ReferenceNumber);
    }

    [Fact]
    public async Task FundRequest_MultipleRequests_AllPersist()
    {
        var db = CreateDb();
        SeedBrokerage(db); SeedUser(db);
        db.FundRequests.AddRange(MakeRequest(), MakeRequest(), MakeRequest());
        await db.SaveChangesAsync();
        Assert.Equal(3, db.FundRequests.Count());
    }
}