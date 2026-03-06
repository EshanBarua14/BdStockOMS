# Day 1 — Project Setup + First API Endpoint

**Date:** March 7, 2026
**Branch:** `day-01-project-setup`
**Status:** ✅ Complete

---

## 🎯 Goal

Create the ASP.NET Core 8 Web API project from scratch,
set up folder structure, build first real endpoint,
write first unit tests, push to GitHub.

---

## ✅ What Was Built

### Health Endpoint

```
GET /api/health
```

Returns live server status + Bangladesh market hours.

**Response:**

```json
{
  "status": "BD Stock OMS is running",
  "bdTime": "2026-03-07 00:43:23",
  "marketStatus": "CLOSED",
  "tradingDays": "Sunday - Thursday",
  "tradingHours": "10:00 AM - 2:30 PM BST",
  "exchanges": ["DSE", "CSE"],
  "apiVersion": "1.0.0"
}
```

### Folder Structure Created

```
BdStockOMS.API/
├── Controllers/
├── Models/
├── DTOs/
├── Services/
├── Repositories/
│   └── Interfaces/
├── Data/
├── Middleware/
├── Hubs/
├── BackgroundServices/
├── Validators/
└── Mappings/

BdStockOMS.Tests/
├── Unit/
├── Integration/
└── TestHelpers/
```

---

## 🧪 Tests Written

| Test                                   | Status    |
| -------------------------------------- | --------- |
| `GetHealth_Returns200OkResponse`       | ✅ Passed |
| `GetHealth_ResponseHasCorrectStatus`   | ✅ Passed |
| `GetHealth_MarketStatusIsOpenOrClosed` | ✅ Passed |

**Total: 3 passing, 0 failing**

---

## 🧠 What I Learned

| Topic                                       | New?   |
| ------------------------------------------- | ------ |
| `Program.cs` in .NET 8 — single config file | ✅ New |
| Swagger UI — auto API docs page             | ✅ New |
| `ControllerBase` vs `Controller`            | ✅ New |
| `TimeZoneInfo` — Bangladesh timezone        | ✅ New |
| xUnit `[Fact]`, Arrange-Act-Assert          | ✅ New |
| Why `dynamic` fails across assemblies       | ✅ New |
| Git branch per day workflow                 | ✅ New |
| GitHub token auth (PAT via GCM)             | ✅ New |

---

## 🔗 Commits

| Hash      | Message                                                       |
| --------- | ------------------------------------------------------------- |
| `30ffbfa` | Day 1: Project setup + Health endpoint + 3 passing unit tests |
| `47208ae` | Day 1: Add README with project docs and build log             |

---

## ⏭️ Tomorrow — Day 2

Design all 7 models + Repository Pattern interfaces.
