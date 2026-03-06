# Day 2 — Models + Repository Pattern

**Date:** March 7, 2026
**Branch:** `day-02-models-repository`
**Status:** ✅ Complete

---

## 🎯 Goal

Design all 7 C# models with proper EF Core relationships
and create Repository Pattern interfaces.

---

## ✅ What Was Built

### Models Created

| File                       | Purpose                             |
| -------------------------- | ----------------------------------- |
| `Models/Role.cs`           | 6 system roles                      |
| `Models/BrokerageHouse.cs` | Registered brokerage firms          |
| `Models/User.cs`           | All 6 role users in one table       |
| `Models/Stock.cs`          | DSE + CSE listed companies          |
| `Models/Order.cs`          | Buy/sell orders with full lifecycle |
| `Models/AuditLog.cs`       | CCD compliance audit trail          |
| `Models/SystemLog.cs`      | IT Support system logs              |

### Repository Interfaces Created

| File                                          | Purpose                                 |
| --------------------------------------------- | --------------------------------------- |
| `Repositories/Interfaces/IRepository.cs`      | Generic base — Get, Add, Update, Delete |
| `Repositories/Interfaces/IUserRepository.cs`  | User-specific queries                   |
| `Repositories/Interfaces/IStockRepository.cs` | Stock-specific queries                  |
| `Repositories/Interfaces/IOrderRepository.cs` | Order-specific queries                  |

### Enums Created

```csharp
// Order type
enum OrderType { Buy, Sell }

// Order lifecycle
enum OrderStatus
{
    Pending, Approved, Rejected,
    Executed, Completed, Cancelled
}

// System log severity
enum LogLevel { Info, Warning, Error, Critical }
```

---

## 🧪 Tests Written

| Test                                    | Status    |
| --------------------------------------- | --------- |
| `Order_DefaultStatus_IsPending`         | ✅ Passed |
| `User_DefaultIsActive_IsTrue`           | ✅ Passed |
| `User_DefaultIsLocked_IsFalse`          | ✅ Passed |
| `Stock_DefaultIsActive_IsTrue`          | ✅ Passed |
| `BrokerageHouse_DefaultIsActive_IsTrue` | ✅ Passed |
| `OrderType_HasBuyAndSell`               | ✅ Passed |
| `OrderStatus_HasAllSixStatuses`         | ✅ Passed |

**Total: 10 passing, 0 failing**

---

## 🧠 What I Learned

| Topic                               | New?   |
| ----------------------------------- | ------ |
| Repository Pattern — analogy + why  | ✅ New |
| Generic interface `IRepository<T>`  | ✅ New |
| EF Core navigation properties       | ✅ New |
| Foreign keys in C# (`[ForeignKey]`) | ✅ New |
| `decimal` vs `double` for money     | ✅ New |
| `DateTime?` nullable timestamps     | ✅ New |
| C# enums for OrderStatus, OrderType | ✅ New |
| `null!` vs `= string.Empty`         | ✅ New |

---

## 🔗 Commits

| Hash   | Message                                                          |
| ------ | ---------------------------------------------------------------- |
| latest | Day 2: All 7 models + 4 repository interfaces + 10 passing tests |

---

## ⏭️ Tomorrow — Day 3

EF Core DbContext + Migrations + Full Seed Data.
