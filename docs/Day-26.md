# Day 26 — Frontend Setup (React + Vite + Tailwind)

## Branch
`day-26-frontend-setup`

## Summary
Set up the React frontend project with TypeScript, Tailwind CSS, Axios,
and React Router. Built the login page and dashboard placeholder with
JWT auth stored in memory (no localStorage).

## Tech Stack
- React 18 + TypeScript
- Vite 7 (build tool)
- Tailwind CSS v4 (via @tailwindcss/vite)
- Axios (HTTP client)
- React Router v6 (navigation)

## Project Location
`BdStockOMS.Client/` inside the solution root

## Files Created
### Configuration
- `vite.config.ts` — Vite config with Tailwind plugin + API proxy to :5000
- `src/index.css` — Tailwind import

### Types
- `src/types/index.ts` — User, LoginResponse, AuthContextType, ApiError

### API Layer
- `src/api/axios.ts` — Axios instance with JWT interceptor + 401 handler

### Auth
- `src/context/AuthContext.tsx` — In-memory auth state, login/logout functions

### Pages
- `src/pages/LoginPage.tsx` — Login form connecting to /api/auth/login
- `src/pages/DashboardPage.tsx` — Welcome dashboard with user info cards

### Components
- `src/components/ProtectedRoute.tsx` — Role-based route guard

### App
- `src/App.tsx` — Router setup with protected routes
- `src/main.tsx` — React entry point

## Key Design Decisions
- JWT token stored in window.__authToken (memory only — no localStorage)
- Axios proxy forwards /api to http://localhost:5000
- ProtectedRoute supports role-based access control
- All type imports use `import type` for TypeScript strict mode

## How To Run
```bash
cd BdStockOMS.Client
npm run dev
```
Then open http://localhost:5173
