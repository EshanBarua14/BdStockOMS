# Day 50 - Full Trading Terminal Dashboard

## Overview
Bloomberg-style 16-widget drag-and-drop trading terminal built on React 18 + TypeScript + Vite.
All widgets consume real-time data from the ASP.NET Core 8 backend via SignalR and REST.

## Backend Simulation Engine (StockPriceUpdateService)
- GBM (Geometric Brownian Motion) price simulation with drift + diffusion
- Mean reversion towards base price to prevent runaway prices
- Market sentiment regime: bull/bear cycles with random shocks
- Volatility by category: Z board (0.8%) > B (0.6%) > A (0.5%) > G (0.3%)
- Circuit breaker: +/-10% from base price (DSE rules)
- OHLCV updated every 2s: High, Low, Volume, ValueInMillionTaka persisted to DB
- Buy/sell pressure correlated with price direction and volume

## SignalR Broadcasts
- BulkPriceUpdate (2s) - all stocks to all clients
- PriceUpdate (2s) - per-stock to subscribed group
- DepthUpdate (10s) - 10-level bid/ask ladder per stock
- PressureUpdate (20s) - buy/sell pressure for all stocks
- IndexUpdate (60s) - DSEX / DSES / DS30 / CSEALL / CSE30
- NewsUpdate (120s) - simulated DSE market news

## New Backend Controllers
- GET /api/MarketDepth/{tradingCode} - REST order book depth
- GET /api/MarketDepth/pressure/{tradingCode} - buy/sell pressure
- GET /api/News?count=20 - simulated market news feed

## 16 Widgets
1.  MarketTickerStrip      - SignalR BulkPriceUpdate
2.  WatchlistWidget        - REST /watchlists + SignalR prices
3.  OrderEntryWidget       - REST POST /Order + RMS validation
4.  OrderBookWidget        - REST GET /Order
5.  ExecutionListWidget    - REST GET /Order (filled only)
6.  TopMoversWidget        - SignalR BulkPriceUpdate
7.  MarketMapWidget        - SignalR BulkPriceUpdate
8.  MarketDepthWidget      - REST + SignalR DepthUpdate
9.  BuySellPressureWidget  - SignalR PressureUpdate
10. PortfolioWidget        - REST /PortfolioSnapshot
11. PriceChartWidget       - SignalR BulkPriceUpdate live chart
12. NotificationsWidget    - SignalR NotificationHub
13. AIPredictionWidget     - Frontend momentum model on live prices
14. IndexSummaryWidget     - SignalR IndexUpdate
15. NewsFeedWidget         - REST /News + SignalR NewsUpdate
16. RMSLimitsWidget        - REST /RMS/my-limits

## Dashboard Features
- react-grid-layout: drag, resize, persist layout per user in localStorage
- 4 preset layouts: Trading, Research, Portfolio, Full (all 16)
- Widget toolbar: searchable categorised add/remove panel
- Symbol linking: color-group widgets to sync symbol selection
- Layout save/reset per userId key in localStorage

## Test Results
- 6 test files, 52 tests, 0 failures
- registry.test.ts     - 9 tests
- marketData.test.ts   - 8 tests
- orders.test.ts       - 7 tests
- aiSignal.test.ts     - 7 tests
- marketDepth.test.ts  - 11 tests
- themeStore.test.ts   - 10 tests

## Build
- npm run build: 0 errors, 852 modules, 869KB bundle
- dotnet build: 0 errors, 5 warnings (pre-existing)
