# Day 28 — Trading Dashboard (Frontend Foundation)

## Date
2025 — Day 28 of BD Stock OMS build

## Branch
`day-28-trading-dashboard`

## Status
✅ Complete — 281 tests passing, 0 failures

---

## What Was Built

### New Frontend Files

| File | Purpose |
|------|---------|
| `src/types/trading.ts` | TypeScript interfaces: Stock, Order, Portfolio, PriceUpdate, OrderRequest |
| `src/services/tradingApi.ts` | Typed API calls: fetchStocks, searchStocks, placeOrder, fetchOrders, fetchPortfolio |
| `src/hooks/useSignalR.ts` | SignalR connection hook — auto-connect, price update callbacks, cleanup |
| `src/components/trading/StockList.tsx` | Market watch panel — search, live price flash (green/red), exchange badge |
| `src/components/trading/PlaceOrderForm.tsx` | Order entry form — buy/sell, quantity, price, order type |
| `src/components/trading/OrderHistory.tsx` | Paginated order list with status badges |
| `src/components/trading/PortfolioPanel.tsx` | Holdings table with unrealised P&L |
| `src/pages/TradingDashboard.tsx` | 3-column layout — StockList / PlaceOrderForm / Orders+Portfolio tabs |
| `src/pages/DashboardPage.tsx` | Updated to load TradingDashboard |

### Architecture Decisions
- **No localStorage** — auth state via httpOnly cookie + memory only
- **Role-gated order form** — only Investor and Trader roles see PlaceOrderForm
- **SignalR price updates** — flash animation (800ms) on price change, green=up, red=down
- **Debounced search** — 300ms debounce on stock search input
- **Refresh trigger pattern** — integer counter passed as prop to force re-fetch after order placed

---

## Tests
- **Total: 281 passing, 0 failures**
- No new backend tests added (Day 28 was frontend-only)

---

## Build
- Vite: 132 modules, 0 errors
- SignalR pure comment warnings are harmless

---

## Next: Day 29
- `ITenantContext` — extract BrokerageHouseId from JWT claims
- Tenant filter on all repository queries
- `IExchangeConnector` interface + `SimulatedExchangeConnector`
- Keyed DI: `"DSE"` and `"CSE"` keys
- Migration: `Day29_TenantIsolation`
- Target: 300+ tests
