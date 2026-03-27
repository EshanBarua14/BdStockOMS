# Day 59 — System Fixes: Watchlist, Widgets, AppSettings, Page Options, Trade Monitor

**Branch:** day-59-system-fixes
**Tests:** 184 passed (14 files)
**Build:** Clean

## What Was Fixed

### Watchlist Widget
- Demo stocks pre-loaded (8 DSE + 4 CSE) when list is empty or backend offline
- Exchange tab buttons: All / DSE / CSE — filters stocks by exchange
- DSE tab = blue, CSE tab = purple, active tab highlighted
- Live ticks from SignalR overlay demo data when connected

### Widget Sizes
- All widgets reduced to compact default sizes (~8-10 cols x 10-14 rows)
- Ticker: 48→24 cols default (half-width, still resizable to full)
- All widgets: isResizable + isDraggable already enabled on GridLayout
- Users can freely resize any widget after placing

### AppSettingsBtn — real settings panel
- Click ⚙️ in topbar → slide-down panel with 3 tabs
- Settings tab: toggles for Ticker, Sound, Desktop Notifs, Auto-refresh, Compact View
- Notifications tab: list with unread badge, click to mark read
- News tab: latest 5 news items with demo fallback
- Badge count shows unread notifications

### Page Right-Click Context Menu
- Right-click tab: Rename, Duplicate, Save layout, Delete
- Duplicate now copies layout + instances to new page (deep clone)
- addPage() in store updated to accept layout/instances params

### Trade Monitor
- Demo data injected when API returns zero activity (market closed)
- Shows realistic BDT values for traders and clients
- Real data used automatically when backend has actual trades

### Registry Test
- Updated ticker defaultW expectation: 48→24 (reflects new compact default)

## Test Results
- 184 passed, 0 failed (14 test files)
