# Day 09 — CCD APIs + SignalR Real-Time Prices

## What Was Built

### CCD APIs
- `POST /api/ccd/bo-accounts/open` — CCD opens BO account for investor
- `POST /api/ccd/bo-accounts/deposit` — CCD deposits cash into account
- `PUT /api/ccd/bo-accounts/margin` — CCD sets margin limit
- `PUT /api/ccd/bo-accounts/{userId}/activate` — Activate BO account
- `PUT /api/ccd/bo-accounts/{userId}/deactivate` — Freeze BO account
- `GET /api/ccd/bo-accounts` — List all investor BO accounts
- `PUT /api/ccd/orders/{orderId}/settle` — Settle executed order + update portfolio

### SignalR — Real-Time Stock Prices
- `StockPriceHub` at `/hubs/stockprice`
- Clients connect and subscribe to specific stocks
- `StockPriceUpdateService` broadcasts price updates every 5 seconds
- Respects circuit breaker limits during simulation
- Broadcasts both individual stock updates and full market summary

### Portfolio Auto-Update
- BUY settlement → shares added to portfolio, average price recalculated
- SELL settlement → shares removed, cash credited to investor account

## Settlement Flow
```
Order Executed → CCD reviews → PUT /settle
→ Portfolio updated
→ Order marked Completed
```

## New Files
- DTOs/CCD/BOAccountDto.cs
- Services/CCDService.cs
- Controllers/CCDController.cs
- Hubs/StockPriceHub.cs
- BackgroundServices/StockPriceUpdateService.cs
- Tests/Unit/CCDServiceTests.cs

## Tests
- Previous: 44 passing
- New: +8 CCDServiceTests
- **Total: 52 passing, 0 failing**

## Next: Day 10
- React 18 + Vite + Tailwind setup
- Login and Register pages
- JWT stored in memory (not localStorage)
- Role-based routing
