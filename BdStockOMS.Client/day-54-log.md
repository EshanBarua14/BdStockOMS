# Day 54 — Enhanced Watchlist Widget

**Branch:** `day-54-watchlist`
**Date:** 2026-03-14
**Tests:** 177 passed (13 files) — up from 135/12 files (+42 tests)
**Build:** Clean — 924KB JS / 267KB gzip

## What Was Built

### src/components/widgets/WatchlistWidget.tsx (626 lines) — REWRITTEN
Full-featured watchlist with all spec features from Share Price module doc.

Features:
- Custom Columns: 22 available columns, drag-to-reorder, search, toggle, persisted to localStorage
- Default columns: Code, Exch, Cat, LTP, Chg, Chg%, Vol, High, Low
- Sort: click column header to sort asc/desc, indicator arrow shown
- Advanced Filter panel: Traded Only, Spot Only, Exchange, Category, Symbol prefix search
- Filter active indicator on filter button
- Live price merge: SignalR ticksArray overlaid on watchlist base data per tradingCode
- Right-click context menu on any row:
  - Buy (F1) / Sell (F2) → opens BuySellConsole
  - Active Orders, Executed Orders, Market Depth, Time & Sales (stubs, Day 55+)
  - Minute Chart, Technical Analysis, News, Company Info (stubs)
  - Add to Watchlist submenu → any other list
  - Remove from list
- Watchlist tabs: create, rename (double-click), delete non-default lists
- Add stock search: prefix+name autocomplete, arrow key nav, Enter to add
- Stock count display (filtered / total)
- Toolbar: columns button, filter button, refresh button
- Zebra row striping, hover highlight
- Buy/Sell pressure mini bar chart in pressure columns
- Category color coding: A=green, B=amber, G=blue, N=purple, Z=red, Spot=orange

### src/api/watchlist.ts — FIXED + UPDATED
- Added /api prefix to all endpoints (was missing, caused 404s)
- Added reorder() method: PUT /api/watchlists/{id}/reorder

### src/api/market.ts — FIXED
- Fixed searchStocks path: /stocks/search → /api/stocks/search
- Fixed getAllStocks path: /stocks → /api/stocks

### src/test/Unit/Day54/watchlist.test.ts (42 tests) — NEW
6 suites:
- Column configuration (7)
- getCellValue field mapping (9)
- Filter logic (8)
- Sort logic (7)
- Live price merge (5)
- Format helpers (6)

## Backend Sync Notes
- WatchlistWithItems.Stocks[] fields: watchlistItemId, stockId, tradingCode,
  companyName, exchange, lastTradePrice, change, changePercent, sortOrder
- StockResponseDto search fields: id, tradingCode, companyName, exchange,
  lastTradePrice, highPrice, lowPrice, change, changePercent, volume, valueInMillionTaka
- All watchlist CRUD endpoints verified against WatchlistController + WatchlistService
- reorder endpoint: PUT /api/watchlists/{id}/reorder — List<{stockId, sortOrder}>

## Next — Day 55
Market Depth widget: bid/ask ladder, spread indicator, depth chart
