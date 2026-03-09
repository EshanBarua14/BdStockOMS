# Day 35 - KYC Workflow

## Summary
Implemented full KYC (Know Your Customer) document workflow for investor onboarding.

## What Was Built

### Models
- `KycDocument` - stores investor identity documents with status tracking
- `KycApproval` - audit trail of every review action taken on a document
- `KycDocumentType` enum - NationalId, Passport, DrivingLicense, BirthCertificate, TaxIdentificationNumber, BankStatement, UtilityBill
- `KycStatus` enum - Pending, UnderReview, Approved, Rejected, Expired
- `ApprovalAction` enum - full lifecycle actions

### Service
- `IKycService` / `KycService`
  - `SubmitDocumentAsync` - investor submits a document, blocks duplicate pending submissions
  - `ReviewDocumentAsync` - CCD agent approves/rejects, creates approval audit record
  - `GetDocumentsByUserAsync` - investor views their own documents
  - `GetPendingDocumentsAsync` - CCD sees pending docs for their brokerage house
  - `IsKycApprovedAsync` - RMS guard check before trading
  - `GetDocumentByIdAsync` - fetch single document with history
  - `GetApprovalHistoryAsync` - full audit trail per document

### Controller
- `KycController` - REST endpoints with role-based authorization
  - POST /api/kyc/submit (Investor only)
  - POST /api/kyc/review (CCD, SuperAdmin only)
  - GET /api/kyc/user/{userId}
  - GET /api/kyc/pending/{brokerageHouseId}
  - GET /api/kyc/status/{userId}
  - GET /api/kyc/{id}
  - GET /api/kyc/{id}/history

### Database
- Migration: `AddKycTables` - KycDocuments + KycApprovals tables
- Also fixed cascade path issues in Day32 (CommissionLedgers) and Day33 (SettlementItems) migrations

## Tests
- Previous: 390
- Today: 416
- Added: 26 new KYC tests
