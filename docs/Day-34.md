# Day 34 — Market Depth + Enhanced Market Data

## Branch
`day-34-market-depth`

## Status
✅ Complete — 390 tests passing, 0 failures (up from 374)

---

## What Was Built

### MarketDepth Model
- Stores 5-level bid/ask order book per stock
- Fields: `StockId`, `Exchange`, `Bid1-5 Price/Qty`, `Ask1-5 Price/Qty`, `UpdatedAt`
- Unique index on `StockId` — one depth record per stock (upsert pattern)
- All price fields: `HasPrecision(18, 4)`

### IMarketDepthService + MarketDepthService
- `GetDepthAsync(stockId)` — returns cached depth snapshot from DB
- `RefreshDepthAsync(stockId)` — calls `IExchangeConnector.GetMarketDepthAsync()`, upserts to DB
- `UpsertDepthAsync(stockId, dto)` — maps `MarketDepthDto` → `MarketDepth` entity
- `MarketDepthSnapshot` record — clean DTO with `List<DepthLevelDto>` for bids/asks

### Key Design Decisions
- Upsert pattern: one row per stock, updated in place (not append)
- Exchange connector resolved via `IExchangeConnectorFactory` using stock's exchange code
- Bids stored in descending price order, Asks in ascending price order
- `SimulatedExchangeConnector` already implements `GetMarketDepthAsync` (Day 29)

### Migration
`Day34_MarketDepth` — adds `MarketDepths` table with unique index on `StockId`

---

## Tests
| File | Tests Added |
|------|-------------|
| `MarketDepthServiceTests.cs` | 16 |
| **Total new** | **16** |

- Previous: 374 → Current: **390**

---

## Next: Day 35
- KYC Workflow
- `KycDocuments` + `KycApprovals` models
- Investor cannot trade until KYC approved
- CCD role manages approvals
- Target: 415+ tests
