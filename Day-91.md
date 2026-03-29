# Day 91 — T-Bond Module

## Branch
`day-91-tbond-module`

## What was built
- TBond model (ISIN, CouponRate, CouponFrequency, IssueDate, MaturityDate)
- TBondOrder model (Buy/Sell, Pending→Executed→Settled/Cancelled)
- CouponPayment model (period-based, IsPaid tracking)
- TBondHolding model (FaceValueHeld, AverageCost, weighted average on accumulation)
- ITBondService + TBondService:
  - Order routing: place, execute (updates holdings), settle, cancel
  - Coupon generation (all frequencies), idempotent, per-investor
  - PayCoupons — pays all due up to a date
  - ProcessMaturity — zeroes holdings, sets bond Matured
  - GetHoldings per investor
- TBondController — 13 endpoints
- EF migration: Day91_TBondModule

## Tests
- Day 90 baseline: 1,495
- Day 91 new: 40
- Total: 1,535 passing, 0 failures

## Next
Day 92: Advanced portfolio analytics
