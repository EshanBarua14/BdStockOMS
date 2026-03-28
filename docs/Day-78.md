# Day 78 - Keyboard Navigation

## What was built

### useKeyboardShortcuts.ts (new hook)
Central keyboard shortcut registry. Registers all global shortcuts in one place.

Shortcuts implemented:
- Ctrl+K: open command palette
- ?: show keyboard help overlay
- Escape: close modal/palette
- g then d/o/p/m/r/a/s/t/i/b: vim-style go-to navigation (1s buffer)
  - g+d -> /dashboard
  - g+o -> /orders
  - g+p -> /portfolio
  - g+m -> /market
  - g+r -> /reports
  - g+a -> /accounts
  - g+s -> /settings/general
  - g+t -> /trade-monitor
  - g+i -> /ipo
  - g+b -> /tbond
- F1: open buy console (delegates to BuySellConsole)
- F2: open sell console (delegates to BuySellConsole)
- Alt+Left: browser back
- Alt+Right: browser forward
- Input safety: g-prefix and ? shortcuts skip when focus is in input/textarea

### KeyboardHelp.tsx (new component)
Full-screen overlay showing all shortcuts grouped by category.
- Press ? to open, Escape or ? again to close
- Groups: Global, Navigation, Trading, UI
- Each row shows description + formatted key badge(s)
- Ctrl shown as Ctrl+K, Alt as Alt+Left, etc.

### DashboardLayout.tsx (updated)
- Replaced manual Ctrl+K handler with useKeyboardShortcuts hook
- Added helpOpen state for KeyboardHelp overlay
- Renders both CommandPalette and KeyboardHelp as overlays
- Inner component pattern to call hook inside Router context

## Tests - Day78Tests.cs - 13 tests
- Shortcut count is 14
- All have descriptions and categories
- Ctrl+K is palette shortcut
- ? is help shortcut
- F1=Buy, F2=Sell in Trading category
- g-prefix shortcuts count is 7
- Alt arrows are UI category
- All categories are known values
- No duplicate key combinations
- Escape is Global
- G debounce is 1000ms

## Next - Day 79
E2E tests with Playwright
