# Day 90 — IPO Module

## Branch
`day-90-ipo-module`

## What was built
- IPO model (status lifecycle: Upcoming→Open→Closed→Allocated→Refunded→Listed)
- IPOApplication model (Pending→Allocated/Rejected→Refunded)
- IIPOService + IPOService:
  - CreateIPOAsync — validation (date order, min/max)
  - ApplyAsync — open-only, min/max investment, duplicate guard
  - CloseIPOAsync — Open→Closed transition
  - AllocateAsync — pro-rata on oversubscription, refund calculation
  - ProcessRefundsAsync — marks refunds, Allocated→Refunded
  - GetApplicationsAsync / GetApplicationAsync
- IPOController — 9 endpoints
- EF migration: Day90_IPOModule

## Tests
- Day 89 baseline: 1,464
- Day 90 new: 38
- Total: 1,502 passing, 0 failures

## Next
Day 91: T-bond module
