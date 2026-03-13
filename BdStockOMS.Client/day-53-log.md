# Day 53 — Full Buy/Sell Order Console

**Branch:** `day-53-buysell-console`
**Date:** 2026-03-14
**Tests:** 135 passed (12 files) — up from 96/11 files (+39 tests)
**Build:** Clean — 906KB JS / 263KB gzip

## What Was Built

### src/components/trading/BuySellConsole.tsx (798 lines) — REWRITTEN
Full-featured order entry panel, accessible via F1/F2 or widget drawer.

Features:
- Side toggle: BUY (F1) / SELL (F2), ESC to close
- BO Code search: autocomplete against /api/ccd/bo-accounts; on select auto-populates
  Client Name, Available Cash, Purchase Power (CashBalance + AvailableMargin), Account Type
- Exchange: DSE / CSE toggle
- Market: Public / SME / ATB / GOV toggle
- Symbol search: prefix + name match against live ticksArray; shows category badge + exchange
- Live price strip: Last, CHG%, Volume, H/L from SignalR
- Price Type: Limit / Market / Market at Best
- Quantity + Price: arrow key up/down support, price auto-fills on symbol select
- Display Qty: stealth/iceberg trading
- Min Qty: minimum execution volume
- Order Time: Day / IOC / FOK
- Private Order: checkbox flag
- Order value: live calc with purchase power sufficiency check (within/exceeds)
- Confirm step: full order summary before placement
- Keyboard nav: up/down in dropdowns, Tab to select, Enter to confirm
- Wires to useOrders.place() with investorId for trader-on-behalf flow

Exported:
- BuySellConsole: global modal mounted in App.tsx root
- BuySellConsoleEvents: open(side, symbol?) / close() event bus
- BuySellHoverTrigger: hover B/S mini-buttons for watchlist rows

### src/api/client.ts — UPDATED
- getMyInvestors(traderId): GET /api/traders/{id}/investors
- getBOAccounts(): GET /api/ccd/bo-accounts
- getStockByCode(code): GET /api/stocks/search?q=

### src/App.tsx — UPDATED
- BuySellConsole mounted at app root above all routes

### src/styles/oms-tokens.css — UPDATED
- Added keyframes oms-slide-up and oms-fade-in

### src/components/layout/Topbar.tsx — UPDATED
- Removed F1/F2 buttons (console now lives in widget drawer)

### src/test/Unit/Day53/buySellConsole.test.ts (212 lines) — NEW
39 tests across 6 suites:
- BuySellConsoleEvents event bus (7)
- Order value calculation (6)
- Order validation (8)
- Order type mapping (6)
- Symbol search filter (7)
- Keyboard shortcut guard (5)

## API Endpoints Used
- GET /api/ccd/bo-accounts: BO client list for autocomplete
- GET /api/traders/{id}/investors: trader's assigned investors
- POST /api/orders: place order with optional InvestorId

## Backend Notes
- PurchasePower computed as CashBalance + AvailableMargin (no dedicated endpoint)
- StockCategory (A/B/G/N/Z/Spot) shown in symbol search dropdown
- DisplayQty, MinQty, TimeInForce, IsPrivate, Exchange, Market are frontend-only for now;
  PlaceOrderDto needs extending for full backend support

## Next — Day 54
Enhanced Watchlist: right-click context menu, column config, price alerts
