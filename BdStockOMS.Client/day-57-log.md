# Day 57 — Trade Monitor Page

**Branch:** day-57-trade-monitor
**Tests:** 184 passed (14 files, +7 new)
**Build:** Clean

## What Was Built

### src/pages/TradeMonitorPage.tsx — NEW
- Summary metrics: Total Buy/Sell/Turnover, Active Orders, Investors, Traders, Commission, Pending KYC
- Buy vs Sell pie chart (Recharts PieChart) with fallback to 1 when zero
- Top Traders bar chart with Buy/Sell/Total toggle buttons
- Top Clients stacked bar chart
- Auto-refresh every 10 seconds via setInterval
- Manual Refresh button
- Last refresh timestamp display
- BDT currency formatter: Cr/L/raw based on magnitude
- Fully wired to real backend: /api/BrokerSummary/{id}, /top-traders/{type}, /top-clients

### src/api/tradeMonitor.ts — NEW
- getBrokerSummary(brokerageHouseId)
- getTopTraders(brokerageHouseId, type: buy|sell|value)
- getTopClients(brokerageHouseId)

### src/App.tsx — UPDATED
- Added /trade-monitor route → TradeMonitorPage

### src/components/layout/Sidebar.tsx — UPDATED
- Added Trade Monitor nav item (SuperAdmin/Admin/BrokerageAdmin)

### index.html — FIXED
- CSP connect-src updated to localhost:* wildcard (covers all dev ports)

### tsconfig.json — FIXED
- Removed duplicate moduleResolution key

## Test Results
- 184 passed, 0 failed (14 test files)
- Day57/tradeMonitor.test.ts: 6 new tests (formatting, data mapping, pie fallback)
