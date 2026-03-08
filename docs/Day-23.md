# Day 23 — System Settings Management

## Branch
`day-23-system-settings`

## Summary
Built a key-value system settings service that lets SuperAdmins manage
app-wide configuration stored in the database instead of hardcoded values.

## Use Cases
- market_open_time / market_close_time — trading hours control
- maintenance_mode — put app in maintenance without redeployment
- max_order_value — change trading limits dynamically
- session_timeout — security policy changes without code changes

## Files Created
### DTOs
- `DTOs/SystemSettings/SystemSettingDtos.cs`
  - SystemSettingResponseDto — full setting with who last updated it
  - CreateSystemSettingDto — key, value, category, description, isEncrypted
  - UpdateSystemSettingDto — value + optional description update

### Services
- `Services/ISystemSettingService.cs` + `SystemSettingService.cs`
  - GetAllAsync — list all settings, filterable by category
  - GetByKeyAsync — fetch one setting by key name
  - CreateAsync — create new setting, enforces unique key
  - UpdateAsync — update value, logs who changed it and when
  - DeleteAsync — remove a setting permanently

### Controllers
- `Controllers/SystemSettingController.cs`
  - GET /api/systemsetting — any authenticated user (to read market hours etc)
  - GET /api/systemsetting/{key} — any authenticated user
  - POST /api/systemsetting — SuperAdmin only
  - PUT /api/systemsetting/{key} — SuperAdmin, Admin
  - DELETE /api/systemsetting/{key} — SuperAdmin only

### Program.cs
- Registered ISystemSettingService

### Tests
- `Tests/Unit/SystemSettingServiceTests.cs` — 14 new tests

## Test Results
- Previous: 238 passing
- Today: 252 passing (+14)
- Failed: 0

## API Endpoints Added
| Method | Route | Auth |
|--------|-------|------|
| GET | /api/systemsetting | Any authenticated |
| GET | /api/systemsetting/{key} | Any authenticated |
| POST | /api/systemsetting | SuperAdmin |
| PUT | /api/systemsetting/{key} | SuperAdmin, Admin |
| DELETE | /api/systemsetting/{key} | SuperAdmin |
