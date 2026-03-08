# Day 27 — Frontend Auth Pages

## Branch
`day-27-frontend-auth-pages`

## Summary
Completed the frontend authentication flow with role-based redirects,
change password page, and profile page connected to the real backend API.

## Files Created/Updated
### Types
- `src/types/index.ts` — Updated to match real AuthResponseDto fields
  (userId, fullName, email, role, brokerageHouseId, brokerageHouseName, expiresAt)

### Context
- `src/context/AuthContext.tsx` — Updated login to map real response,
  added getRoleRedirect() helper, logout calls /api/auth/logout to blacklist token

### Pages
- `src/pages/LoginPage.tsx` — Improved UI, role-based redirect after login,
  handles validation error arrays from backend
- `src/pages/DashboardPage.tsx` — Uses real user fields, links to profile/change-password
- `src/pages/ChangePasswordPage.tsx` — Calls /api/password/change, auto-logout after success
- `src/pages/ProfilePage.tsx` — Fetches /api/auth/me, shows full profile details

### App
- `src/App.tsx` — Added routes: /change-password, /profile,
  /admin/dashboard, /trader/dashboard, /ccd/dashboard, /it/dashboard

## Role-Based Redirects After Login
| Role | Redirect |
|------|----------|
| SuperAdmin, Admin | /admin/dashboard |
| Trader | /trader/dashboard |
| CCD | /ccd/dashboard |
| ITSupport | /it/dashboard |
| Investor | /dashboard |

## Build
✓ Passing — 98 modules transformed

## How To Run
```bash
# Terminal 1 — Backend
cd BdStockOMS.API && dotnet run

# Terminal 2 — Frontend
cd BdStockOMS.Client && npm run dev
```
Open http://localhost:5173
