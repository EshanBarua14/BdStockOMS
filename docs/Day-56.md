# Day 56 - Market Depth Widget + Widget System Polish
**Branch:** day-56-seed-depth
**Tests:** 177 passed (13 files)
**Build:** Clean

## What Was Built
- MarketDepthWidget: symbol input, REST load, SignalR live updates, bid/ask volume bars
- WatchlistWidget: viewport-clamped filter/context menu, double-click opens BuySell
- TopbarIconBtn: new component with badge count support
- AppSettingsBtn: extracted from Topbar
- Widget system: empty new pages, sensible sizes, buysell as overlay
- Registry fix: buysell _old_component renamed to component
- Added vitest test script to package.json
