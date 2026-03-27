# Day 69 - T&S Multi-Symbol, TA Indicators, Widget Drawer Reorder

**Branch:** `day-69-tas-widgets`
**Tests:** 914 (start) -> 934 (end) | +20 tests
**All tests:** 934 backend passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | T&S multi-symbol — pinned symbol tabs bar (up to 6 symbols) | Done |
| 2 | Pin/unpin symbols, switch between tabs with independent filter state | Done |
| 3 | PriceChartWidget TA indicators — RSI, MACD, Bollinger Bands | Done |
| 4 | TA panel below main chart, toggleable (OFF/RSI/MACD/BB) | Done |
| 5 | RSI(14) with overbought/oversold reference lines | Done |
| 6 | MACD(12,26,9) with histogram bars + signal line | Done |
| 7 | Bollinger Bands(20,2) with upper/mid/lower dashed lines | Done |
| 8 | WidgetDrawer Add + Manage tabs | Done |
| 9 | Manage tab — drag-to-reorder active widgets with visual feedback | Done |
| 10 | Manage tab — remove widgets with ✕ button | Done |
| 11 | Day69Tests.cs — 20 tests (T&S logic, RSI/BB math, stock queries) | Done |
| 12 | Build passing: 0 errors | Done |

---

## T&S Multi-Symbol

- Pinned symbol tab bar appears below the symbol search input
- Up to 6 symbols can be pinned simultaneously
- Each tab shows the symbol code with an ✕ to unpin
- Switching tabs changes the active T&S feed without losing filter state
- Searching and pressing GO auto-pins the new symbol
- Last pinned symbol cannot be unpinned (always at least 1)

## TA Indicators (PriceChartWidget)

| Indicator | Params | Description |
|-----------|--------|-------------|
| RSI | Period 14 | Relative Strength Index with 70/30 reference lines |
| MACD | 12, 26, 9 | MACD line + Signal line + Histogram bars |
| BB | 20 period, 2σ | Bollinger Bands upper/mid/lower dashed lines |

Toggle buttons in chart toolbar: OFF / RSI / MACD / BB
TA panel renders below main price chart at 100px height

## Widget Drawer Enhancement

**Add tab** (unchanged): search + category list, click/drag to add
**Manage tab** (new):
- Lists all active widget instances on current page
- Drag handle (⠿) for reordering via HTML5 drag-and-drop
- Visual feedback: drag-over highlight + opacity on dragged item
- ✕ button to remove individual widgets
- Widget count shown in header

---

## Test Count Progression

| Day | Backend | Total | Notes |
|-----|---------|-------|-------|
| 68 | 914 | 914 | Start |
| 69 | 934 | 934 | +20: T&S logic, RSI/BB math, stock queries |

---

## Next: Day 70 - Accounts Module UI (Deposit/Withdrawal/IPO/TBond)