# Day 31 — Real RMS Engine

## Branch
`day-31-rms-engine`

## Status
✅ Complete — 342 tests passing, 0 failures (up from 325)

---

## What Was Built

### TradeAlert Model
- Fields: `InvestorId`, `BrokerageHouseId`, `OrderId`, `AlertType`, `Severity`, `Message`, `ThresholdValue`, `ActualValue`, `IsAcknowledged`, `AcknowledgedAt`, `CreatedAt`
- `TradeAlertType` enum: `OrderValueLimit`, `DailyExposureLimit`, `StockConcentration`, `SectorConcentration`, `CashBalanceInsufficient`, `MarginBreached`
- `TradeAlertSeverity` enum: `Warning`, `Breach`
- Added `DbSet<TradeAlert> TradeAlerts` to AppDbContext
- `HasPrecision(18,4)` on `ThresholdValue` and `ActualValue`

### RMS Engine Upgrades
- **New check: `CheckCashBalanceAsync`** — compares investor's `User.CashBalance` against order value
  - Blocks if `cashBalance < orderValue`
  - Warns if `cashBalance < orderValue * 1.1`
- All individual check methods now set `IsAllowed = false` when violations exist
- Cash balance check runs first on BUY orders in `ValidateOrderAsync`
- **TradeAlert creation** on every breach or warning via `CreateTradeAlertAsync`
- **SignalR push** to `user-{investorId}` group on every RMS breach/warning (`RmsAlert` event)

### Migration
`Day31_TradeAlerts` — adds `TradeAlerts` table

---

## Tests
| File | Tests Added |
|------|-------------|
| `RMSEngineTests.cs` | 17 |
| **Total new** | **17** |

- Previous: 325 → Current: **342**

---

## Next: Day 32
- Real Commission Engine
- Hierarchy: InvestorRate → BrokerageRate → DefaultRate
- Commission per Trade (not per order)
- `CommissionLedger` table
- CDBL fee + DSE/CSE fee + broker fee all separate
- Target: 370+ tests
