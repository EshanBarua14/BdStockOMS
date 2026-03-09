# Day 38 - Broker-Specific Settings

## Summary
Implemented per-broker configuration system with feature toggles, RMS limits, trading hours, and branch office management.

## What Was Built

### Models
- `BrokerageSettings` - per-broker config: RMS limits, feature toggles, trading hours
- `BranchOffice` - branch offices under a brokerage house

### Service
- `IBrokerageSettingsService` / `BrokerageSettingsService`
  - `GetOrCreateSettingsAsync` - auto-creates defaults if none exist
  - `UpdateSettingsAsync` - update all broker settings
  - `IsFeatureEnabledAsync` - check feature toggle by name
  - `IsWithinTradingHoursAsync` - BST-aware trading hours check
  - `CreateBranchAsync` - create branch with unique code per broker
  - `UpdateBranchAsync` - update branch details
  - `DeactivateBranchAsync` - soft delete branch
  - `GetBranchesAsync` - list all branches for a broker
  - `GetBranchByIdAsync` - fetch single branch

### Feature Toggles
- MarginTrading, ShortSelling, SmsAlert, EmailAlert, AutoSettlement, TwoFactor

### Controller
- `BrokerageSettingsController` - 9 endpoints with role-based authorization

### Database
- Migration: `AddBrokerageSettingsAndBranches`
- Unique index on BrokerageSettings (BrokerageHouseId) - one settings record per broker
- Unique index on BranchOffices (BrokerageHouseId, BranchCode)

## Tests
- Previous: 469
- Today: 494
- Added: 25 new broker settings tests
