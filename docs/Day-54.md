# Day 54 — Enhanced Watchlist + Buy/Sell Console Overhaul

**Branch:** `day-54-watchlist`
**Date:** 2026-03-14
**Tests:** 177 passed (13 files) — up from 135/12 (+42 tests)
**Build:** Clean — 924KB JS / 267KB gzip

## What Was Built

### src/components/trading/BuySellConsole.tsx — FULLY REWRITTEN
- Side toggle: BUY (F1) / SELL (F2), ESC close
- BO Code autocomplete: searches /api/ccd/bo-accounts, arrow/tab/enter keyboard nav
- Client info card: Cash Balance, Purchase Power (cash+margin), Available Margin
- Exchange (SOR): DSE / CSE / Both dropdown
- Market/Board: Regular, SME, ATB, GOV, Odd Lot, Block dropdown
- Symbol search: prefix+name, category badge, exchange, arrow/enter nav
- Live price strip: Last, CHG%, High, Low, Volume tiles
- Circuit Breaker display: HIGH/LOW with in-range/out-of-range indicator
- Price Type: Limit / Market / Market at Best (dropdown)
- Order Time TIF: Day / IOC / FOK (segmented buttons)
- Quantity + Price: arrow key up/down, auto-fill from live data
- Display Qty (stealth) + Min Qty fields
- Private Order checkbox
- Order Summary: Total Cost, Commission (~0.5%), Remaining purchase power, progress bar
- Purchase power warning if order exceeds limit
- Limit Request toggle button in topbar
- Confirm popup: full order summary, Enter=confirm, Esc=cancel, keyboard+mouse
- Result feedback with auto-close on success
- Reset button
- Footer: keyboard shortcut hints + live status

### src/components/widgets/registry.tsx — UPDATED
- Added buysell widget (id: buysell, icon ⚡, category Trading)
- Clicking buysell in widget picker opens BuySellConsole directly

### src/pages/DashboardPage.tsx — UPDATED
- Imported BuySellConsoleEvents
- Widget picker: buysell click opens console instead of adding to grid

### src/components/widgets/WatchlistWidget.tsx — REWRITTEN (626 lines)
- 22 configurable columns, drag-to-reorder, localStorage persist
- Right-click context menu: Buy/Sell, market actions, add-to-watchlist submenu
- Advanced filter: Traded Only, Spot Only, Exchange, Category, Symbol
- Column sort asc/desc with header indicators
- Live price merge from SignalR ticksArray
- Create/rename/delete watchlists, tab UI
- Add stock search with arrow key nav
- Buy/Sell pressure mini bar charts

### src/api/watchlist.ts — FIXED
- Added /api prefix to all endpoints
- Added reorder() method

### src/api/market.ts — FIXED
- searchStocks: /stocks/search → /api/stocks/search, param q → query
- getAllStocks: /stocks → /api/stocks

### src/test/Unit/Day50/registry.test.ts — UPDATED
- Updated widget count assertion: 16 → 17

### src/test/Unit/Day54/watchlist.test.ts — NEW (42 tests)
- Column config, getCellValue, filter, sort, live merge, formatters

## Backend Sync
- Stock search: GET /api/stocks/search?query= (not ?q=)
- BO accounts: GET /api/ccd/bo-accounts → BOAccountResponseDto
- PlaceOrderDto: stockId, orderType(0/1), orderCategory(0/1), quantity, limitPrice?, investorId?
- Exchange/Market/TIF/DisplayQty/MinQty/Private: frontend-only (PlaceOrderDto extension needed)
- Circuit breaker: circuitBreakerHigh/Low on Stock model (in ticksArray via SignalR)

## Next — Day 55
Market Depth widget: bid/ask ladder, spread, depth chart
