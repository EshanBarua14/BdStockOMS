# Day 32 — Real Commission Engine

## Branch
`day-32-commission-engine`

## Status
✅ Complete — 357 tests passing, 0 failures (up from 342)

---

## What Was Built

### CommissionLedger Model
- Fields: `TradeId`, `OrderId`, `InvestorId`, `BrokerageHouseId`, `Exchange`, `Side`, `TradeValue`, `BrokerCommission`, `CDBLCharge`, `ExchangeFee`, `TotalCharges`, `NetAmount`, `CommissionRate`, `PostedAt`
- All decimal fields: `HasPrecision(18, 4)` — `CommissionRate` uses `HasPrecision(18, 6)`
- Added `DbSet<CommissionLedger> CommissionLedgers` to AppDbContext

### ICommissionLedgerService + CommissionLedgerService
- `PostTradeCommissionAsync(trade, exchange)` — calculates and persists commission for a trade
  - BUY: `NetAmount = TradeValue + TotalCharges`
  - SELL: `NetAmount = TradeValue - TotalCharges`
  - Uses existing `ICommissionCalculatorService` hierarchy (Investor → Brokerage → Default)
- `GetInvestorLedgerAsync(investorId, from, to)` — filterable ledger history
- `GetTotalCommissionAsync(brokerageHouseId, from, to)` — broker revenue summary

### Commission Hierarchy (existing, now wired to ledger)
```
InvestorCommissionRate → BrokerageCommissionRate → CommissionRate (system) → Default 0.5%
```

### Fee Breakdown Per Trade
| Fee | Rate |
|-----|------|
| Broker Commission | Investor/Brokerage/System rate |
| CDBL Charge | 0.015% of trade value |
| DSE/CSE Fee | 0.05% of trade value |

### Migration
`Day32_CommissionLedger` — adds `CommissionLedgers` table

---

## Tests
| File | Tests Added |
|------|-------------|
| `CommissionLedgerTests.cs` | 15 |
| **Total new** | **15** |

- Previous: 342 → Current: **357**

---

## Next: Day 33
- Settlement Engine (T+2)
- `SettlementBatches` + `SettlementItems` models
- Background service runs at market close
- Target: 385+ tests
