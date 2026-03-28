# Day 76 - Admin Settings Full Wiring

## Problem
AdminSettingsPage had hardcoded BASE URL (https://localhost:7219/api) and
several sections using mock useState data instead of real API calls.

## Fixes Applied

### Fix 1 - BASE URL
Changed: const BASE = 'https://localhost:7219/api'
To:      const BASE = '/api'
All apiFetch calls now go through Vite proxy correctly.

### Fix 2 - Fees
Added useEffect to load GET /admin/fees on mount.
Previously showed hardcoded Standard A/B and Z Category entries only.

### Fix 3 - API Keys
Added useEffect to load GET /admin/api-keys on mount.
Previously showed two hardcoded sk_live_ entries.

### Fix 4 - Announcements
Added useEffect to load GET /admin/announcements on mount.
Previously showed two hardcoded announcements.

### Fix 5 - IP Whitelist
Added useEffect to load GET /admin/ip-whitelist on mount.
Previously showed two hardcoded IP entries.

### Fix 6 - Backup History
Added useEffect to load GET /admin/backup/history on mount.
Previously showed hardcoded backup history entries.

### Fix 7 - System Health
Added useEffect to load GET /admin/health on mount.
Previously showed hardcoded health metrics.

### Fix 8 - Roles
Added useEffect to load GET /admin/roles on mount.
Previously showed hardcoded role list.

## What was already wired (no changes needed)
- General Settings: GET/PUT /admin/settings/general
- Market Settings: GET/PUT /admin/settings/market
- Trading Rules: GET/PUT /admin/settings/trading-rules
- Notifications: GET/PUT /admin/settings/notifications
- FIX Engine: GET/PUT /admin/fix/config, POST connect/disconnect
- Backup Config: GET/PUT /admin/backup/config
- Audit Log: GET /admin/audit-log with filters
- Data Retention: GET/PUT /admin/settings/data-retention

## Tests - Day76Tests.cs - 8 tests
GeneralSettingsDto construction, MarketSettingsDto construction,
BackupConfigDto construction, S3 keys null by default,
FeeStructureDto construction, all 15 routes exist,
BASE URL is relative, DataRetentionDto construction

## Next - Day 77
Ctrl+K command palette + global search
