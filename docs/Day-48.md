# Day 48 - Swagger, FluentValidation & Backend Hardening

**Branch:** `day-48-swagger-validation`
**Tests:** 746 (start) -> 776 (end) | +30 tests
**All tests:** 776 passing, 0 failures

---

## Summary

| # | Goal | Status |
|---|------|--------|
| 1 | FluentValidation installed (API + Tests) | Done |
| 2 | Validators: Login, RegisterBrokerage, PlaceOrder, CancelOrder | Done |
| 3 | Global Exception Middleware (structured JSON errors) | Done |
| 4 | Swagger XML docs enabled in csproj | Done |
| 5 | Swagger JWT Bearer security definition | Done |
| 6 | 30 new validator + middleware tests | Done |
| 7 | 776 tests passing, 0 failures | Done |

---

## Backend Analysis (Pre-Day 48)

| Check | Result |
|-------|--------|
| All services registered in Program.cs | Pass - 45 services registered |
| All controllers have [ApiController] | Pass |
| Controllers missing [Authorize] | Only HealthController (correct) |
| FluentValidation | Added Day 48 |
| Global exception handler | Added Day 48 |
| ProducesResponseType annotations | Pending (Day 49) |

---

## FluentValidation

**File:** `BdStockOMS.API/Validators/DtoValidators.cs`

### LoginDtoValidator
- Email: required, valid format, max 100 chars
- Password: required, min 6, max 100 chars

### RegisterBrokerageDtoValidator
- FirmName: required, max 100
- LicenseNumber: required, alphanumeric+dash only, max 50
- FirmEmail: required, valid format
- FirmPhone: digits/+/-/spaces only (optional)
- FullName: letters and spaces only, max 100
- Email: required, valid format
- Password: min 8, uppercase, lowercase, digit, special char required

### PlaceOrderDtoValidator
- StockId: > 0
- Quantity: 1 to 1,000,000
- LimitPrice: required and > 0 for Limit orders, must be null for Market orders
- InvestorId: > 0 when provided

### CancelOrderDtoValidator
- Reason: required, min 5, max 500 chars

---

## Global Exception Middleware

**File:** `BdStockOMS.API/Middleware/GlobalExceptionMiddleware.cs`

Catches all unhandled exceptions and returns structured JSON:

    {
      "success": false,
      "errorCode": "SERVER_ERROR",
      "message": "An unexpected error occurred.",
      "traceId": "..."
    }

| Exception Type | HTTP Status | Error Code |
|----------------|-------------|------------|
| UnauthorizedAccessException | 401 | UNAUTHORIZED |
| KeyNotFoundException | 404 | NOT_FOUND |
| ArgumentException | 400 | INVALID_INPUT |
| InvalidOperationException | 400 | INVALID_OPERATION |
| Any other | 500 | SERVER_ERROR |

Registered in Program.cs before UseAuthentication().

---

## Swagger

- XML documentation generation enabled in BdStockOMS.API.csproj
- CS1591 warnings suppressed (missing XML comments)
- JWT Bearer security definition added
- Swagger UI title: "BD Stock OMS API v1"

---

## New Tests (+30)

### LoginDtoValidatorTests (6 tests)
Valid dto, empty email, invalid email format, empty password, short password, too long email

### RegisterBrokerageDtoValidatorTests (9 tests)
Valid dto, empty firm name, invalid license chars, invalid firm email, weak passwords (3 cases), full name with numbers, empty admin email

### PlaceOrderDtoValidatorTests (9 tests)
Valid limit/market orders, zero stock id, zero quantity, over max quantity, limit order no price, limit order zero price, market order with price, invalid investor id

### CancelOrderDtoValidatorTests (4 tests)
Valid reason, empty reason, too short, too long

### GlobalExceptionMiddlewareTests (2 tests)
Instantiation, session policy timeouts

---

## Test Count Progression

| Day | Passing | Notes |
|-----|---------|-------|
| 47 | 746 | Start of Day 48 |
| 48 | 776 | +30 validation + middleware tests |

---

## Next: Day 49 - React Frontend

- Vite + React 18 + TypeScript + Tailwind scaffold
- Login page with JWT auth
- Dashboard layout
- API client (axios + interceptors)
- Target: 790+ tests
