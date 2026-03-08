# Day 15 — RMS (Risk Management System) Engine

**Branch:** day-15-rms-engine
**Tests:** 148 passing (was 136, +12 new tests)

## What Was Built

### RMSValidationService
- ValidateOrderAsync — runs all 4 checks before order placement
- CheckOrderValueLimitAsync — single order value vs limit
- CheckDailyExposureAsync — today's total orders vs daily limit
- CheckConcentrationAsync — single stock % of portfolio (BSEC 10% rule)
- CheckSectorConcentrationAsync — sector % of portfolio
- 3-tier limit resolution: Investor RMSLimit → Default BSEC limits
- Warnings at 80% of any limit, violations block the order
- Audit log on every blocked order

### Default BSEC Limits (when no RMSLimit configured)
- Max order value: 50 lakh BDT
- Max daily exposure: 2 crore BDT
- Max total exposure: 5 crore BDT
- Max stock concentration: 10%
- Max sector concentration: 25%

### RMSController (3 endpoints)
- POST /api/rms/validate-order — check before placing order
- GET /api/rms/my-limits — view investor's active limits
- POST /api/rms/set-limit — SuperAdmin/Admin/Compliance set limits

### Bug Fixes
- OrderStatus comparison fixed to use enum (not string)
- Stock.Category is StockCategory enum — SectorName compared via .ToString()
- Order price field is PriceAtOrder (not Price)

## Tests Added (RMSValidationTests.cs — 12 tests)
- Order value below limit allowed
- Order value exceeds default limit blocked
- Order value above 80% triggers warning
- Custom RMS limit overrides default
- Daily exposure no orders today allowed
- Daily exposure exceeds limit blocked
- Concentration empty portfolio allowed
- Concentration exceeds 10% blocked
- Full validation valid order allowed
- Full validation exceeds order limit blocked
- Sell order skips concentration check
- Multiple violations all captured
