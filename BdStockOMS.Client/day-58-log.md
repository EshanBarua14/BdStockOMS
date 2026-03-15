# Day 58 — Widget Fixes, Page Options, Demo Data, RBAC

**Branch:** day-58-user-management
**Tests:** 184 passed (14 files)
**Build:** Clean

## What Was Fixed

### Sidebar — Trade Monitor visible to all roles
- Removed `section: 'Trading'` from Trade Monitor nav item (was filtered out of main nav)
- Now visible to: SuperAdmin, Admin, BrokerageAdmin, CCD, Trader, Investor

### BuySell Console — inline widget
- Added `BuySellConsoleInline` export to BuySellConsole.tsx (wraps with `embedded=true`)
- Registry now uses `BuySellConsoleInline` as component (places on grid like any widget)
- Popup/overlay still works when triggered from Watchlist row double-click or F1/F2
- Widget drawer: buysell now draggable and click-to-add like all other widgets

### Per-page options — right-click context menu on tabs
- Right-click any page tab → Rename, Duplicate, Save layout, Delete
- Double-click tab → inline rename input
- Duplicate creates copy with same layout and widgets
- Delete disabled when only 1 page remains

### Demo data fallbacks
- PortfolioWidget: demo holdings + P&L when API unavailable
- NewsFeedWidget: 6 demo BD market news items when API unavailable
- RMSLimitsWidget: demo credit/margin limits when API unavailable

### TypeScript fixes
- NewsFeedWidget: `useState<any[]>`, function params typed as `any`
- RMSLimitsWidget: `useState<any>`, GaugeBar typed as `any`
- Fixed broken catch block from previous Python edit

### CSP
- vite.config.ts: removed conflicting CSP header (index.html has correct one)
- index.html: connect-src uses `localhost:*` wildcard

## Test Results
- 184 passed, 0 failed (14 test files)
