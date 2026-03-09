using System.Security.Claims;
using BdStockOMS.API.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

public class TenantContextTests
{
    private static IHttpContextAccessor BuildAccessor(List<Claim> claims)
    {
        var identity  = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var ctx       = new DefaultHttpContext { User = principal };
        var mock      = new Mock<IHttpContextAccessor>();
        mock.Setup(a => a.HttpContext).Returns(ctx);
        return mock.Object;
    }

    [Fact]
    public void TenantContext_ExtractsBrokerageHouseId_FromClaims()
    {
        var accessor = BuildAccessor(new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-123"),
            new(ClaimTypes.Role, "Investor"),
            new("BrokerageHouseId", "7"),
        });

        var ctx = new TenantContext(accessor);

        Assert.Equal(7,          ctx.BrokerageHouseId);
        Assert.Equal("user-123", ctx.UserId);
        Assert.Equal("Investor", ctx.Role);
        Assert.False(ctx.IsSuperAdmin);
    }

    [Fact]
    public void TenantContext_IsSuperAdmin_WhenRoleIsSuperAdmin()
    {
        var accessor = BuildAccessor(new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "admin-1"),
            new(ClaimTypes.Role, "SuperAdmin"),
            new("BrokerageHouseId", "0"),
        });

        var ctx = new TenantContext(accessor);

        Assert.True(ctx.IsSuperAdmin);
    }

    [Fact]
    public void TenantContext_DefaultsToZero_WhenNoBrokerageHouseClaim()
    {
        var accessor = BuildAccessor(new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-99"),
            new(ClaimTypes.Role, "Trader"),
        });

        var ctx = new TenantContext(accessor);

        Assert.Equal(0, ctx.BrokerageHouseId);
        Assert.False(ctx.IsSuperAdmin);
    }

    [Fact]
    public void TenantContext_HandlesNullHttpContext_Gracefully()
    {
        var mock = new Mock<IHttpContextAccessor>();
        mock.Setup(a => a.HttpContext).Returns((HttpContext?)null);

        var ctx = new TenantContext(mock.Object);

        Assert.Equal(0,            ctx.BrokerageHouseId);
        Assert.Equal(string.Empty, ctx.UserId);
        Assert.Equal(string.Empty, ctx.Role);
        Assert.False(ctx.IsSuperAdmin);
    }

    [Fact]
    public void TenantContext_HandlesInvalidBrokerageHouseId_ReturnsZero()
    {
        var accessor = BuildAccessor(new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user-1"),
            new(ClaimTypes.Role, "Investor"),
            new("BrokerageHouseId", "not-a-number"),
        });

        var ctx = new TenantContext(accessor);

        Assert.Equal(0, ctx.BrokerageHouseId);
    }
}
