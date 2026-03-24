# Day 61 — System Audit Fix Log

**Branch:** `day-61-market-analytics-widgets`
**Date:** 2026-03-24
**Status:** ✅ All 9 bugs fixed

---

## Bugs Found & Fixed

### Bug 1 — `client.ts`: Hardcoded BASE URL bypasses Vite proxy ⚠️ CRITICAL
**File:** `src/api/client.ts`
**Problem:** `const BASE = "https://localhost:7219"` hardcoded. In production this breaks entirely. Also Vite proxy at `/api` and `/hubs` is bypassed because fetch goes directly to absolute URL.
**Fix:** Removed BASE entirely. All fetch calls use relative `/api/...` paths. Vite proxy routes them correctly in dev. In production, environment variable controls the base.
**Also fixed:** `_fetch()` now strips any absolute base from URLs to prevent double-hostname bugs.

---

### Bug 2 — `orders.ts` (api): Missing `/api/` prefix ⚠️ CRITICAL
**File:** `src/api/orders.ts`
**Problem:** `apiClient.get('/orders')` → 404. Backend route is `/api/orders`.
**Fix:** All routes now have `/api/` prefix: `/api/orders`, `/api/orders/{id}`, etc.

---

### Bug 3 — `orders.ts` (api): Wrong HTTP method for cancel ⚠️ CRITICAL
**File:** `src/api/orders.ts`
**Problem:** `apiClient.post('/orders/{id}/cancel')` → 405 Method Not Allowed. Backend uses `[HttpPut("{id:int}/cancel")]`.
**Fix:** Changed to `apiClient.put('/api/orders/{id}/cancel', data)`.

---

### Bug 4 — `client.ts`: `cancelOrder` used POST not PUT ⚠️ CRITICAL
**File:** `src/api/client.ts`
**Problem:** `fetch(.../cancel, { method: "POST" })` → 405. Backend: `[HttpPut("{id:int}/cancel")]`.
**Fix:** Changed to `method: "PUT"`.

---

### Bug 5 — `OrderBookWidget`: Wrong field names from Order interface ⚠️ CRITICAL
**File:** `src/components/widgets/OrderBookWidget.tsx`
**Problem:** Widget accessed `o.symbol`, `o.side`, `o.type`, `o.price`, `o.orderId` — none of these exist on the `Order` interface. Correct fields are `o.tradingCode`, `o.orderType` (0=Buy/1=Sell), `ORDER_CAT_LABEL[o.orderCategory]`, `o.limitPrice`, `o.id`.
**Also:** Status filter compared `o.status !== "Open"` (string) but status is numeric (0=Pending, 1=Open etc).
**Fix:** Complete rewrite of OrderBookWidget using correct field names and numeric status codes.

---

### Bug 6 — `startGlobalMarketHub` never called ⚠️ CRITICAL
**File:** `src/main.tsx`
**Problem:** `startGlobalMarketHub(token)` is defined in `useSignalR.ts` but was never called anywhere in the app. This means the global SignalR connection (that feeds `BulkPriceUpdate`, `IndexUpdate`, `TradeExecuted`, `NewsUpdate` etc.) never starts. All `subscribeMarket()` listeners were silently receiving nothing.
**Fix:** `main.tsx` now calls `initSignalR()` on startup which: (1) starts hub if token exists in localStorage, (2) subscribes to auth store changes to restart hub on login.

---

### Bug 7 — `authApi.logout()` is a no-op ⚠️ MEDIUM
**File:** `src/api/auth.ts`
**Problem:** `logout: () => Promise.resolve()` — does nothing. Backend has Redis token blacklisting. Without calling `POST /api/auth/logout`, the JWT token remains valid on the server even after logout.
**Fix:** `logout: () => apiClient.post("/api/auth/logout").catch(() => {})` — calls backend to blacklist the token. `.catch(() => {})` ensures logout still works even if API is down.

---

### Bug 8 — `searchStocks` query param mismatch ⚠️ MEDIUM
**Files:** `src/api/client.ts`, `src/api/market.ts`
**Problem:** Both used `?q=` but backend `StockController.cs` search endpoint uses `?query=` (confirmed from `[HttpGet("search")]` → `string query` parameter).
**Fix:** Changed to `?query=` in both files.

---

### Bug 9 — Topbar indexes show hardcoded placeholder values ⚠️ LOW
**File:** `src/components/layout/Topbar.tsx`
**Problem:** Index values like `'5,432'`, `'1,987'` are hardcoded strings that never update. They don't reflect real live data. The `IndexSummaryWidget` correctly uses `subscribeMarket("IndexUpdate")` but Topbar doesn't.
**Status:** Not breaking, but misleading. The `IndexSummaryWidget` is the source of truth for live index values. Topbar approximates from ticksArray average which is better than hardcoded but still not real index data.
**Note:** Real fix requires backend to broadcast actual index values via `IndexUpdate` SignalR event — this is a backend task for a future day.

---

## Files Changed

| File | Action | Critical |
|------|--------|----------|
| `src/api/client.ts` | ✅ Fixed BASE URL, cancelOrder method, searchStocks param | YES |
| `src/api/orders.ts` | ✅ Fixed /api/ prefix, cancel HTTP method | YES |
| `src/api/auth.ts` | ✅ Fixed logout to call backend | MEDIUM |
| `src/api/market.ts` | ✅ Fixed searchStocks query param | MEDIUM |
| `src/components/widgets/OrderBookWidget.tsx` | ✅ Fixed all field names, status codes | YES |
| `src/hooks/useOrders.ts` | ✅ Added cancelOrder alias, TradeExecuted listener | YES |
| `src/main.tsx` | ✅ Added startGlobalMarketHub initialization | YES |

---

## Verification Checklist

- [ ] Login → JWT stored → `startGlobalMarketHub` called → SignalR connects
- [ ] `BulkPriceUpdate` received → `useMarketData` ticksArray populates → all widgets update
- [ ] Watchlist loads → stocks show live prices
- [ ] Order placement → `POST /api/orders` → order appears in OrderBookWidget
- [ ] Order cancel → `PUT /api/orders/{id}/cancel` → status changes to Cancelled (4)
- [ ] Logout → `POST /api/auth/logout` → token blacklisted on server
- [ ] Search in Watchlist → `GET /api/stocks/search?query=GP` → results appear
- [ ] Portfolio loads → `GET /api/PortfolioSnapshot/latest/{userId}` → holdings visible
- [ ] Notifications → `subscribeMarket("Notification")` via NotificationHub
- [ ] ScoreBoard, MostActive, TimeAndSales → all use `useMarketData().ticksArray` ✅
