# Day 3 — EF Core + DbContext + Migrations + Seed Data

**Date:** March 7, 2026
**Branch:** `day-03-database-seed`
**Status:** ✅ Complete

---

## 🎯 Goal

Connect ASP.NET Core to SQL Server using Entity Framework Core.
Create all database tables automatically from C# models.
Seed the database with roles and stocks.

---

## ✅ What Was Built

### Files Created

| File                   | Purpose                                    |
| ---------------------- | ------------------------------------------ |
| `Data/AppDbContext.cs` | EF Core brain — represents entire database |
| `Data/Migrations/`     | Auto-generated SQL migration files         |
| `appsettings.json`     | Connection string + JWT config added       |

### Database Created

| Table           | Records Seeded                                              |
| --------------- | ----------------------------------------------------------- |
| Roles           | 6 (BrokerageHouse, Admin, CCD, ITSupport, Trader, Investor) |
| BrokerageHouses | 0 (created via API on Day 4)                                |
| Users           | 0 (created via API on Day 4)                                |
| Stocks          | 15 (10 DSE + 5 CSE)                                         |
| Orders          | 0 (created via API on Day 7)                                |
| AuditLogs       | 0                                                           |
| SystemLogs      | 0                                                           |

### Packages Installed

| Package                                 | Version | Purpose                |
| --------------------------------------- | ------- | ---------------------- |
| Microsoft.EntityFrameworkCore           | 8.0.0   | EF Core ORM            |
| Microsoft.EntityFrameworkCore.SqlServer | 8.0.0   | SQL Server provider    |
| Microsoft.EntityFrameworkCore.Design    | 8.0.0   | Migration tools        |
| Microsoft.EntityFrameworkCore.InMemory  | 8.0.0   | In-memory DB for tests |

---

## 🧪 Tests Written

| Test                                       | Status    |
| ------------------------------------------ | --------- |
| `CanAddAndRetrieveRole`                    | ✅ Passed |
| `CanAddAndRetrieveStock`                   | ✅ Passed |
| `CanAddMultipleStocks`                     | ✅ Passed |
| `Order_DefaultStatus_IsPending_InDatabase` | ✅ Passed |

**Cumulative Total: 14 passing, 0 failing**

---

## 🧠 What I Learned

| Topic                                         | New?        |
| --------------------------------------------- | ----------- |
| DbContext — represents DB in C#               | ✅ New      |
| Fluent API — configure relationships in code  | ✅ New      |
| Migrations — EF generates SQL from C# models  | ✅ New      |
| `dotnet ef migrations add` command            | ✅ New      |
| `dotnet ef database update` command           | ✅ New      |
| In-memory database for unit tests             | ✅ New      |
| `HasPrecision(18,4)` for decimal money values | ✅ New      |
| `OnDelete(DeleteBehavior.Restrict)`           | ✅ New      |
| Connection string in appsettings.json         | Quick touch |

---

## 🔗 Commits

| Message                                                         |
| --------------------------------------------------------------- |
| Day 3: AppDbContext + migrations + seed data + 14 passing tests |

---

## ⏭️ Tomorrow — Day 4

JWT Authentication — Register + Login endpoints.
