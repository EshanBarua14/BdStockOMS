# Day 46 - VAPT (Vulnerability Assessment & Penetration Testing)

**Branch:** `day-46-vapt`
**Tests:** 681 (start) -> 717 (end) | +36 tests
**All tests:** 717 passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | IDOR vulnerability tests | Done |
| 2 | JWT tampering tests (alg:none, expired, wrong sig, escalation) | Done |
| 3 | Auth bypass tests (no token, wrong verb, all protected routes) | Done |
| 4 | Build clean - 0 errors | Done |
| 5 | 717 tests passing | Done |

---

## New Test File

**File:** `BdStockOMS.Tests/Security/VaptTests.cs`

3 test classes, 36 new tests total.

---

## IDOR Tests (12 tests)

Verify that unauthenticated requests to user-scoped endpoints are rejected.

| Test | Endpoint | Expected |
|------|----------|----------|
| IDOR_Portfolio_OtherUser_NoToken_Returns401 | GET /api/Portfolio/9999/summary | 401 |
| IDOR_Portfolio_NegativeId_NoToken_Returns401 | GET /api/Portfolio/-1/summary | 401 |
| IDOR_KycEndpoint_OtherUser_NoToken_Returns401 | GET /api/Kyc/pending/9999 | 401 |
| IDOR_BosExport_OtherBrokerage_NoToken_Returns401 | GET /api/bos/export/positions/9999 | 401 |
| IDOR_Order_OtherUser_NoToken_Returns401 | GET /api/orders/9999 | 401 |
| IDOR_FundRequest_OtherUser_NoToken_Returns401 | GET /api/fund-requests | 401 |
| IDOR_Watchlist_OtherUser_NoToken_Returns401 | GET /api/watchlists | 401 |
| IDOR_AdminDashboard_NoToken_Returns401 | GET /api/AdminDashboard | 401 |
| IDOR_AuditLogs_NoToken_Returns401 | GET /api/AuditCompliance/logs | 401 |
| IDOR_BrokerageReport_NoToken_Returns401 | GET /api/BrokerageReport/1/orders | 401/403/404 |
| IDOR_PortfolioSnapshot_OtherUser_NoToken_Returns401 | GET /api/PortfolioSnapshot/history/9999 | 401 |
| IDOR_UserEndpoint_OtherUser_NoToken_Returns401 | GET /api/users/9999 | 401 |

---

## JWT Tampering Tests (9 tests)

Verify the API rejects malformed, tampered, or malicious JWTs.

| Test | Attack Vector | Expected |
|------|--------------|----------|
| JWT_NoneAlgorithm_Returns401 | alg:none header attack | 401 |
| JWT_ExpiredToken_Returns401 | exp:1 (past) | 401 |
| JWT_WrongSignature_Returns401 | Valid structure, bad signature | 401 |
| JWT_EmptyToken_Returns401 | Empty Bearer value | 401 |
| JWT_MalformedToken_OnePart_Returns401 | Single segment token | 401 |
| JWT_MalformedToken_TwoParts_Returns401 | Two segment token | 401 |
| JWT_NullBearerValue_Returns401 | "Bearer " with no value | 401 |
| JWT_SqlInjectionInToken_Returns401 | SQL injection as token | 401 |
| JWT_TamperedPayload_EscalateRole_Returns401 | Payload role=SuperAdmin, bad sig | 401 |

---

## Auth Bypass Tests (15 tests)

Verify all protected endpoints reject unauthenticated requests.

| Test | Endpoint | Expected |
|------|----------|----------|
| AuthBypass_NoToken_AdminDashboard_Returns401 | GET /api/AdminDashboard | 401 |
| AuthBypass_NoToken_UsersList_Returns401 | GET /api/users | 401 |
| AuthBypass_NoToken_SystemSettings_Returns401 | GET /api/SystemSetting | 401 |
| AuthBypass_NoToken_Notifications_Returns401 | GET /api/Notification/logs | 401/403/404 |
| AuthBypass_NoToken_Orders_Returns401 | GET /api/orders | 401 |
| AuthBypass_NoToken_RMS_Returns401 | GET /api/rms/my-limits | 401 |
| AuthBypass_NoToken_Commission_Returns401 | GET /api/commission/rates | 401 |
| AuthBypass_NoToken_MarketData_Returns401 | GET /api/MarketData | 401 |
| AuthBypass_NoToken_BrokerageSettings_Returns401 | GET /api/BrokerageSettings/1 | 401 |
| AuthBypass_HttpVerbTampering_PostToGetEndpoint_Returns401or405 | POST /api/orders | 401/405/400 |
| AuthBypass_NoToken_Logout_Returns401 | POST /api/auth/logout | 401 |
| AuthBypass_NoToken_GetMe_Returns401 | GET /api/auth/me | 401 |
| AuthBypass_NoToken_AuditCompliance_Returns401 | GET /api/AuditCompliance/logs | 401 |
| AuthBypass_NoToken_FileImport_Returns401 | POST /api/FileImport/stage | 401 |
| AuthBypass_NoToken_TenantProvisioning_Returns401 | POST /api/TenantProvisioning/provision | 401 |

---

## Test Count Progression

| Day | Passing | Notes |
|-----|---------|-------|
| 45 | 681 | Start of Day 46 |
| 46 | 717 | +36 VAPT security tests |

---

## Next: Day 47 - MFA & Session Management

- MFA mandatory for Admin/SuperAdmin roles
- Single session enforcement (logout previous on new login)
- UserPermissions table (granular permission system)
- Target: 725+ tests
