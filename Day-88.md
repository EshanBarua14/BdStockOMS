# Day 88 — Compliance Reporting

## Branch
`day-88-compliance-reporting`

## What was built
- `ComplianceReport` model — enums: AlertType (8 types), Severity, Status
- `ComplianceSettings` POCO + appsettings.json section
- `IComplianceService` interface (8 methods)
- `ComplianceService` — 4 detection engines:
  - Large trade alert (3-tier severity: Medium/High/Critical)
  - AML structuring (count + volume window check)
  - Wash trade (same StockId, opposite OrderType, time window)
  - Unusual frequency (orders/hour threshold)
- `ComplianceController` — 8 endpoints (scan order, scan investor, list, get, resolve, escalate, summary, export CSV)
- `Permissions.ComplianceManage` added
- EF migration: `Day88_ComplianceReports` table

## Tests
- Day 87 baseline: 1,390
- Day 88 new: 38
- **Total: 1,428 passing, 0 failures**

## Next
Day 89: Corporate actions — dividend, bonus, rights issue processing, portfolio adjustment
