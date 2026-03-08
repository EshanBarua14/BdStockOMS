using BdStockOMS.API.Data;
using BdStockOMS.API.Hubs;
using BdStockOMS.API.Models;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace BdStockOMS.Tests.Unit;

public class NotificationServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private NotificationService CreateService(AppDbContext db)
    {
        var mockClients = new Mock<IHubClients>();
        var mockClient  = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClient.Object);
        mockClient.Setup(c => c.SendCoreAsync(
            It.IsAny<string>(), It.IsAny<object[]>(),
            It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var mockHub = new Mock<IHubContext<NotificationHub>>();
        mockHub.Setup(h => h.Clients).Returns(mockClients.Object);

        return new NotificationService(db, mockHub.Object);
    }

    private async Task SeedUserAsync(AppDbContext db)
    {
        db.Roles.Add(new Role { Id = 1, Name = "Investor" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        db.Users.Add(new User
        {
            Id = 1, FullName = "Test User", Email = "test@test.com",
            PasswordHash = "hash", Phone = "01700000000",
            RoleId = 1, BrokerageHouseId = 1
        });
        await db.SaveChangesAsync();
    }

    // ── CREATE ────────────────────────────────────────────────

    [Fact]
    public async Task Create_ValidNotification_SavedToDb()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var result = await svc.CreateAsync(
            1, NotificationType.OrderExecuted,
            "Order Executed", "Your order was filled.");

        Assert.NotNull(result);
        Assert.Equal(1, result.UserId);
        Assert.False(result.IsRead);
        Assert.Equal(NotificationType.OrderExecuted, result.Type);
    }

    [Fact]
    public async Task Create_MultipleUsers_CreatesForEach()
    {
        var db = CreateDb();
        db.Roles.Add(new Role { Id = 1, Name = "Investor" });
        db.BrokerageHouses.Add(new BrokerageHouse
        {
            Id = 1, Name = "Test", LicenseNumber = "L1",
            Email = "b@b.com", Phone = "0100", Address = "Dhaka", IsActive = true
        });
        db.Users.AddRange(
            new User { Id = 1, FullName = "U1", Email = "u1@test.com",
                PasswordHash = "h", Phone = "01700000001", RoleId = 1, BrokerageHouseId = 1 },
            new User { Id = 2, FullName = "U2", Email = "u2@test.com",
                PasswordHash = "h", Phone = "01700000002", RoleId = 1, BrokerageHouseId = 1 }
        );
        await db.SaveChangesAsync();
        var svc = CreateService(db);

        await svc.CreateForMultipleUsersAsync(
            new[] { 1, 2 }, NotificationType.SystemAlert,
            "System Alert", "Maintenance at midnight.");

        var count = await db.Notifications.CountAsync();
        Assert.Equal(2, count);
    }

    // ── MARK AS READ ──────────────────────────────────────────

    [Fact]
    public async Task MarkAsRead_ExistingNotification_MarksRead()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var notif = await svc.CreateAsync(
            1, NotificationType.OrderPlaced, "Order Placed", "Buy order placed.");

        var result = await svc.MarkAsReadAsync(notif.Id, 1);

        Assert.True(result.IsSuccess);
        var updated = await db.Notifications.FindAsync(notif.Id);
        Assert.True(updated!.IsRead);
        Assert.NotNull(updated.ReadAt);
    }

    [Fact]
    public async Task MarkAsRead_WrongUser_ReturnsNotFound()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var notif = await svc.CreateAsync(
            1, NotificationType.OrderPlaced, "Order Placed", "Buy order placed.");

        var result = await svc.MarkAsReadAsync(notif.Id, 99); // wrong userId
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task MarkAsRead_AlreadyRead_ReturnsSuccess()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var notif = await svc.CreateAsync(
            1, NotificationType.OrderPlaced, "Order Placed", "Buy order placed.");
        await svc.MarkAsReadAsync(notif.Id, 1);

        var result = await svc.MarkAsReadAsync(notif.Id, 1); // read again
        Assert.True(result.IsSuccess);
    }

    // ── MARK ALL AS READ ──────────────────────────────────────

    [Fact]
    public async Task MarkAllAsRead_MultipleUnread_MarksAll()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        await svc.CreateAsync(1, NotificationType.OrderPlaced, "T1", "M1");
        await svc.CreateAsync(1, NotificationType.OrderExecuted, "T2", "M2");
        await svc.CreateAsync(1, NotificationType.FundDeposited, "T3", "M3");

        var count = await svc.MarkAllAsReadAsync(1);

        Assert.Equal(3, count);
        var unread = await db.Notifications.CountAsync(n => n.UserId == 1 && !n.IsRead);
        Assert.Equal(0, unread);
    }

    // ── DELETE ────────────────────────────────────────────────

    [Fact]
    public async Task Delete_ExistingNotification_Removed()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var notif = await svc.CreateAsync(
            1, NotificationType.SystemAlert, "Alert", "Test.");

        var result = await svc.DeleteAsync(notif.Id, 1);

        Assert.True(result.IsSuccess);
        var deleted = await db.Notifications.FindAsync(notif.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task Delete_WrongUser_ReturnsNotFound()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var notif = await svc.CreateAsync(
            1, NotificationType.SystemAlert, "Alert", "Test.");

        var result = await svc.DeleteAsync(notif.Id, 99);
        Assert.False(result.IsSuccess);
        Assert.Equal("NOT_FOUND", result.ErrorCode);
    }

    // ── UNREAD COUNT ──────────────────────────────────────────

    [Fact]
    public async Task GetUnreadCount_ReturnsCorrectCount()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        await svc.CreateAsync(1, NotificationType.OrderPlaced, "T1", "M1");
        await svc.CreateAsync(1, NotificationType.OrderExecuted, "T2", "M2");
        var notif = await svc.CreateAsync(1, NotificationType.FundDeposited, "T3", "M3");
        await svc.MarkAsReadAsync(notif.Id, 1);

        var count = await svc.GetUnreadCountAsync(1);
        Assert.Equal(2, count);
    }

    // ── PAGINATION ────────────────────────────────────────────

    [Fact]
    public async Task GetMyNotifications_UnreadOnly_FiltersCorrectly()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        var n1 = await svc.CreateAsync(1, NotificationType.OrderPlaced, "T1", "M1");
        await svc.CreateAsync(1, NotificationType.OrderExecuted, "T2", "M2");
        await svc.MarkAsReadAsync(n1.Id, 1);

        var result = await svc.GetMyNotificationsAsync(1, 1, 10, unreadOnly: true);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetMyNotifications_Paginated_ReturnsCorrectPage()
    {
        var db  = CreateDb();
        await SeedUserAsync(db);
        var svc = CreateService(db);

        for (int i = 0; i < 5; i++)
            await svc.CreateAsync(1, NotificationType.SystemAlert, $"T{i}", $"M{i}");

        var result = await svc.GetMyNotificationsAsync(1, 1, 3);
        Assert.Equal(5, result.TotalCount);
        Assert.Equal(3, result.Items.Count());
    }
}
