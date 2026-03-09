# Day 37 - Back Office File Integration

## Summary
Implemented CSV file import pipeline with staging, validation, approval, and commit workflow.

## What Was Built

### Models
- `FileImportBatch` - tracks an uploaded file through its lifecycle
- `FileImportRow` - individual rows within a batch with validation results
- `ImportFileType` enum - TradeUpload, PortfolioUpload, FundUpload, InvestorUpload
- `ImportBatchStatus` enum - Staged, Validating, ValidationFailed, PendingApproval, Approved, Committed, Rejected
- `RowStatus` enum - Pending, Valid, Invalid, Committed, Skipped

### Service
- `IFileImportService` / `FileImportService`
  - `StageAsync` - parse CSV, create batch + row records
  - `ValidateAsync` - validate each row by file type rules, mark valid/invalid
  - `ApproveAsync` - CCD/Admin approves a validated batch
  - `RejectAsync` - reject with reason
  - `CommitAsync` - mark all valid rows as committed
  - `GetBatchAsync` - fetch batch with rows
  - `GetBatchesByBrokerageHouseAsync` - list batches for a brokerage
  - `GetRowsAsync` - fetch rows for a batch

### Validation Rules
- TradeUpload: StockCode, Side (BUY/SELL), Quantity (int > 0), Price (decimal > 0)
- FundUpload: Email, Amount (decimal > 0)
- InvestorUpload: FullName, Email (must contain @), Phone

### Controller
- `FileImportController` - 8 endpoints with role-based authorization

### Database
- Migration: `AddFileImportTables`

## Tests
- Previous: 440
- Today: 469
- Added: 29 new file import tests
