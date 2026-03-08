# Day 25 — Backend Hardening

## Branch
`day-25-backend-hardening`

## Summary
Added DataAnnotations validation to all newer DTOs and configured a standardized
validation error response format across the entire API.

## What Changed

### DataAnnotations Added To
- `DTOs/MarketData/MarketDataDtos.cs` — Exchange required, prices > 0, page range
- `DTOs/CorporateAction/CorporateActionDtos.cs` — Type required, Value > 0, Description max 500
- `DTOs/News/NewsDtos.cs` — Title 3-200 chars, Content min 10 chars, Category required
- `DTOs/OrderAmendment/OrderAmendmentDtos.cs` — Quantity > 0, Price > 0, Reason max 500
- `DTOs/TraderReassignment/TraderReassignmentDtos.cs` — InvestorId/NewTraderId > 0
- `DTOs/SystemSettings/SystemSettingDtos.cs` — Key lowercase/underscore only, Value max 1000

### Standardized Validation Response (Program.cs)
All validation failures now return:
```json
{
  "message": "Validation failed",
  "errors": ["Field X is required.", "Value must be greater than zero."]
}
```

### Tests
- `Tests/Unit/ValidationTests.cs` — 17 new tests covering all 6 DTO groups

## Test Results
- Previous: 264 passing
- Today: 281 passing (+17)
- Failed: 0

## Key Validation Rules Enforced
| DTO | Key Rules |
|-----|-----------|
| CreateMarketDataDto | Exchange 2-10 chars, all prices > 0 |
| CreateCorporateActionDto | Type required, Value > 0 |
| CreateNewsDto | Title 3-200 chars, Content min 10 chars |
| AmendOrderDto | Quantity > 0, Price > 0 |
| CreateTraderReassignmentDto | Both IDs must be positive |
| CreateSystemSettingDto | Key = lowercase + underscores only |
