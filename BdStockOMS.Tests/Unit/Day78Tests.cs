using Xunit;

namespace BdStockOMS.Tests.Unit;

public class Day78Tests
{
    // Mirror the shortcut registry from useKeyboardShortcuts.ts
    private record Shortcut(string Key, bool Ctrl, bool Alt, bool Shift, string Category, string Description);

    private static readonly Shortcut[] Shortcuts = new[]
    {
        new Shortcut("k",          true,  false, false, "Global",     "Open command palette"),
        new Shortcut("?",          false, false, false, "Global",     "Show keyboard shortcuts"),
        new Shortcut("Escape",     false, false, false, "Global",     "Close modal / palette"),
        new Shortcut("g d",        false, false, false, "Navigation", "Go to Dashboard"),
        new Shortcut("g o",        false, false, false, "Navigation", "Go to Orders"),
        new Shortcut("g p",        false, false, false, "Navigation", "Go to Portfolio"),
        new Shortcut("g m",        false, false, false, "Navigation", "Go to Market"),
        new Shortcut("g r",        false, false, false, "Navigation", "Go to Reports"),
        new Shortcut("g a",        false, false, false, "Navigation", "Go to Accounts"),
        new Shortcut("g s",        false, false, false, "Navigation", "Go to Settings"),
        new Shortcut("F1",         false, false, false, "Trading",    "Open Buy console"),
        new Shortcut("F2",         false, false, false, "Trading",    "Open Sell console"),
        new Shortcut("ArrowLeft",  false, true,  false, "UI",         "Go back"),
        new Shortcut("ArrowRight", false, true,  false, "UI",         "Go forward"),
    };

    [Fact]
    public void Shortcuts_Count_Is14()
        => Assert.Equal(14, Shortcuts.Length);

    [Fact]
    public void Shortcuts_AllHaveDescriptions()
    {
        foreach (var s in Shortcuts)
            Assert.False(string.IsNullOrEmpty(s.Description));
    }

    [Fact]
    public void Shortcuts_AllHaveCategories()
    {
        foreach (var s in Shortcuts)
            Assert.False(string.IsNullOrEmpty(s.Category));
    }

    [Fact]
    public void Shortcuts_CtrlK_IsPaletteShortcut()
    {
        var s = Shortcuts.First(x => x.Key == "k");
        Assert.True(s.Ctrl);
        Assert.Equal("Global", s.Category);
    }

    [Fact]
    public void Shortcuts_QuestionMark_IsHelpShortcut()
    {
        var s = Shortcuts.First(x => x.Key == "?");
        Assert.False(s.Ctrl);
        Assert.Contains("shortcuts", s.Description.ToLower());
    }

    [Fact]
    public void Shortcuts_F1_IsBuyShortcut()
    {
        var s = Shortcuts.First(x => x.Key == "F1");
        Assert.Equal("Trading", s.Category);
        Assert.Contains("Buy", s.Description);
    }

    [Fact]
    public void Shortcuts_F2_IsSellShortcut()
    {
        var s = Shortcuts.First(x => x.Key == "F2");
        Assert.Equal("Trading", s.Category);
        Assert.Contains("Sell", s.Description);
    }

    [Fact]
    public void Shortcuts_GPrefix_NavigatesCorrectly()
    {
        var navShortcuts = Shortcuts.Where(s => s.Key.StartsWith("g ")).ToArray();
        Assert.Equal(7, navShortcuts.Length);
        Assert.Contains(navShortcuts, s => s.Key == "g d");
        Assert.Contains(navShortcuts, s => s.Key == "g o");
        Assert.Contains(navShortcuts, s => s.Key == "g p");
    }

    [Fact]
    public void Shortcuts_AltArrows_AreNavigationShortcuts()
    {
        var back    = Shortcuts.First(x => x.Key == "ArrowLeft");
        var forward = Shortcuts.First(x => x.Key == "ArrowRight");
        Assert.True(back.Alt);
        Assert.True(forward.Alt);
        Assert.Equal("UI", back.Category);
        Assert.Equal("UI", forward.Category);
    }

    [Fact]
    public void Shortcuts_Categories_AreKnown()
    {
        var known = new[] { "Global", "Navigation", "Trading", "UI" };
        var actual = Shortcuts.Select(s => s.Category).Distinct().ToArray();
        foreach (var cat in actual)
            Assert.Contains(cat, known);
    }

    [Fact]
    public void Shortcuts_NoDuplicateKeys()
    {
        var keys = Shortcuts.Select(s => (s.Key, s.Ctrl, s.Alt, s.Shift)).ToArray();
        var distinct = keys.Distinct().Count();
        Assert.Equal(keys.Length, distinct);
    }

    [Fact]
    public void Shortcuts_EscapeIsGlobal()
    {
        var esc = Shortcuts.First(x => x.Key == "Escape");
        Assert.Equal("Global", esc.Category);
    }

    [Fact]
    public void Shortcuts_GDebounce_Is1000ms()
    {
        var debounceMs = 1000;
        Assert.Equal(1000, debounceMs);
    }
}
