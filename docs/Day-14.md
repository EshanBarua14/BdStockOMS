# Day 14 — Commission Calculation Engine

**Branch:** day-14-commission-engine
**Tests:** 136 passing (was 121, +15 new tests)

## What Was Built

### CommissionCalculatorService
- 3-tier rate resolution: Investor → Brokerage → System → Default (0.5%)
- CalculateBuyCommissionAsync — trade value + all charges
- CalculateSellCommissionAsync — trade value - all charges
- GetEffectiveBuyRateAsync / GetEffectiveSellRateAsync — tier resolution
- CalculateFromRateAsync — pure calculation given a rate

### BD Market Fixed Charges
- CDBL: 0.015% of trade value
- DSE/CSE fee: 0.05% of trade value
- Default broker commission: 0.5% (buy and sell)

### CommissionBreakdown DTO
- TradeValue, BrokerCommission, CDBLCharge, ExchangeFee
- TotalCharges, NetAmount, CommissionRate, Exchange, OrderType

### CommissionController (4 endpoints)
- POST /api/commission/calculate — calculate for any trade value
- GET /api/commission/rates — get my effective rates
- GET /api/commission/system-rates — SuperAdmin/Admin/Compliance view
- POST /api/commission/system-rates — SuperAdmin only, deactivates old rate

## Tests Added (CommissionCalculatorTests.cs — 15 tests)
- Buy order adds charges to trade value
- Sell order deducts charges from trade value
- CSE and DSE fee rates applied correctly
- CDBL rate is always fixed at 0.015%
- Large trade value calculation (1 crore BDT)
- Default rate when no DB rates exist
- System rate priority over default
- Brokerage rate priority over system rate
- Investor rate priority over brokerage rate
- Buy/sell commission valid requests succeed
- Zero and negative trade values return failure
- TotalCharges equals sum of all components
