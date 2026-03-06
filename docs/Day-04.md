
# Day 4 — JWT Authentication (Register + Login)

**Date:** March 8, 2026
**Branch:** `day-04-auth-jwt`
**Status:** ✅ Complete

---

## 🎯 Goal
Build Register and Login endpoints with JWT token generation
and BCrypt password hashing.

---

## ✅ What Was Built

### Files Created
| File | Purpose |
|------|---------|
| `DTOs/Auth/RegisterBrokerageDto.cs` | Registration input data |
| `DTOs/Auth/LoginDto.cs` | Login input data |
| `DTOs/Auth/AuthResponseDto.cs` | Token + user info response |
| `Services/AuthService.cs` | Register + Login business logic |
| `Controllers/AuthController.cs` | HTTP endpoints |

### Endpoints
| Method | URL | Auth | Description |
|--------|-----|------|-------------|
| POST | `/api/auth/register-brokerage` | Public | Register new brokerage + owner |
| POST | `/api/auth/login` | Public | Login, get JWT token |

### Packages Installed
| Package | Purpose |
|---------|---------|
| BCrypt.Net-Next 4.0.3 | Password hashing |
| Microsoft.AspNetCore.Authentication.JwtBearer 8.0.0 | JWT validation |
| System.IdentityModel.Tokens.Jwt 7.5.0 | JWT generation |

---

## 🧪 Tests Written

| Test | Status |
|------|--------|
| `RegisterBrokerage_CreatesUserAndBrokerage` | ✅ Passed |
| `RegisterBrokerage_DuplicateEmail_ThrowsException` | ✅ Passed |
| `Login_ValidCredentials_ReturnsToken` | ✅ Passed |
| `Login_WrongPassword_ThrowsException` | ✅ Passed |
| `Login_NonExistentEmail_ThrowsException` | ✅ Passed |

**Cumulative Total: 19 passing, 0 failing**

---

## 🧠 What I Learned

| Topic | New? |
|-------|------|
| JWT — what it is, 3 parts, how it works | ✅ New |
| Claims — data inside JWT token | ✅ New |
| BCrypt password hashing — one-way, salted | ✅ New |
| DTOs — separate input/output from models | ✅ New |
| IAuthService interface + DI registration | ✅ New |
| AddScoped vs AddSingleton vs AddTransient | ✅ New |
| UseAuthentication + UseAuthorization order | ✅ New |
| Swagger Authorize button setup | ✅ New |

---

## 🔗 Commits
| Message |
|---------|
| Day 4: JWT auth + register + login + 19 passing tests |

---

## ⏭️ Tomorrow — Day 5
Role-based Authorization — protect endpoints by role.
EOF