using BdStockOMS.API.Data;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class FundRequestServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private FundRequestService CreateService(AppDbContext db)
    {
        var auditMock = new Mock<IAuditService>();
        auditMock.Setup(x => x.LogAsync(
            It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
            It.IsAny<string?>())).Returns(Task.CompletedTask);
        return new FundRequestService(db, auditMock.Object);
    }

    private async Task<User> SeedDataAsync(AppDbContext db)
    {
        db.Roles.Add(new Role { Id = 1, Name = "Investor" });
        db.Roles.Add(new Role { Id = 2, Name = "Trader" });
        db.Roles.Add(new Role { Id = 3, Name = "CCD" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test Brokerage", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        var investor = new User
        {
            Id = 1, FullName = "Investor", Email = "investor@test.com",
            PasswordHash = "hash", Phone = "01700000000",
            RoleId = 1, BrokerageHouseId = 1, CashBalance = 0m
        };
        var trader = new User
        {
            Id = 2, FullName = "Trader", Email = "trader@test.com",
            PasswordHash = "hash", Phone = "01700000001",
            RoleId = 2, BrokerageHouseId = 1
        };
        var ccd = new User
        {
            Id = 3, FullName = "CCD User", Email = "ccd@test.com",
            PasswordHash = "hash", Phone = "01700000002",
            RoleId = 3, BrokerageHouseId = 1
        };
        db.Users.AddRange(investor, trader, ccd);
        await db.SaveChangesAsync();
        return investor;
    }

    // ── CREATE REQUEST ────────────────────────────────────────

    [Fact]
    public async Task CreateRequest_ValidAmount_Succeeds()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.BEFTN, "REF001", "Test deposit", 1);

        Assert.True(result.IsSuccess);
        Assert.Equal(FundRequestStatus.Pending, result.Value!.Status);
        Assert.Equal(50000m, result.Value.Amount);
    }

    [Fact]
    public async Task CreateRequest_ZeroAmount_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CreateRequestAsync(
            1, 0m, PaymentMethod.Cash, null, null, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_AMOUNT", result.ErrorCode);
    }

    [Fact]
    public async Task CreateRequest_ExceedsLimit_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var result = await svc.CreateRequestAsync(
            1, 11_000_000m, PaymentMethod.BEFTN, null, null, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal("AMOUNT_EXCEEDS_LIMIT", result.ErrorCode);
    }

    [Fact]
    public async Task CreateRequest_PendingExists_ReturnsFailure()
    {
        var db = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        await svc.CreateRequestAsync(1, 50000m, PaymentMethod.Cash, null, null, 1);
        var result = await svc.CreateRequestAsync(
            1, 30000m, PaymentMethod.Cash, null, null, 1);

        Assert.False(result.IsSuccess);
        Assert.Equal("PENDING_REQUEST_EXISTS", result.ErrorCode);
    }

    // ── TRADER APPROVAL ───────────────────────────────────────

    [Fact]
    public async Task ApproveByTrader_PendingRequest_Succeeds()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.BEFTN, null, null, 1);
        var result = await svc.ApproveByTraderAsync(created.Value!.Id, 2, null);

        Assert.True(result.IsSuccess);
        var request = await db.FundRequests.FindAsync(created.Value.Id);
        Assert.Equal(FundRequestStatus.ApprovedByTrader, request!.Status);
    }

    [Fact]
    public async Task ApproveByTrader_AlreadyApproved_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.BEFTN, null, null, 1);
        await svc.ApproveByTraderAsync(created.Value!.Id, 2, null);
        var result = await svc.ApproveByTraderAsync(created.Value.Id, 2, null);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_STATUS", result.ErrorCode);
    }

    // ── CCD APPROVAL ──────────────────────────────────────────

    [Fact]
    public async Task ApproveByCCD_TraderApproved_Succeeds()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.BEFTN, null, null, 1);
        await svc.ApproveByTraderAsync(created.Value!.Id, 2, null);
        var result = await svc.ApproveByCCDAsync(created.Value.Id, 3);

        Assert.True(result.IsSuccess);
        var request = await db.FundRequests.FindAsync(created.Value.Id);
        Assert.Equal(FundRequestStatus.ApprovedByCCD, request!.Status);
    }

    [Fact]
    public async Task ApproveByCCD_NotTraderApproved_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.BEFTN, null, null, 1);
        var result = await svc.ApproveByCCDAsync(created.Value!.Id, 3);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_STATUS", result.ErrorCode);
    }

    // ── REJECT ────────────────────────────────────────────────

    [Fact]
    public async Task Reject_PendingRequest_Succeeds()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.Cash, null, null, 1);
        var result = await svc.RejectAsync(created.Value!.Id, 2, "Invalid reference");

        Assert.True(result.IsSuccess);
        var request = await db.FundRequests.FindAsync(created.Value.Id);
        Assert.Equal(FundRequestStatus.Rejected, request!.Status);
        Assert.Equal("Invalid reference", request.RejectionReason);
    }

    [Fact]
    public async Task Reject_NoReason_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.Cash, null, null, 1);
        var result = await svc.RejectAsync(created.Value!.Id, 2, "");

        Assert.False(result.IsSuccess);
        Assert.Equal("REASON_REQUIRED", result.ErrorCode);
    }

    // ── COMPLETE ──────────────────────────────────────────────

    [Fact]
    public async Task Complete_CCDApproved_CreditsCashBalance()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.BEFTN, null, null, 1);
        await svc.ApproveByTraderAsync(created.Value!.Id, 2, null);
        await svc.ApproveByCCDAsync(created.Value.Id, 3);
        var result = await svc.CompleteAsync(created.Value.Id, 3);

        Assert.True(result.IsSuccess);
        var investor = await db.Users.FindAsync(1);
        Assert.Equal(50000m, investor!.CashBalance);
    }

    [Fact]
    public async Task Complete_NotCCDApproved_ReturnsFailure()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        var created = await svc.CreateRequestAsync(
            1, 50000m, PaymentMethod.BEFTN, null, null, 1);
        await svc.ApproveByTraderAsync(created.Value!.Id, 2, null);
        var result = await svc.CompleteAsync(created.Value.Id, 3);

        Assert.False(result.IsSuccess);
        Assert.Equal("INVALID_STATUS", result.ErrorCode);
    }

    // ── PAGINATION ────────────────────────────────────────────

    [Fact]
    public async Task GetMyRequests_ReturnsPagedResult()
    {
        var db  = CreateDb();
        await SeedDataAsync(db);
        var svc = CreateService(db);

        await svc.CreateRequestAsync(1, 10000m, PaymentMethod.Cash, null, null, 1);

        var result = await svc.GetMyRequestsAsync(1, 1, 10);
        Assert.Equal(1, result.TotalCount);
        Assert.Single(result.Items);
    }
}
