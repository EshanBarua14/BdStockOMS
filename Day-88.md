# Day 88 — Compliance Reporting

## Branch
`day-88-compliance-reporting`

## Completed
- ComplianceReport model (enums: AlertType, Severity, Status)
- ComplianceSettings POCO + appsettings.json section
- IComplianceService interface (8 methods)
- ComplianceService implementation:
  - CheckLargeTrade (threshold-based, 3 severity tiers)
  - CheckAMLStructuring (count + volume window)
  - CheckWashTrade (same symbol, opposite side, time window)
  - CheckUnusualFrequency (orders/hour threshold)
- ComplianceController (8 endpoints)
- Permissions: ComplianceView, ComplianceManage
- EF migration: Day88_ComplianceReports
- 50 new tests in Day88Tests.cs

## Tests
- 1,390 (Day 87) + 50 (Day 88) = **1,440 passing, 0 failures**

## Next
Day 89: Corporate actions (dividend, bonus, rights issue)
