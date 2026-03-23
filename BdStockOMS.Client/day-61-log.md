# Day 61 Log — Market Analytics Widgets

**Branch:** `day-61-market-analytics-widgets`
**Date:** 2026-03-22
**Status:** ✅ Complete

---

## Objective

Implement Phase C (Market Analytics Widgets) from the master analysis blueprint — Priority 3.
Day 61 delivers the first three: **ScoreBoard**, **Time & Sales**, and **Most Active**.

---

## What Was Built

### 1. `ScoreBoardWidget.tsx`
Sector-wise market breakdown matching XFL reference widget #9.

- **Columns:** Sector | Gainers▲ | Losers▼ | Unchanged | Not Traded | Value(mn) | Volume | Trades
- **Sortable** by any column (click header), ascending/descending
- **Exchange filter:** All / DSE / CSE pill buttons
- **Summary bar:** Total gainers/losers/unchanged/not-traded + breadth progress bar
- **Per-row mini bar:** Visual breadth indicator inside each sector row
- **Background heat:** Subtle green/red fill proportional to gainer/loser ratio
- **Footer totals:** Summed across all visible sectors
- **Live data:** Builds sector breakdown from `useMarketData().ticksArray`, groups by `sector` field
- **Demo fallback:** 16 realistic Bangladesh sectors with realistic data when no live ticks

### 2. `TimeAndSalesWidget.tsx`
Chronological trade log with Buy/Sell pressure indicators matching XFL reference widget #13.

- **Columns:** Time | Symbol | Exchange | Side (B/S pill) | Price | Qty | Value(mn)
- **Pressure bar:** Real-time Buy% vs Sell% animated bar at top
- **Filters:** Symbol search, Exchange (All/DSE/CSE), Side (All/Buy/Sell)
- **Pause/Resume:** Freeze the feed without losing data
- **Auto-scroll:** Auto-scrolls to newest trade, disables on manual scroll
- **SignalR:** Subscribes to `TradeExecuted` events for real trades
- **Simulation:** Falls back to simulating trades from live ticks every 2.5s when no SignalR events
- **Demo data:** 60 pre-seeded trades across 10 DSE/CSE symbols
- **Context menu:** Right-click symbol → opens BuySellConsole
- **Max buffer:** 500 trades in memory, shows latest 200

### 3. `MostActiveWidget.tsx`
Parameter-based ranking matching XFL reference widget #12.

- **Parameters:** Value (৳) / Volume / Trades / Change% — tab switcher
- **Top N:** Dropdown for Top 10 / 15 / 20
- **Exchange filter:** All / DSE / CSE
- **Per-row progress bar:** Proportional bar showing relative standing vs #1
- **Background heat bar:** Full-width subtle fill behind each row
- **Right-click:** Opens BuySellConsole for quick trading
- **Change% mode:** Sorts by absolute % change, colors by direction
- **Live data:** From `useMarketData().ticksArray`, demo fallback to 20 stocks
- **Category badge:** A/B/G/N/Z color-coded per WatchlistWidget convention

### 4. `registry.tsx` (updated)
- Added `ScoreBoardWidget`, `TimeAndSalesWidget`, `MostActiveWidget` imports
- Registered with `id: "scoreboard"`, `"timesales"`, `"mostactive"`
- Category: `"Analytics"` (new category, auto-appears in widget picker)
- Default sizes: scoreboard 14×16, timesales 10×16, mostactive 10×16

---

## Files Changed

| File | Action |
|------|--------|
| `src/components/widgets/ScoreBoardWidget.tsx`   | ✅ Created |
| `src/components/widgets/TimeAndSalesWidget.tsx` | ✅ Created |
| `src/components/widgets/MostActiveWidget.tsx`   | ✅ Created |
| `src/components/widgets/registry.tsx`           | ✅ Updated |

---

## Test Results

- `dotnet build BDStockOMS.slnx` → ✅ Build succeeded (no new backend changes)
- `dotnet test BDStockOMS.slnx` → ✅ 812 tests passing — 28 new Day 61 tests added (no regressions)
- Frontend: `npm run build` → verify no TypeScript errors

---

## Design Decisions

- **CSS vars throughout:** All colors use `var(--t-*)` tokens — works with all 14 themes
- **Demo data fallback:** Every widget works without a live API/SignalR connection
- **`// @ts-nocheck`:** Consistent with all existing widgets
- **`useMarketData()` hook:** Same pattern as `TopMoversWidget` / `WatchlistWidget`
- **`BuySellConsoleEvents.open()`:** Right-click integration consistent with `WatchlistWidget`
- **`subscribeMarket()`:** SignalR event subscription consistent with `IndexSummaryWidget`
- **JetBrains Mono font:** Newer widget standard (vs Space Mono in older widgets)

---

## Next — Day 62

**Target:** Time & Sales enhancements + News Feed widget upgrade
- [ ] News feed: keyword filter, board filter, category filter, live SignalR news events
- [ ] Time & Sales: add Trade Match ID column, Aggressor Indicator (per XFL reference)
- [ ] Price History widget: OHLC table with date range picker
