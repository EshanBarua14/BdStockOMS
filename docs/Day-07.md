# Day 07 — Model Updates + Database Migration

## What Was Built

### Updated Models
- **Stock** — Added `Category` (A/B/G/N/Z/Spot), `CircuitBreakerHigh`, `CircuitBreakerLow`, `BoardLotSize`
- **Order** — Added `OrderCategory` (Market/Limit), `LimitPrice`, `SettlementType` (T+2/T+0), `PlacedBy`, `RejectionReason`. Removed `Approved` status (no admin approval needed — direct to exchange)
- **User** — Added BO Account fields: `BONumber`, `AccountType` (Cash/Margin), `CashBalance`, `MarginLimit`, `MarginUsed`, `IsBOAccountActive`

### New Model
- **Portfolio** — Tracks investor stock holdings: `InvestorId`, `StockId`, `Quantity`, `AverageBuyPrice`

### Roles Updated (6 → 7)
| Id | Role |
|----|------|
| 1 | SuperAdmin |
| 2 | BrokerageHouse |
| 3 | Admin |
| 4 | CCD |
| 5 | ITSupport |
| 6 | Trader |
| 7 | Investor |

### Real DSE/CSE Business Rules Applied
- Z/Spot category stocks → T+0 settlement, no margin allowed
- A/B/G/N category stocks → T+2 settlement
- Circuit breaker = ±10% of previous close price
- Orders outside circuit breaker range → auto rejected
- No admin approval step — orders go directly to exchange
- Both Investor and Trader can place orders

### Migration
- `Day07_ModelUpdates` — applied successfully to SQL Server

## Tests
- Previous: 34 passing
- Fixed: 1 broken test (OrderStatus.Approved removed)
- **Total: 34 passing, 0 failing**

## Next: Day 08
- Order placement APIs with full validation
- Investor-Trader assignment APIs
- Portfolio tracking
