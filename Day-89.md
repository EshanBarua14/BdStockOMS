# Day 89 — Corporate Actions

## Branch
`day-89-corporate-actions`

## What was built
- CorporateActionLedger model (DividendCash, BonusShareCredit, RightsEntitlement)
- ProcessAsync engine — applies actions to all Portfolio holdings:
  - Dividend: cash amount per share, logged to ledger, quantity unchanged
  - Bonus share: shares = floor(qty * ratio), added to portfolio
  - Rights issue: entitlement = floor(qty * ratio), added to portfolio
- GetLedgerAsync — per-investor breakdown of a processed action
- New endpoints: POST /{id}/process, GET /{id}/ledger
- EF migration: Day89_CorporateActionLedger

## Tests
- Day 88 baseline: 1,428
- Day 89 new: 36
- Total: 1,464 passing, 0 failures

## Next
Day 90: IPO module
