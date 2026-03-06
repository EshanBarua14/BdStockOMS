# 🏦 BD Stock OMS

### Bangladesh Stock Exchange — Order Management System

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![React](https://img.shields.io/badge/React-18-61DAFB?logo=react)
![SQL Server](https://img.shields.io/badge/SQL%20Server-2022-CC2927?logo=microsoftsqlserver)
![xUnit](https://img.shields.io/badge/Tests-xUnit-green)
![GitHub](https://img.shields.io/badge/GitHub-EshanBarua14-181717?logo=github)

A full-stack, role-based Order Management System for
Dhaka Stock Exchange (DSE) and Chittagong Stock Exchange (CSE)
of Bangladesh — built from scratch as a 30-day learning project.

---

## 🎯 Project Goal

Build a production-grade, secure, real-time Order Management
System with:

- 6 role-based access levels
- Live stock data scraped from DSE + CSE
- Real-time price updates via SignalR
- Full order lifecycle management
- Automated testing at every layer

---

## 👥 Roles

| Role                  | Description                     |
| --------------------- | ------------------------------- |
| 🏢 Brokerage House    | Firm owner — highest authority  |
| 🛡️ Admin              | Daily operations manager        |
| 👁️ Shadow Admin (CCD) | Central Compliance Department   |
| 🔧 IT Support         | System health + user management |
| 📈 Trader             | Executes orders for investors   |
| 💰 Investor           | Places own buy/sell orders      |

---

## 🛠️ Tech Stack

**Backend**

- ASP.NET Core 8 Web API
- Entity Framework Core 8
- SQL Server 2022
- JWT Authentication
- SignalR (real-time)
- HtmlAgilityPack (DSE/CSE scraping)

**Frontend**

- React 18 + Vite
- Tailwind CSS
- Axios
- React Router v6

**Testing**

- xUnit + Moq (backend unit tests)
- Vitest + React Testing Library (frontend)
- Cypress (frontend E2E)
- Playwright (full-stack E2E)

---

## 📅 30-Day Build Log

| Day           | What Was Built                                               | Tests     |
| ------------- | ------------------------------------------------------------ | --------- |
| ✅ Day 1      | Project setup, folder structure, Health endpoint, Swagger UI | 3 passing |
| ⏳ Day 2      | Models + Repository Pattern                                  | -         |
| ⏳ Day 3      | EF Core + Migrations + Seed Data                             | -         |
| ⏳ Day 4      | Auth — Register + Login + JWT                                | -         |
| ⏳ Day 5      | Role-based Authorization                                     | -         |
| ⏳ Day 6      | User Management APIs                                         | -         |
| ⏳ Day 7      | Stocks + Orders APIs                                         | -         |
| ⏳ Day 8      | Postman Collection                                           | -         |
| ⏳ Day 9      | DSE/CSE Scraper Service                                      | -         |
| ⏳ Day 10     | React Setup                                                  | -         |
| ⏳ Days 11-30 | Coming soon...                                               | -         |

---

## 🚀 Getting Started

### Prerequisites

- .NET 8 SDK
- SQL Server 2022
- Node.js v20+
- Git

### Run Backend

```bash
cd BdStockOMS.API
dotnet restore
dotnet run
```

API runs at: `http://localhost:5289`
Swagger UI: `http://localhost:5289`

### Run Tests

```bash
dotnet test
```

---

## 📊 Day 1 — What Was Built

### Health Endpoint

```
GET /api/health
```

Returns server status and live BD market hours info.

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

### Folder Structure

```
BdStockOMS/
├── BdStockOMS.API/
│   ├── Controllers/
│   ├── Models/
│   ├── DTOs/
│   ├── Services/
│   ├── Repositories/
│   │   └── Interfaces/
│   ├── Data/
│   ├── Middleware/
│   ├── Hubs/
│   ├── BackgroundServices/
│   ├── Validators/
│   ├── Mappings/
│   ├── Program.cs
│   └── appsettings.json
└── BdStockOMS.Tests/
    ├── Unit/
    ├── Integration/
    └── TestHelpers/
```

### Tests Passing

```
✅ GetHealth_Returns200OkResponse
✅ GetHealth_ResponseHasCorrectStatus
✅ GetHealth_MarketStatusIsOpenOrClosed

Total: 3 | Passed: 3 | Failed: 0
```

---

## 🧠 What I Learned — Day 1

- `Program.cs` in .NET 8 — single config file
- Swagger UI — auto-generated API docs
- `ControllerBase` vs `Controller`
- `TimeZoneInfo` for Bangladesh timezone
- xUnit — `[Fact]`, Arrange, Act, Assert
- Why `dynamic` fails across assemblies
- Git branch per day workflow
- GitHub token authentication (PAT via GCM)

---

## 👤 Author

**Eshan Barua**
GitHub: [@EshanBarua14](https://github.com/EshanBarua14)

---

_Built with ❤️ in Bangladesh 🇧🇩_
