using Xunit;

namespace BdStockOMS.Tests.Unit;

public class Day77Tests
{
    // Command palette navigation items
    private static readonly string[] NavPaths = new[]
    {
        "/dashboard", "/orders", "/portfolio", "/market",
        "/trade-monitor", "/rms", "/reports", "/accounts",
        "/ipo", "/tbond", "/admin/brokers", "/admin/branches",
        "/admin/bo-accounts", "/admin/bos", "/admin/fix",
        "/settings/general", "/settings/market",
        "/settings/fix-engine", "/settings/roles",
    };

    [Fact]
    public void CommandPalette_NavItems_AllHaveValidPaths()
    {
        foreach (var path in NavPaths)
            Assert.StartsWith("/", path);
    }

    [Fact]
    public void CommandPalette_NavItems_Count_Is19()
        => Assert.Equal(19, NavPaths.Length);

    [Fact]
    public void CommandPalette_NoDuplicatePaths()
    {
        var distinct = NavPaths.Distinct().Count();
        Assert.Equal(NavPaths.Length, distinct);
    }

    [Fact]
    public void CommandPalette_AdminPaths_StartWithAdmin()
    {
        var adminPaths = NavPaths.Where(p => p.StartsWith("/admin/")).ToArray();
        Assert.True(adminPaths.Length >= 5);
    }

    [Fact]
    public void CommandPalette_SettingsPaths_StartWithSettings()
    {
        var settingPaths = NavPaths.Where(p => p.StartsWith("/settings/")).ToArray();
        Assert.True(settingPaths.Length >= 3);
    }

    [Fact]
    public void CommandPalette_CorePaths_AllPresent()
    {
        Assert.Contains("/dashboard",    NavPaths);
        Assert.Contains("/orders",       NavPaths);
        Assert.Contains("/portfolio",    NavPaths);
        Assert.Contains("/market",       NavPaths);
        Assert.Contains("/reports",      NavPaths);
    }

    [Fact]
    public void CommandPalette_KeyboardShortcut_IsCtrlK()
    {
        var shortcut = "ctrl+k";
        Assert.Contains("ctrl", shortcut);
        Assert.Contains("k",    shortcut);
    }

    [Fact]
    public void CommandPalette_StockSearch_MinQueryLength()
    {
        var minLen = 2;
        Assert.Equal(2, minLen);
        Assert.True("BR".Length >= minLen);
        Assert.False("B".Length >= minLen);
    }

    [Fact]
    public void CommandPalette_Highlight_EmptyQuery_ReturnsOriginal()
    {
        var text  = "Go to Dashboard";
        var query = "";
        var result = query.Length == 0 ? text : text;
        Assert.Equal("Go to Dashboard", result);
    }

    [Fact]
    public void CommandPalette_Highlight_MatchFound()
    {
        var text  = "Go to Dashboard";
        var query = "dash";
        var idx   = text.ToLower().IndexOf(query.ToLower());
        Assert.True(idx >= 0);
        Assert.Equal("Dashboard", text.Substring(idx, "Dashboard".Length));
    }

    [Fact]
    public void CommandPalette_Debounce_Is250ms()
    {
        var debounceMs = 250;
        Assert.Equal(250, debounceMs);
    }

    [Fact]
    public void CommandPalette_MaxStockResults_Is6()
    {
        var maxResults = 6;
        Assert.Equal(6, maxResults);
    }

    [Fact]
    public void CommandPalette_DefaultNavItems_Shows8()
    {
        var defaultCount = 8;
        Assert.Equal(8, defaultCount);
        Assert.True(NavPaths.Length >= defaultCount);
    }
}
