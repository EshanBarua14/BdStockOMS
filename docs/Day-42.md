# Day 42 — Tenant DB Provisioning + BOS Compliance Checklist + Contract Notes

## Summary
- **Tests:** 565 → 603 (+38 new tests)
- **Branch:** day-42-tenant-db-compliance
- **Build:** Clean (0 errors, 0 failures)

## What Was Built

### 1. Tenant DB Provisioning
**Files:**
- `BdStockOMS.API/Services/Interfaces/ITenantProvisioningService.cs`
- `BdStockOMS.API/Services/TenantProvisioningService.cs`
- `BdStockOMS.API/Controllers/TenantProvisioningController.cs`
- `BdStockOMS.API/Models/BrokerageConnection.cs`

**Endpoints (SuperAdmin only):**
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/tenantprovisioning/provision` | Provision new tenant DB |
| POST | `/api/tenantprovisioning/{id}/activate` | Activate tenant |
| POST | `/api/tenantprovisioning/{id}/deactivate` | Deactivate tenant |
| POST | `/api/tenantprovisioning/{id}/migrate` | Run migration instructions |
| GET  | `/api/tenantprovisioning/{id}/health` | Tenant health status |
| GET  | `/api/tenantprovisioning/all` | All tenant summaries |

**Architecture decisions:**
- DB name sanitization: `BdStockOMS_{BrokerageName}` (strips all non-alphanumeric)
- Connection strings stored in `BrokerageConnections` table in master DB
- Per-tenant DB migrations are DBA-managed via CLI (per approved architecture)
- `TenantHealthStatus.Status` computed property: Healthy / NeedsMigration / ConnectionFailed / Inactive

### 2. FlexTrade BOS Compliance Checklist
**Files:**
- `BdStockOMS.API/Services/Interfaces/IFlextradeBosComplianceService.cs`
- `BdStockOMS.API/Services/FlextradeBosComplianceService.cs`
- `BdStockOMS.API/Services/BosComplianceHostedService.cs`
- `BdStockOMS.API/Controllers/BosComplianceController.cs`

**10-Point Checklist:**
| # | Check | Severity |
|---|-------|----------|
| 1 | BrokerageSettingsExist | Critical |
| 2 | TrecNumberConfigured (LicenseNumber) | Critical |
| 3 | ActiveBranchExists | Critical |
| 4 | CommissionRatesConfigured | Warning |
| 5 | RmsLimitsConfigured | Critical |
| 6 | ActiveTraderExists | Critical |
| 7 | BosImportWithin24Hours | Critical |
| 8 | BoAccountFormatValid | Warning |
| 9 | KycQueueClear (>48h pending) | Warning |
| 10 | SettlementUpToDate (no Failed items) | Critical |

**Endpoints (SuperAdmin only):**
| Method | Route | Description |
|--------|-------|-------------|
| GET  | `/api/boscompliance/{id}` | Get cached report |
| POST | `/api/boscompliance/{id}/refresh` | Force live check |
| GET  | `/api/boscompliance/all` | All brokerage reports |
| POST | `/api/boscompliance/refresh-all` | Refresh all |

**Auto-run:** `BosComplianceHostedService` runs at midnight UTC daily via `BackgroundService`.
**Caching:** Redis with 25-hour TTL per brokerage report.

### 3. Contract Notes
**Files:**
- `BdStockOMS.API/Services/Interfaces/IContractNoteService.cs`
- `BdStockOMS.API/Services/ContractNoteService.cs`
- `BdStockOMS.API/Controllers/ContractNoteController.cs`
- `BdStockOMS.API/Models/ContractNote.cs`
- `BdStockOMS.API/Models/BosImportLog.cs`

**Fee structure (DSE standard):**
- Commission: configurable rate (default 0.5%)
- CDSC fee: 0.05%
- Levy charge: 0.03%
- VAT on commission: 15%
- Buy: net = gross + all charges
- Sell: net = gross - all charges

**Endpoints:**
| Method | Route | Description |
|--------|-------|-------------|
| POST | `/api/contractnote/generate/{orderId}` | Generate contract note |
| GET  | `/api/contractnote/{id}` | Get by ID |
| GET  | `/api/contractnote/client/{clientId}` | By client (with date filter) |
| GET  | `/api/contractnote/date/{date}` | By trade date |
| POST | `/api/contractnote/regenerate/{orderId}` | Void + regenerate |
| GET  | `/api/contractnote/{id}/export` | Export as text |

**Contract note number format:** `CN-{yyyyMMdd}-{orderId:D6}`
**Settlement date:** Trade date + 2 business days (T+2)

## New Tests (Day42Tests.cs — 38 tests)
- `TenantProvisioningServiceTests` (8) — SanitizeDatabaseName variants
- `BosComplianceReportTests` (9) — IsCompliant, PassedCount, FailedCount, check names
- `ContractNoteModelTests` (12) — fee calculations, defaults, result objects
- `BrokerageConnectionModelTests` (9) — model defaults, BosImportLog, TenantSummary

## AppDbContext Changes
Added DbSets:
- `ContractNotes`
- `BosImportLogs`
- `BrokerageConnections`

## Next: Day 43
- DSE FlexTrade BOS XML parsing
- Clients-UBR.xml + Clients-UBR-ctrl.xml (MD5 verification)
- Positions-UBR.xml + Positions-UBR-ctrl.xml (MD5 verification)
- Client/position reconciliation
- RBAC dashboard
- EOD export back to DSE-compatible XML
- Target: 620+ tests
