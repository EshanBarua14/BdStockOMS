# Day 68 - Risk Management System (RMS) UI

**Branch:** `day-68-rms-ui`
**Tests:** 895 (start) -> 910 (end) | +15 tests
**All tests:** 910 backend passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | RMSManagementPage.tsx — full limit management UI | Done |
| 2 | Summary cards: Total/Investor/Block/Warn counts | Done |
| 3 | Tabs: All / Investor / Trader / Market+Sector | Done |
| 4 | Filter by brokerage + entity type | Done |
| 5 | Set Limit modal — Level, EntityType, Brokerage, Investor picker | Done |
| 6 | Action on Breach selector: Warn / Block / Freeze | Done |
| 7 | GET /api/rms/limits endpoint added to RMSController | Done |
| 8 | App.tsx route /rms wired to RMSManagementPage | Done |
| 9 | Day68Tests.cs — 15 RMSLimit entity + DB tests | Done |
| 10 | Build passing: 0 errors | Done |

---

## New API Endpoint

| Method | Route | Description |
|--------|-------|-------------|
| GET | /api/rms/limits | All active RMS limits (filter: brokerageHouseId, entityType) |

Existing endpoints used:
| POST | /api/rms/set-limit | Create/replace limit for entity |
| GET | /api/rms/investor/{id} | Limits for specific investor |
| GET | /api/rms/my-limits | Current user limits |

---

## RMS Limit Fields

| Field | Type | Description |
|-------|------|-------------|
| Level | RMSLevel enum | Investor/Trader/Stock/Sector/Market/Exchange |
| EntityType | string | Matches Level name |
| EntityId | int? | null = global limit for all entities of this type |
| MaxOrderValue | decimal | Max single order in BDT |
| MaxDailyValue | decimal | Max total orders per day in BDT |
| MaxExposure | decimal | Max portfolio exposure in BDT |
| ConcentrationPct | decimal | Max % of portfolio in single stock |
| ActionOnBreach | RMSAction | Warn=1 / Block=2 / Freeze=3 |

---

## Test Count Progression

| Day | Backend | Total | Notes |
|-----|---------|-------|-------|
| 67 | 895 | 895 | Start |
| 68 | 910 | 910 | +15 RMSLimit entity + DB tests |

---

## Next: Day 69 - T&S Multi-Symbol + Widget Drawer Drag-to-Reorder