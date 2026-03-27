# Day 50 — Premium UI/UX Redesign Phase 1-2

**Branch:** `day-50-trading-dashboard`
**Date:** 2026-03-13

## What was done

### Phase 1: Foundation
- Added BD Stock OMS logo (`src/assets/images/logo.png`)
- Created design tokens CSS (`src/styles/oms-tokens.css`) with 20+ CSS variables
- Upgraded themeStore to 14 exclusive themes (6 Dark, 6 Special, 2 Light)
- All components now read `--t-*` CSS variables — themes actually change the UI
- Light themes (Arctic, Sand) fully functional
- Theme persistence via Zustand + localStorage (`bd_oms_theme_v5`)
- Buy/sell color customization with 5 presets + custom color picker

### Phase 2: Layout Components
- **Sidebar:** Logo image, glass styling, RBAC nav, neon accent active states, all CSS vars
- **Topbar:** Market Phase (Pre-Opening/Opening/Continuous/Closing/Post-Closing/Closed per BSEC), DSE indexes (DSEX, DS30, DSES) + CSE indexes (CASPI, CSE30), SignalR connection indicator (LIVE/SYNC/OFF), BDT clock, global search (Ctrl+K)
- **DashboardLayout:** Reads CSS vars, no hardcoded colors
- **ThemeMenu:** 4-tab premium drawer (Themes/Accent/Layout/Colors), preview + Apply/Cancel flow

### Phase 3: Auth Pages
- **LoginPage:** Logo in card + top bar, animated candlestick canvas, index strip (DSEX/DS30/DSES/CSE30 with change amounts), LinkedIn credit link
- **SignUpPage:** Investor-only 2-step wizard, field validation (email regex, BD phone format), brokerage selector, password strength, pending RBAC approval flow, success screen

## Tests
- **10 test files, 77 tests — all passing**
- Fixed themeStore test (12→14 themes, added new field checks)
- Fixed registry test (WIDGET_REGISTRY_LIST, ticker exception)

## Files Changed
- `src/store/themeStore.ts` — 14 themes, applyTheme sets 20+ CSS vars
- `src/styles/oms-tokens.css` — CSS variable defaults + animations
- `src/components/layout/Sidebar.tsx` — Logo + CSS vars
- `src/components/layout/Topbar.tsx` — Indexes + BD sessions + SignalR
- `src/components/layout/DashboardLayout.tsx` — CSS vars
- `src/components/ui/ThemeMenu.tsx` — 4-tab premium drawer
- `src/pages/LoginPage.tsx` — Logo + CSS vars + index strip
- `src/pages/SignUpPage.tsx` — Investor registration + validation
- `src/assets/images/logo.png` — BD Stock OMS logo

## Build
- `vite build` clean, 0 errors
- Bundle: ~857KB JS (gzip ~253KB)
