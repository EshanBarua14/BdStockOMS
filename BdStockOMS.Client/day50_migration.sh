#!/usr/bin/env bash
# =============================================================================
# BD Stock OMS — Day 50 Migration Script
# Migrates: AuthContext  →  Zustand authStore
# Adds:     New design system, Logo, SignUpPage, ThemePanel v2
# Design & Developed by Eshan Barua
#
# RUN FROM:  /e/Projects/BdStockOMS/BdStockOMS.Client
# COMMAND:   bash day50_migration.sh
# =============================================================================

set -e
RED='\033[0;31m'; GREEN='\033[0;32m'; YELLOW='\033[1;33m'; CYAN='\033[0;36m'; BOLD='\033[1m'; NC='\033[0m'

ok()   { echo -e "  ${GREEN}✓${NC} $1"; }
err()  { echo -e "  ${RED}✗ $1${NC}"; }
step() { echo -e "\n${CYAN}${BOLD}━━━ STEP $1 — $2 ━━━${NC}"; }
warn() { echo -e "  ${YELLOW}⚠  $1${NC}"; }
info() { echo -e "  ${CYAN}ℹ  $1${NC}"; }

# =============================================================================
# PRE-FLIGHT
# =============================================================================
step 0 "Pre-flight checks"

if [ ! -f "package.json" ]; then
  err "Not in BdStockOMS.Client root — package.json not found"
  echo "    cd /e/Projects/BdStockOMS/BdStockOMS.Client"
  exit 1
fi
ok "In correct directory: $(pwd)"

BRANCH=$(git branch --show-current 2>/dev/null)
ok "Current branch: $BRANCH"

if [ "$BRANCH" = "day-50-ui-overhaul" ]; then
  warn "Already on day-50-ui-overhaul — continuing"
else
  git checkout -b day-50-ui-overhaul
  ok "Created and switched to branch: day-50-ui-overhaul"
fi


# =============================================================================
# STEP 1 — Create all required directories
# =============================================================================
step 1 "Create directory structure"

mkdir -p src/components/ui
mkdir -p src/components/layout
mkdir -p src/components/auth
mkdir -p src/components/widgets
mkdir -p src/store
mkdir -p src/styles
mkdir -p src/hooks
mkdir -p src/api
mkdir -p src/types
mkdir -p src/pages
mkdir -p src/test
mkdir -p public

ok "All directories created"


# =============================================================================
# STEP 2 — Backup old AuthContext files (don't delete yet)
# =============================================================================
step 2 "Backup old AuthContext architecture"

mkdir -p .archive_day48

[ -f src/context/AuthContext.tsx ]          && cp src/context/AuthContext.tsx .archive_day48/ && ok "Backed up AuthContext.tsx"
[ -f src/components/ProtectedRoute.tsx ]    && cp src/components/ProtectedRoute.tsx .archive_day48/ && ok "Backed up old ProtectedRoute.tsx"
[ -f src/api/axios.ts ]                     && cp src/api/axios.ts .archive_day48/ && ok "Backed up axios.ts"
[ -f src/pages/TradingDashboard.tsx ]       && cp src/pages/TradingDashboard.tsx .archive_day48/ && ok "Backed up TradingDashboard.tsx"
[ -f src/pages/ChangePasswordPage.tsx ]     && cp src/pages/ChangePasswordPage.tsx .archive_day48/ && ok "Backed up ChangePasswordPage.tsx"
[ -f src/pages/ProfilePage.tsx ]            && cp src/pages/ProfilePage.tsx .archive_day48/ && ok "Backed up ProfilePage.tsx"
[ -f src/components/trading/OrderHistory.tsx ]  && cp src/components/trading/OrderHistory.tsx .archive_day48/ && ok "Backed up OrderHistory.tsx"
[ -f src/components/trading/PortfolioPanel.tsx ] && cp src/components/trading/PortfolioPanel.tsx .archive_day48/ && ok "Backed up PortfolioPanel.tsx"

ok "All old files backed up to .archive_day48/"


# =============================================================================
# STEP 3 — index.html — new fonts + anti-FOUC
# =============================================================================
step 3 "index.html — Outfit + Space Grotesk + Space Mono fonts"

cat > index.html << 'EOF'
<!doctype html>
<html lang="en" data-theme="obsidian" data-accent="azure" data-density="comfortable">
  <head>
    <meta charset="UTF-8" />
    <link rel="icon" type="image/svg+xml" href="/favicon.svg" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <meta name="description" content="BD Stock OMS — International-grade Order Management System for Bangladesh Securities Exchange" />
    <title>BD Stock OMS</title>
    <link rel="preconnect" href="https://fonts.googleapis.com" />
    <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin />
    <link href="https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700;800;900&family=Space+Grotesk:wght@400;500;600;700&family=Space+Mono:wght@400;700&display=swap" rel="stylesheet" />
    <style>html { background: #05070F; }</style>
  </head>
  <body>
    <div id="root"></div>
    <script type="module" src="/src/main.tsx"></script>
  </body>
</html>
EOF
ok "index.html"


# =============================================================================
# STEP 4 — tailwind.config.js
# =============================================================================
step 4 "tailwind.config.js — register new font families"

cat > tailwind.config.js << 'EOF'
/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: ['class', '[data-theme="obsidian"]'],
  theme: {
    extend: {
      fontFamily: {
        ui:      ['"Outfit"',        'system-ui', 'sans-serif'],
        display: ['"Space Grotesk"', 'system-ui', 'sans-serif'],
        mono:    ['"Space Mono"',    'monospace'],
      },
    },
  },
  plugins: [],
}
EOF
ok "tailwind.config.js"


# =============================================================================
# STEP 5 — public/favicon.svg — hexagon trend-line logo
# =============================================================================
step 5 "public/favicon.svg — crystalline hexagon logo"

cat > public/favicon.svg << 'EOF'
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 48 48">
  <defs>
    <linearGradient id="g1" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="#3B82F6"/>
      <stop offset="100%" stop-color="#06B6D4"/>
    </linearGradient>
    <linearGradient id="g2" x1="0%" y1="100%" x2="100%" y2="0%">
      <stop offset="0%" stop-color="#3B82F6" stop-opacity="0.4"/>
      <stop offset="100%" stop-color="#06B6D4" stop-opacity="0.8"/>
    </linearGradient>
  </defs>
  <polygon points="24,3 42,13 42,35 24,45 6,35 6,13" fill="url(#g1)" opacity="0.15"/>
  <polygon points="24,3 42,13 42,35 24,45 6,35 6,13" fill="none" stroke="url(#g1)" stroke-width="1.5"/>
  <polygon points="24,9 37,16 37,32 24,39 11,32 11,16" fill="none" stroke="url(#g2)" stroke-width="0.8" opacity="0.6"/>
  <polyline points="14,31 20,24 26,27 34,17" fill="none" stroke="url(#g1)" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/>
  <circle cx="14" cy="31" r="2" fill="#3B82F6"/>
  <circle cx="20" cy="24" r="1.5" fill="#3B82F6" opacity="0.7"/>
  <circle cx="26" cy="27" r="1.5" fill="#3B82F6" opacity="0.7"/>
  <circle cx="34" cy="17" r="2.5" fill="#06B6D4"/>
  <polyline points="31,14 34,17 37,14" fill="none" stroke="#06B6D4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
EOF
ok "public/favicon.svg"


# =============================================================================
# STEP 6 — src/styles/themes.css — Design System v2
# =============================================================================
step 6 "src/styles/themes.css — 5 themes × 6 accents × 3 densities"

cat > src/styles/themes.css << 'EOF'
/* BD STOCK OMS — DESIGN SYSTEM v2.0
   "Precision Instrument" — International-grade OMS
   Design & Developed by Eshan Barua */

@import url('https://fonts.googleapis.com/css2?family=Outfit:wght@300;400;500;600;700;800;900&family=Space+Grotesk:wght@400;500;600;700&family=Space+Mono:wght@400;700&display=swap');

:root {
  --font-ui:      'Outfit', system-ui, sans-serif;
  --font-display: 'Space Grotesk', system-ui, sans-serif;
  --font-mono:    'Space Mono', 'Courier New', monospace;
  --ease-out-expo: cubic-bezier(0.16, 1, 0.3, 1);
  --ease-in-expo:  cubic-bezier(0.7, 0, 0.84, 0);
  --ease-spring:   cubic-bezier(0.34, 1.56, 0.64, 1);
  --ease-smooth:   cubic-bezier(0.4, 0, 0.2, 1);
  --dur-instant:80ms; --dur-fast:150ms; --dur-base:250ms; --dur-slow:400ms; --dur-xslow:600ms;
  --r-xs:2px; --r-sm:6px; --r-md:10px; --r-lg:14px; --r-xl:20px; --r-2xl:28px; --r-full:9999px;
  --z-dropdown:50; --z-sticky:60; --z-overlay:100; --z-modal:200; --z-toast:1000;
  --sp-1:4px; --sp-2:8px; --sp-3:12px; --sp-4:16px; --sp-5:20px; --sp-6:24px; --sp-8:32px;
  --bull-strong:#00E5A0; --bull-base:#10B981; --bull-muted:rgba(16,185,129,.12); --bull-border:rgba(16,185,129,.25); --bull-glow:rgba(0,229,160,.20);
  --bear-strong:#FF5C5C; --bear-base:#EF4444; --bear-muted:rgba(239,68,68,.12); --bear-border:rgba(239,68,68,.25); --bear-glow:rgba(255,92,92,.20);
  --warn-base:#F59E0B; --warn-muted:rgba(245,158,11,.12); --warn-border:rgba(245,158,11,.25);
  --info-base:#38BDF8; --info-muted:rgba(56,189,248,.12); --info-border:rgba(56,189,248,.25);
  --neutral-base:#64748B; --neutral-muted:rgba(100,116,139,.12);
}

[data-theme="obsidian"] {
  --bg-base:#05070F; --bg-surface:#080C18; --bg-elevated:#0D1220; --bg-overlay:#111828; --bg-hover:#161E30; --bg-active:#1A2438;
  --border-subtle:rgba(255,255,255,.045); --border-default:rgba(255,255,255,.085); --border-strong:rgba(255,255,255,.15); --border-focus:var(--accent-500);
  --text-primary:#EEF2FF; --text-secondary:#7B88A0; --text-tertiary:#475569; --text-disabled:#2A3448; --text-inverse:#05070F;
  --glass-bg:rgba(8,12,24,.72); --glass-border:rgba(255,255,255,.07);
  --shadow-sm:0 1px 3px rgba(0,0,0,.6); --shadow-md:0 4px 16px rgba(0,0,0,.5),0 1px 4px rgba(0,0,0,.6);
  --shadow-lg:0 8px 40px rgba(0,0,0,.6),0 2px 10px rgba(0,0,0,.5); --shadow-xl:0 24px 72px rgba(0,0,0,.7),0 6px 20px rgba(0,0,0,.6);
  --shadow-glow:0 0 40px var(--accent-glow);
}
[data-theme="midnight"] {
  --bg-base:#000000; --bg-surface:#030303; --bg-elevated:#080808; --bg-overlay:#0C0C0C; --bg-hover:#111111; --bg-active:#161616;
  --border-subtle:rgba(255,255,255,.04); --border-default:rgba(255,255,255,.08); --border-strong:rgba(255,255,255,.14); --border-focus:var(--accent-500);
  --text-primary:#F8F8F8; --text-secondary:#666666; --text-tertiary:#3A3A3A; --text-disabled:#222222; --text-inverse:#000000;
  --glass-bg:rgba(3,3,3,.80); --glass-border:rgba(255,255,255,.06);
  --shadow-sm:0 1px 3px rgba(0,0,0,.8); --shadow-md:0 4px 16px rgba(0,0,0,.7); --shadow-lg:0 8px 40px rgba(0,0,0,.8); --shadow-xl:0 24px 72px rgba(0,0,0,.9);
  --shadow-glow:0 0 40px var(--accent-glow);
}
[data-theme="slate"] {
  --bg-base:#0C0E13; --bg-surface:#131620; --bg-elevated:#181C28; --bg-overlay:#1D2232; --bg-hover:#222840; --bg-active:#272E48;
  --border-subtle:rgba(148,163,184,.06); --border-default:rgba(148,163,184,.11); --border-strong:rgba(148,163,184,.20); --border-focus:var(--accent-500);
  --text-primary:#E2E8F4; --text-secondary:#8492AA; --text-tertiary:#4E5D78; --text-disabled:#2E3850; --text-inverse:#0C0E13;
  --glass-bg:rgba(19,22,32,.75); --glass-border:rgba(148,163,184,.08);
  --shadow-sm:0 1px 3px rgba(0,0,0,.55); --shadow-md:0 4px 16px rgba(0,0,0,.45); --shadow-lg:0 8px 40px rgba(0,0,0,.55); --shadow-xl:0 24px 72px rgba(0,0,0,.65);
  --shadow-glow:0 0 40px var(--accent-glow);
}
[data-theme="aurora"] {
  --bg-base:#020B1A; --bg-surface:#051428; --bg-elevated:#081C36; --bg-overlay:#0C2244; --bg-hover:#102850; --bg-active:#142E5C;
  --border-subtle:rgba(56,189,248,.06); --border-default:rgba(56,189,248,.12); --border-strong:rgba(56,189,248,.22); --border-focus:var(--accent-500);
  --text-primary:#E0F2FE; --text-secondary:#5B8FAE; --text-tertiary:#2E5470; --text-disabled:#163050; --text-inverse:#020B1A;
  --glass-bg:rgba(5,20,40,.78); --glass-border:rgba(56,189,248,.08);
  --shadow-sm:0 1px 3px rgba(0,0,0,.7); --shadow-md:0 4px 16px rgba(0,5,20,.6); --shadow-lg:0 8px 40px rgba(0,5,20,.7); --shadow-xl:0 24px 72px rgba(0,5,20,.8);
  --shadow-glow:0 0 50px var(--accent-glow);
}
[data-theme="arctic"] {
  --bg-base:#F4F6FA; --bg-surface:#FFFFFF; --bg-elevated:#FFFFFF; --bg-overlay:#F8FAFD; --bg-hover:#EEF2F8; --bg-active:#E4EAF4;
  --border-subtle:rgba(0,0,0,.06); --border-default:rgba(0,0,0,.10); --border-strong:rgba(0,0,0,.18); --border-focus:var(--accent-500);
  --text-primary:#0D1117; --text-secondary:#4B5563; --text-tertiary:#9CA3AF; --text-disabled:#D1D5DB; --text-inverse:#FFFFFF;
  --glass-bg:rgba(255,255,255,.85); --glass-border:rgba(0,0,0,.08);
  --shadow-sm:0 1px 3px rgba(0,0,0,.07); --shadow-md:0 4px 16px rgba(0,0,0,.08),0 1px 4px rgba(0,0,0,.05);
  --shadow-lg:0 8px 40px rgba(0,0,0,.10),0 2px 10px rgba(0,0,0,.06); --shadow-xl:0 24px 72px rgba(0,0,0,.12),0 6px 20px rgba(0,0,0,.07);
  --shadow-glow:0 0 30px var(--accent-glow);
  --bull-strong:#059669; --bull-base:#10B981; --bear-strong:#DC2626; --bear-base:#EF4444;
}

[data-accent="azure"]   { --accent-50:#EFF6FF; --accent-100:#DBEAFE; --accent-200:#BFDBFE; --accent-300:#93C5FD; --accent-400:#60A5FA; --accent-500:#3B82F6; --accent-600:#2563EB; --accent-700:#1D4ED8; --accent-800:#1E40AF; --accent-900:#1E3A8A; --accent-glow:rgba(59,130,246,.25); --accent-glow-strong:rgba(59,130,246,.45); }
[data-accent="emerald"] { --accent-50:#ECFDF5; --accent-100:#D1FAE5; --accent-200:#A7F3D0; --accent-300:#6EE7B7; --accent-400:#34D399; --accent-500:#10B981; --accent-600:#059669; --accent-700:#047857; --accent-800:#065F46; --accent-900:#064E3B; --accent-glow:rgba(16,185,129,.22); --accent-glow-strong:rgba(16,185,129,.42); }
[data-accent="amber"]   { --accent-50:#FFFBEB; --accent-100:#FEF3C7; --accent-200:#FDE68A; --accent-300:#FCD34D; --accent-400:#FBBF24; --accent-500:#F59E0B; --accent-600:#D97706; --accent-700:#B45309; --accent-800:#92400E; --accent-900:#78350F; --accent-glow:rgba(245,158,11,.22); --accent-glow-strong:rgba(245,158,11,.42); }
[data-accent="rose"]    { --accent-50:#FFF1F2; --accent-100:#FFE4E6; --accent-200:#FECDD3; --accent-300:#FDA4AF; --accent-400:#FB7185; --accent-500:#F43F5E; --accent-600:#E11D48; --accent-700:#BE123C; --accent-800:#9F1239; --accent-900:#881337; --accent-glow:rgba(244,63,94,.22); --accent-glow-strong:rgba(244,63,94,.42); }
[data-accent="violet"]  { --accent-50:#F5F3FF; --accent-100:#EDE9FE; --accent-200:#DDD6FE; --accent-300:#C4B5FD; --accent-400:#A78BFA; --accent-500:#8B5CF6; --accent-600:#7C3AED; --accent-700:#6D28D9; --accent-800:#5B21B6; --accent-900:#4C1D95; --accent-glow:rgba(139,92,246,.22); --accent-glow-strong:rgba(139,92,246,.42); }
[data-accent="cyan"]    { --accent-50:#ECFEFF; --accent-100:#CFFAFE; --accent-200:#A5F3FC; --accent-300:#67E8F9; --accent-400:#22D3EE; --accent-500:#06B6D4; --accent-600:#0891B2; --accent-700:#0E7490; --accent-800:#155E75; --accent-900:#164E63; --accent-glow:rgba(6,182,212,.22); --accent-glow-strong:rgba(6,182,212,.42); }

[data-density="compact"]     { --spacing-row:5px;  --font-table:11.5px; --row-height:28px; --sidebar-item-py:6px; }
[data-density="comfortable"] { --spacing-row:9px;  --font-table:13px;   --row-height:38px; --sidebar-item-py:8px; }
[data-density="spacious"]    { --spacing-row:14px; --font-table:14px;   --row-height:48px; --sidebar-item-py:11px; }
EOF
ok "src/styles/themes.css"


# =============================================================================
# STEP 7 — src/index.css — global styles
# =============================================================================
step 7 "src/index.css — global base + all component classes + keyframes"

cat > src/index.css << 'EOF'
@tailwind base;
@tailwind components;
@tailwind utilities;

@import './styles/themes.css';

@layer base {
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
  html { font-family:var(--font-ui); -webkit-font-smoothing:antialiased; -moz-osx-font-smoothing:grayscale; font-feature-settings:"cv02","cv03","cv04","cv11","ss01","liga"; text-rendering:optimizeLegibility; font-size:14px; height:100%; }
  body { background-color:var(--bg-base); color:var(--text-primary); line-height:1.5; height:100%; overflow:hidden; transition:background-color var(--dur-slow) var(--ease-out-expo),color var(--dur-slow) var(--ease-out-expo); }
  #root { height:100%; display:flex; flex-direction:column; }
  ::selection { background:var(--accent-glow-strong); color:var(--text-primary); }
  *::-webkit-scrollbar { width:4px; height:4px; }
  *::-webkit-scrollbar-track { background:transparent; }
  *::-webkit-scrollbar-thumb { background:var(--border-strong); border-radius:99px; }
  * { scrollbar-width:thin; scrollbar-color:var(--border-strong) transparent; }
  :focus-visible { outline:2px solid var(--accent-500); outline-offset:2px; border-radius:var(--r-sm); }
  a { color:var(--accent-400); text-decoration:none; }
  a:hover { color:var(--accent-300); }
  input, button, select, textarea { font-family:inherit; }
}

@layer components {
  .surface { background:var(--bg-surface); border:1px solid var(--border-subtle); transition:background var(--dur-slow) var(--ease-out-expo),border-color var(--dur-slow) var(--ease-out-expo); }
  .surface-elevated { background:var(--bg-elevated); border:1px solid var(--border-default); box-shadow:var(--shadow-md); }
  .surface-overlay  { background:var(--bg-overlay); border:1px solid var(--border-strong); box-shadow:var(--shadow-lg); }
  .glass { background:var(--glass-bg); border:1px solid var(--glass-border); backdrop-filter:blur(24px) saturate(200%); -webkit-backdrop-filter:blur(24px) saturate(200%); }
  .btn { display:inline-flex; align-items:center; justify-content:center; gap:6px; padding:0 16px; height:36px; font-family:var(--font-ui); font-size:13px; font-weight:500; letter-spacing:.01em; border-radius:var(--r-md); border:1px solid transparent; cursor:pointer; user-select:none; white-space:nowrap; position:relative; overflow:hidden; text-decoration:none; outline:none; transition:background var(--dur-fast) var(--ease-smooth),border-color var(--dur-fast) var(--ease-smooth),box-shadow var(--dur-fast) var(--ease-smooth),transform var(--dur-fast) var(--ease-smooth),color var(--dur-fast) var(--ease-smooth),opacity var(--dur-fast) var(--ease-smooth); }
  .btn::before { content:''; position:absolute; inset:0; opacity:0; background:rgba(255,255,255,.06); transition:opacity var(--dur-fast); }
  .btn:hover::before { opacity:1; }
  .btn:active { transform:scale(.98); }
  .btn:disabled { opacity:.38; cursor:not-allowed; pointer-events:none; }
  .btn-primary  { background:var(--accent-600); color:#fff; border-color:var(--accent-600); }
  .btn-primary:hover { background:var(--accent-500); border-color:var(--accent-500); box-shadow:0 4px 24px var(--accent-glow),0 0 0 1px var(--accent-500); transform:translateY(-1px); }
  .btn-primary:active { transform:translateY(0) scale(.98); box-shadow:none; }
  .btn-secondary { background:var(--bg-elevated); color:var(--text-primary); border-color:var(--border-default); }
  .btn-secondary:hover { background:var(--bg-hover); border-color:var(--border-strong); }
  .btn-ghost { background:transparent; color:var(--text-secondary); border-color:transparent; }
  .btn-ghost:hover { background:var(--bg-hover); color:var(--text-primary); }
  .btn-danger { background:var(--bear-muted); color:var(--bear-strong); border-color:var(--bear-border); }
  .btn-danger:hover { background:rgba(239,68,68,.22); border-color:var(--bear-base); }
  .btn-success { background:var(--bull-muted); color:var(--bull-strong); border-color:var(--bull-border); }
  .btn-success:hover { background:rgba(16,185,129,.22); border-color:var(--bull-base); }
  .btn-outline { background:transparent; color:var(--accent-400); border-color:color-mix(in srgb,var(--accent-500) 40%,transparent); }
  .btn-outline:hover { background:var(--accent-glow); border-color:var(--accent-500); color:var(--accent-300); }
  .btn-xs  { height:24px; padding:0 8px;  font-size:11px; border-radius:var(--r-sm); }
  .btn-sm  { height:30px; padding:0 12px; font-size:12px; border-radius:var(--r-sm); }
  .btn-lg  { height:44px; padding:0 22px; font-size:14px; border-radius:var(--r-lg); }
  .btn-xl  { height:52px; padding:0 30px; font-size:15px; font-weight:600; border-radius:var(--r-lg); letter-spacing:.02em; }
  .btn-icon { padding:0; aspect-ratio:1; }
  .btn-icon.btn-xs { width:24px; } .btn-icon.btn-sm { width:30px; } .btn-icon { width:36px; } .btn-icon.btn-lg { width:44px; }
  .input { display:block; width:100%; height:38px; padding:0 14px; font-family:var(--font-ui); font-size:13px; color:var(--text-primary); background:var(--bg-elevated); border:1px solid var(--border-default); border-radius:var(--r-md); outline:none; transition:border-color var(--dur-fast),box-shadow var(--dur-fast),background var(--dur-fast); }
  .input::placeholder { color:var(--text-tertiary); }
  .input:hover:not(:disabled) { border-color:var(--border-strong); }
  .input:focus { border-color:var(--accent-500); box-shadow:0 0 0 3px var(--accent-glow); background:var(--bg-overlay); }
  .input:disabled { opacity:.45; cursor:not-allowed; }
  .input-lg { height:46px; padding:0 18px; font-size:14px; border-radius:var(--r-lg); }
  .input-xl { height:54px; padding:0 20px; font-size:15px; border-radius:var(--r-lg); }
  .input-error { border-color:var(--bear-base) !important; box-shadow:0 0 0 3px var(--bear-glow) !important; }
  .input-group { position:relative; display:flex; align-items:center; }
  .input-group .input { padding-left:40px; }
  .input-group-icon { position:absolute; left:13px; z-index:1; color:var(--text-tertiary); pointer-events:none; display:flex; align-items:center; transition:color var(--dur-fast); }
  .input-group:focus-within .input-group-icon { color:var(--accent-400); }
  .badge { display:inline-flex; align-items:center; gap:4px; padding:2px 8px; height:20px; font-size:10.5px; font-weight:600; letter-spacing:.04em; border-radius:var(--r-full); border:1px solid transparent; white-space:nowrap; }
  .badge-bull    { background:var(--bull-muted);    color:var(--bull-strong);  border-color:var(--bull-border); }
  .badge-bear    { background:var(--bear-muted);    color:var(--bear-strong);  border-color:var(--bear-border); }
  .badge-warn    { background:var(--warn-muted);    color:var(--warn-base);    border-color:var(--warn-border); }
  .badge-info    { background:var(--info-muted);    color:var(--info-base);    border-color:var(--info-border); }
  .badge-neutral { background:var(--neutral-muted); color:var(--neutral-base); border-color:rgba(100,116,139,.25); }
  .badge-accent  { background:var(--accent-glow);   color:var(--accent-300);   border-color:color-mix(in srgb,var(--accent-500) 30%,transparent); }
  .data-table { width:100%; border-collapse:collapse; font-size:var(--font-table,13px); }
  .data-table th { padding:8px 14px; text-align:left; font-size:10px; font-weight:700; letter-spacing:.08em; text-transform:uppercase; color:var(--text-tertiary); background:var(--bg-surface); border-bottom:1px solid var(--border-default); position:sticky; top:0; z-index:2; white-space:nowrap; user-select:none; transition:background var(--dur-slow); }
  .data-table th:hover { color:var(--text-secondary); }
  .data-table td { padding:var(--spacing-row,9px) 14px; border-bottom:1px solid var(--border-subtle); color:var(--text-primary); vertical-align:middle; white-space:nowrap; transition:background 80ms; }
  .data-table tr:last-child td { border-bottom:none; }
  .data-table tbody tr:hover td { background:var(--bg-hover); }
  .mono { font-family:var(--font-mono); font-size:.91em; letter-spacing:.01em; }
  .tabular { font-variant-numeric:tabular-nums; }
  .text-display { font-family:var(--font-display); font-weight:700; }
  .gradient-text { background:linear-gradient(135deg,var(--accent-300) 0%,var(--accent-500) 100%); -webkit-background-clip:text; -webkit-text-fill-color:transparent; background-clip:text; }
  .sep   { border:none; border-top:1px solid var(--border-subtle); }
  .sep-v { border:none; border-left:1px solid var(--border-subtle); }
  .flash-bull { animation:flashBull .8s var(--ease-out-expo) both; }
  .flash-bear { animation:flashBear .8s var(--ease-out-expo) both; }
  .no-scrollbar { scrollbar-width:none; }
  .no-scrollbar::-webkit-scrollbar { display:none; }
  .card-hover { transition:border-color var(--dur-fast),box-shadow var(--dur-fast),transform var(--dur-fast); }
  .card-hover:hover { border-color:var(--border-strong) !important; box-shadow:var(--shadow-md); transform:translateY(-1px); }
}

@keyframes fadeIn     { from{opacity:0} to{opacity:1} }
@keyframes slideUp    { from{opacity:0;transform:translateY(16px)} to{opacity:1;transform:translateY(0)} }
@keyframes slideDown  { from{opacity:0;transform:translateY(-12px)} to{opacity:1;transform:translateY(0)} }
@keyframes slideLeft  { from{opacity:0;transform:translateX(16px)} to{opacity:1;transform:translateX(0)} }
@keyframes slideRight { from{opacity:0;transform:translateX(-16px)} to{opacity:1;transform:translateX(0)} }
@keyframes scaleIn    { from{opacity:0;transform:scale(.94)} to{opacity:1;transform:scale(1)} }
@keyframes pulse      { 0%,100%{opacity:1} 50%{opacity:.35} }
@keyframes shimmer    { from{background-position:-200% 0} to{background-position:200% 0} }
@keyframes glowPulse  { 0%,100%{box-shadow:0 0 16px var(--accent-glow)} 50%{box-shadow:0 0 40px var(--accent-glow-strong)} }
@keyframes spinSlow   { from{transform:rotate(0deg)} to{transform:rotate(360deg)} }
@keyframes flashBull  { 0%,100%{background:transparent} 25%{background:rgba(16,185,129,.18)} }
@keyframes flashBear  { 0%,100%{background:transparent} 25%{background:rgba(239,68,68,.16)} }
@keyframes tickerScroll { from{transform:translateX(0)} to{transform:translateX(-50%)} }
@keyframes float      { 0%,100%{transform:translateY(0)} 50%{transform:translateY(-8px)} }
@keyframes logoEntry  { from{opacity:0;transform:scale(.7) rotate(-10deg)} to{opacity:1;transform:scale(1) rotate(0deg)} }

@layer utilities {
  .animate-fade-in     { animation:fadeIn     200ms var(--ease-out-expo) both; }
  .animate-slide-up    { animation:slideUp    380ms var(--ease-out-expo) both; }
  .animate-slide-down  { animation:slideDown  300ms var(--ease-out-expo) both; }
  .animate-slide-left  { animation:slideLeft  300ms var(--ease-out-expo) both; }
  .animate-slide-right { animation:slideRight 300ms var(--ease-out-expo) both; }
  .animate-scale-in    { animation:scaleIn    220ms var(--ease-out-expo) both; }
  .animate-pulse-dot   { animation:pulse 2s ease-in-out infinite; }
  .animate-spin        { animation:spinSlow 1s linear infinite; }
  .animate-glow        { animation:glowPulse 3s ease-in-out infinite; }
  .animate-float       { animation:float 4s ease-in-out infinite; }
  .animate-logo        { animation:logoEntry 600ms var(--ease-spring) both; }
  .shimmer-bg { background:linear-gradient(90deg,var(--bg-elevated) 0%,var(--bg-hover) 50%,var(--bg-elevated) 100%); background-size:200% 100%; animation:shimmer 1.8s linear infinite; }
  .delay-50{animation-delay:50ms!important}   .delay-100{animation-delay:100ms!important}  .delay-150{animation-delay:150ms!important}
  .delay-200{animation-delay:200ms!important} .delay-250{animation-delay:250ms!important}  .delay-300{animation-delay:300ms!important}
  .delay-350{animation-delay:350ms!important} .delay-400{animation-delay:400ms!important}  .delay-500{animation-delay:500ms!important}
  .delay-600{animation-delay:600ms!important} .delay-700{animation-delay:700ms!important}
}
EOF
ok "src/index.css"


# =============================================================================
# STEP 8 — src/types/index.ts — canonical type definitions
# =============================================================================
step 8 "src/types/index.ts — AuthContextType removed, AuthUser/UserRole kept"

cat > src/types/index.ts << 'EOF'
// ─── API Response Envelope ────────────────────────────────────────────────────
export interface ApiResponse<T = unknown> {
  success: boolean
  data?: T
  message?: string
  errorCode?: string
  traceId?: string
}

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// ─── Auth ─────────────────────────────────────────────────────────────────────
export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  requiresMfa: boolean
  userId: string
  email: string
  role: UserRole
  permissions: string[]
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export type UserRole =
  | 'SuperAdmin'
  | 'Admin'
  | 'BrokerageAdmin'
  | 'Broker'
  | 'Investor'

// ─── User ─────────────────────────────────────────────────────────────────────
export interface AuthUser {
  userId: string
  email: string
  role: UserRole
  permissions: string[]
  accessToken: string
  refreshToken: string
  expiresAt: number   // unix ms
}

// ─── Orders ──────────────────────────────────────────────────────────────────
export type OrderSide   = 'Buy' | 'Sell'
export type OrderType   = 'Market' | 'Limit' | 'StopLoss' | 'StopLimit'
export type OrderStatus =
  | 'Pending' | 'Open' | 'PartiallyFilled'
  | 'Filled'  | 'Cancelled' | 'Rejected' | 'Expired'

export interface Order {
  orderId: string
  symbol: string
  side: OrderSide
  type: OrderType
  quantity: number
  filledQuantity: number
  price?: number
  stopPrice?: number
  status: OrderStatus
  createdAt: string
  updatedAt: string
  brokerageId?: string
  investorId?: string
}

export interface PlaceOrderRequest {
  symbol: string
  side: OrderSide
  type: OrderType
  quantity: number
  price?: number
  stopPrice?: number
}

export interface CancelOrderRequest {
  orderId: string
  reason?: string
}

// ─── Portfolio ────────────────────────────────────────────────────────────────
export interface PortfolioSummary {
  totalValue: number
  cashBalance: number
  investedAmount: number
  todayPnl: number
  todayPnlPercent: number
  totalPnl: number
  totalPnlPercent: number
}

export interface Holding {
  symbol: string
  companyName: string
  quantity: number
  avgCostPrice: number
  currentPrice: number
  currentValue: number
  unrealizedPnl: number
  unrealizedPnlPercent: number
}

// ─── Market Data ──────────────────────────────────────────────────────────────
export interface MarketTicker {
  symbol: string
  name: string
  lastPrice: number
  change: number
  changePercent: number
  volume: number
  high: number
  low: number
  open: number
  previousClose: number
}

// ─── Navigation ───────────────────────────────────────────────────────────────
export interface NavItem {
  label: string
  path: string
  icon: React.ReactNode
  roles?: UserRole[]
  badge?: number
}
EOF
ok "src/types/index.ts"


# =============================================================================
# STEP 9 — src/store/authStore.ts — Zustand (replaces AuthContext)
# =============================================================================
step 9 "src/store/authStore.ts — Zustand replaces AuthContext"

cat > src/store/authStore.ts << 'EOF'
import { create } from 'zustand'
import { persist } from 'zustand/middleware'
import type { AuthUser } from '@/types'

interface AuthState {
  user: AuthUser | null
  isAuthenticated: boolean
  setUser: (user: AuthUser) => void
  logout: () => void
}

const STORAGE_KEY = 'bd_oms_auth'

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      user: null,
      isAuthenticated: false,
      setUser: (user: AuthUser) => {
        set({ user, isAuthenticated: true })
      },
      logout: () => {
        set({ user: null, isAuthenticated: false })
      },
    }),
    {
      name: STORAGE_KEY,
      partialize: (state) => ({
        user: state.user,
        isAuthenticated: state.isAuthenticated,
      }),
    },
  ),
)

// Selector helpers
export const selectUser            = (s: AuthState) => s.user
export const selectIsAuthenticated = (s: AuthState) => s.isAuthenticated
export const selectRole            = (s: AuthState) => s.user?.role
export const selectPermissions     = (s: AuthState) => s.user?.permissions ?? []
EOF
ok "src/store/authStore.ts"


# =============================================================================
# STEP 10 — src/store/themeStore.ts — 5 themes × 6 accents
# =============================================================================
step 10 "src/store/themeStore.ts — theme engine v2"

cat > src/store/themeStore.ts << 'EOF'
import { create } from 'zustand'
import { persist } from 'zustand/middleware'

export type ThemeId   = 'obsidian' | 'midnight' | 'slate' | 'aurora' | 'arctic'
export type AccentId  = 'azure' | 'emerald' | 'amber' | 'rose' | 'violet' | 'cyan'
export type DensityId = 'compact' | 'comfortable' | 'spacious'

export interface ThemeOption   { id: ThemeId;   label: string; emoji: string; dark: boolean }
export interface AccentOption  { id: AccentId;  label: string; color: string }
export interface DensityOption { id: DensityId; label: string; desc: string  }

export const THEMES: ThemeOption[] = [
  { id: 'obsidian', label: 'Obsidian', emoji: '⬛', dark: true  },
  { id: 'midnight', label: 'Midnight', emoji: '🌑', dark: true  },
  { id: 'slate',    label: 'Slate',    emoji: '🌫',  dark: true  },
  { id: 'aurora',   label: 'Aurora',   emoji: '🌊', dark: true  },
  { id: 'arctic',   label: 'Arctic',   emoji: '☀️', dark: false },
]
export const ACCENTS: AccentOption[] = [
  { id: 'azure',   label: 'Azure',   color: '#3B82F6' },
  { id: 'cyan',    label: 'Cyan',    color: '#06B6D4' },
  { id: 'emerald', label: 'Emerald', color: '#10B981' },
  { id: 'violet',  label: 'Violet',  color: '#8B5CF6' },
  { id: 'rose',    label: 'Rose',    color: '#F43F5E' },
  { id: 'amber',   label: 'Amber',   color: '#F59E0B' },
]
export const DENSITIES: DensityOption[] = [
  { id: 'compact',     label: 'Compact',     desc: 'Max data density' },
  { id: 'comfortable', label: 'Comfortable', desc: 'Balanced default'  },
  { id: 'spacious',    label: 'Spacious',    desc: 'Relaxed reading'   },
]

interface ThemeState {
  theme: ThemeId; accent: AccentId; density: DensityId
  sidebarCollapsed: boolean; tickerEnabled: boolean
  setTheme: (t: ThemeId) => void; setAccent: (a: AccentId) => void
  setDensity: (d: DensityId) => void; toggleSidebar: () => void
  setSidebarCollapsed: (v: boolean) => void; toggleTicker: () => void
}

function applyTheme(theme: ThemeId, accent: AccentId, density: DensityId) {
  const r = document.documentElement
  r.setAttribute('data-theme',   theme)
  r.setAttribute('data-accent',  accent)
  r.setAttribute('data-density', density)
}

export const useThemeStore = create<ThemeState>()(
  persist(
    (set, get) => ({
      theme: 'obsidian', accent: 'azure', density: 'comfortable',
      sidebarCollapsed: false, tickerEnabled: true,
      setTheme:   (theme)   => { set({ theme });   applyTheme(theme,       get().accent, get().density) },
      setAccent:  (accent)  => { set({ accent });  applyTheme(get().theme, accent,       get().density) },
      setDensity: (density) => { set({ density }); applyTheme(get().theme, get().accent, density) },
      toggleSidebar:       () => set(s => ({ sidebarCollapsed: !s.sidebarCollapsed })),
      setSidebarCollapsed: (v) => set({ sidebarCollapsed: v }),
      toggleTicker:        () => set(s => ({ tickerEnabled: !s.tickerEnabled })),
    }),
    {
      name: 'bd_oms_theme_v2',
      onRehydrateStorage: () => (state) => {
        if (state) applyTheme(state.theme, state.accent, state.density)
      },
    }
  )
)

// Apply immediately before React mounts (prevents flash)
applyTheme('obsidian', 'azure', 'comfortable')
EOF
ok "src/store/themeStore.ts"


# =============================================================================
# STEP 11 — src/api/client.ts — Axios with proactive JWT refresh
# =============================================================================
step 11 "src/api/client.ts — Axios + JWT refresh queue (replaces axios.ts)"

cat > src/api/client.ts << 'EOF'
import axios, {
  AxiosInstance, AxiosRequestConfig, AxiosResponse, InternalAxiosRequestConfig,
} from 'axios'
import { useAuthStore } from '@/store/authStore'
import type { ApiResponse, RefreshTokenRequest, LoginResponse } from '@/types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'
const TOKEN_REFRESH_THRESHOLD_MS = 60 * 1000

export const apiClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 30_000,
})

let isRefreshing = false
let refreshQueue: Array<{ resolve: (token: string) => void; reject: (err: unknown) => void }> = []

function processRefreshQueue(token: string | null, error?: unknown) {
  refreshQueue.forEach((cb) => { if (token) cb.resolve(token); else cb.reject(error) })
  refreshQueue = []
}

async function refreshAccessToken(): Promise<string> {
  const { user, setUser, logout } = useAuthStore.getState()
  if (!user?.refreshToken) { logout(); throw new Error('No refresh token') }
  try {
    const body: RefreshTokenRequest = { refreshToken: user.refreshToken }
    const res = await axios.post<ApiResponse<LoginResponse>>(`${BASE_URL}/auth/refresh`, body)
    const data = res.data.data
    if (!data?.accessToken) throw new Error('Refresh returned no token')
    setUser({ ...user, accessToken: data.accessToken, refreshToken: data.refreshToken ?? user.refreshToken, expiresAt: Date.now() + data.expiresIn * 1000 })
    return data.accessToken
  } catch (err) { logout(); throw err }
}

apiClient.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    const { user } = useAuthStore.getState()
    if (!user?.accessToken) return config
    if (user.expiresAt - Date.now() < TOKEN_REFRESH_THRESHOLD_MS) {
      if (!isRefreshing) {
        isRefreshing = true
        try {
          const newToken = await refreshAccessToken()
          processRefreshQueue(newToken)
          config.headers.Authorization = `Bearer ${newToken}`
        } catch (err) { processRefreshQueue(null, err); return Promise.reject(err) }
        finally { isRefreshing = false }
      } else {
        const token = await new Promise<string>((resolve, reject) => { refreshQueue.push({ resolve, reject }) })
        config.headers.Authorization = `Bearer ${token}`
        return config
      }
    } else {
      config.headers.Authorization = `Bearer ${user.accessToken}`
    }
    return config
  },
  (error) => Promise.reject(error),
)

apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean }
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      if (isRefreshing) {
        try {
          const token = await new Promise<string>((resolve, reject) => { refreshQueue.push({ resolve, reject }) })
          if (originalRequest.headers) (originalRequest.headers as Record<string, string>)['Authorization'] = `Bearer ${token}`
          return apiClient(originalRequest)
        } catch (queueError) { return Promise.reject(queueError) }
      }
      isRefreshing = true
      try {
        const newToken = await refreshAccessToken()
        processRefreshQueue(newToken)
        if (originalRequest.headers) (originalRequest.headers as Record<string, string>)['Authorization'] = `Bearer ${newToken}`
        return apiClient(originalRequest)
      } catch (refreshError) { processRefreshQueue(null, refreshError); return Promise.reject(refreshError) }
      finally { isRefreshing = false }
    }
    return Promise.reject(error)
  },
)

export async function apiGet<T>(url: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const res = await apiClient.get<ApiResponse<T>>(url, config); return res.data
}
export async function apiPost<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const res = await apiClient.post<ApiResponse<T>>(url, data, config); return res.data
}
export async function apiPut<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const res = await apiClient.put<ApiResponse<T>>(url, data, config); return res.data
}
export async function apiDelete<T>(url: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const res = await apiClient.delete<ApiResponse<T>>(url, config); return res.data
}
EOF
ok "src/api/client.ts"


# =============================================================================
# STEP 12 — src/api/auth.ts
# =============================================================================
step 12 "src/api/auth.ts"

cat > src/api/auth.ts << 'EOF'
import { apiPost } from './client'
import type { ApiResponse, LoginRequest, LoginResponse, RefreshTokenRequest } from '@/types'

export const authApi = {
  login(body: LoginRequest):   Promise<ApiResponse<LoginResponse>> { return apiPost<LoginResponse>('/auth/login', body) },
  refresh(body: RefreshTokenRequest): Promise<ApiResponse<LoginResponse>> { return apiPost<LoginResponse>('/auth/refresh', body) },
  logout(): Promise<ApiResponse<void>>         { return apiPost<void>('/auth/logout') },
  me():     Promise<ApiResponse<LoginResponse>> { return apiPost<LoginResponse>('/auth/me') },
}
EOF
ok "src/api/auth.ts"


# =============================================================================
# STEP 13 — src/api/orders.ts
# =============================================================================
step 13 "src/api/orders.ts"

cat > src/api/orders.ts << 'EOF'
import { apiGet, apiPost } from './client'
import type { ApiResponse, PaginatedResponse, Order, PlaceOrderRequest, CancelOrderRequest } from '@/types'

export const ordersApi = {
  list(params?: { page?: number; pageSize?: number; status?: string }): Promise<ApiResponse<PaginatedResponse<Order>>> {
    return apiGet<PaginatedResponse<Order>>('/orders', { params })
  },
  getById(orderId: string): Promise<ApiResponse<Order>> {
    return apiGet<Order>(`/orders/${orderId}`)
  },
  place(body: PlaceOrderRequest): Promise<ApiResponse<Order>> {
    return apiPost<Order>('/orders', body)
  },
  cancel(body: CancelOrderRequest): Promise<ApiResponse<void>> {
    return apiPost<void>(`/orders/${body.orderId}/cancel`, { reason: body.reason })
  },
}
EOF
ok "src/api/orders.ts"


# =============================================================================
# STEP 14 — src/hooks/useAuth.ts — replaces useContext(AuthContext)
# =============================================================================
step 14 "src/hooks/useAuth.ts — hook that replaces useContext(AuthContext)"

cat > src/hooks/useAuth.ts << 'EOF'
import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import type { LoginRequest, AuthUser } from '@/types'

export function useAuth() {
  const navigate = useNavigate()
  const { user, isAuthenticated, setUser, logout: storeLogout } = useAuthStore()
  const [isLoading, setIsLoading] = useState(false)
  const [error,     setError]     = useState<string | null>(null)

  const login = useCallback(async (credentials: LoginRequest) => {
    setIsLoading(true)
    setError(null)
    try {
      const res = await authApi.login(credentials)
      if (!res.success || !res.data) { setError(res.message ?? 'Login failed'); return false }
      const { data } = res
      if (data.requiresMfa) { navigate('/auth/mfa', { state: { email: credentials.email } }); return false }
      const authUser: AuthUser = {
        userId: data.userId, email: data.email, role: data.role,
        permissions: data.permissions, accessToken: data.accessToken,
        refreshToken: data.refreshToken, expiresAt: Date.now() + data.expiresIn * 1000,
      }
      setUser(authUser)
      navigate('/dashboard')
      return true
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'An unexpected error occurred')
      return false
    } finally { setIsLoading(false) }
  }, [navigate, setUser])

  const logout = useCallback(async () => {
    try { await authApi.logout() } catch { /* swallow */ } finally { storeLogout(); navigate('/login') }
  }, [storeLogout, navigate])

  const hasPermission = useCallback((permission: string) => user?.permissions.includes(permission) ?? false, [user])
  const hasRole       = useCallback((...roles: AuthUser['role'][]) => user ? roles.includes(user.role) : false, [user])

  return { user, isAuthenticated, isLoading, error, login, logout, hasPermission, hasRole, clearError: () => setError(null) }
}
EOF
ok "src/hooks/useAuth.ts"


# =============================================================================
# STEP 15 — src/hooks/useSignalR.ts
# =============================================================================
step 15 "src/hooks/useSignalR.ts"

cat > src/hooks/useSignalR.ts << 'EOF'
import { useEffect, useRef, useCallback } from 'react'
import { HubConnectionBuilder, HubConnection, LogLevel } from '@microsoft/signalr'
import { useAuthStore } from '@/store/authStore'

interface UseSignalROptions {
  hubUrl: string
  events: Record<string, (...args: unknown[]) => void>
  enabled?: boolean
}

export function useSignalR({ hubUrl, events, enabled = true }: UseSignalROptions) {
  const connectionRef = useRef<HubConnection | null>(null)
  const { user } = useAuthStore()

  useEffect(() => {
    if (!enabled || !user?.accessToken) return

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl, { accessTokenFactory: () => user.accessToken })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build()

    connectionRef.current = connection

    Object.entries(events).forEach(([event, handler]) => {
      connection.on(event, handler)
    })

    connection.start().catch(console.error)

    return () => {
      Object.keys(events).forEach(event => connection.off(event))
      connection.stop()
    }
  }, [hubUrl, enabled, user?.accessToken])

  const invoke = useCallback(async (method: string, ...args: unknown[]) => {
    if (connectionRef.current?.state === 'Connected') {
      return connectionRef.current.invoke(method, ...args)
    }
  }, [])

  return { invoke }
}
EOF
ok "src/hooks/useSignalR.ts"


# =============================================================================
# STEP 16 — src/components/auth/ProtectedRoute.tsx
# =============================================================================
step 16 "src/components/auth/ProtectedRoute.tsx — uses authStore (not AuthContext)"

cat > src/components/auth/ProtectedRoute.tsx << 'EOF'
import { Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import type { UserRole } from '@/types'

interface ProtectedRouteProps {
  allowedRoles?: UserRole[]
}

export function ProtectedRoute({ allowedRoles }: ProtectedRouteProps) {
  const location = useLocation()
  const { isAuthenticated, user } = useAuthStore()

  if (!isAuthenticated || !user) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  if (allowedRoles && !allowedRoles.includes(user.role)) {
    return <Navigate to="/forbidden" replace />
  }

  return <Outlet />
}
EOF
ok "src/components/auth/ProtectedRoute.tsx"


# =============================================================================
# STEP 17 — src/components/ui/Logo.tsx
# =============================================================================
step 17 "src/components/ui/Logo.tsx — hexagon SVG component"

cat > src/components/ui/Logo.tsx << 'EOF'
interface LogoProps {
  size?: number; animated?: boolean; showText?: boolean; textSize?: 'sm' | 'md' | 'lg'
}

export function Logo({ size = 36, animated = false, showText = false, textSize = 'md' }: LogoProps) {
  const textSizes = { sm: { name: 13, sub: 9 }, md: { name: 16, sub: 10 }, lg: { name: 22, sub: 12 } }
  const ts = textSizes[textSize]

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: size * 0.33 }}>
      <svg width={size} height={size} viewBox="0 0 48 48"
        className={animated ? 'animate-logo' : undefined}
        style={{ flexShrink: 0 }}>
        <defs>
          <linearGradient id="logo-g1" x1="0%" y1="0%" x2="100%" y2="100%">
            <stop offset="0%" stopColor="var(--accent-400)"/>
            <stop offset="100%" stopColor="var(--accent-600)"/>
          </linearGradient>
          <linearGradient id="logo-g2" x1="0%" y1="100%" x2="100%" y2="0%">
            <stop offset="0%" stopColor="var(--accent-600)" stopOpacity="0.3"/>
            <stop offset="100%" stopColor="var(--accent-300)" stopOpacity="0.7"/>
          </linearGradient>
          <filter id="logo-glow">
            <feGaussianBlur stdDeviation="1.5" result="coloredBlur"/>
            <feMerge><feMergeNode in="coloredBlur"/><feMergeNode in="SourceGraphic"/></feMerge>
          </filter>
        </defs>
        <polygon points="24,2 43,13 43,35 24,46 5,35 5,13" fill="var(--accent-glow)" stroke="url(#logo-g1)" strokeWidth="1.2"/>
        <polygon points="24,8 38,16.5 38,31.5 24,40 10,31.5 10,16.5" fill="none" stroke="url(#logo-g2)" strokeWidth="0.7" opacity="0.5"/>
        <polyline points="13,31 19,23 25,26.5 35,16" fill="none" stroke="url(#logo-g1)" strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round" filter="url(#logo-glow)"/>
        <circle cx="13" cy="31" r="2" fill="var(--accent-500)" opacity="0.7"/>
        <circle cx="35" cy="16" r="3" fill="var(--accent-400)" filter="url(#logo-glow)"/>
        <circle cx="35" cy="16" r="1.5" fill="#fff" opacity="0.9"/>
        <polyline points="31.5,13 35,16 38.5,13" fill="none" stroke="var(--accent-300)" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"/>
      </svg>
      {showText && (
        <div style={{ lineHeight: 1.1 }}>
          <div style={{ fontFamily: 'var(--font-display)', fontWeight: 800, fontSize: ts.name, color: 'var(--text-primary)', letterSpacing: '-0.03em', lineHeight: 1 }}>
            BD<span style={{ color: 'var(--accent-400)' }}>OMS</span>
          </div>
          <div style={{ fontSize: ts.sub, color: 'var(--text-tertiary)', letterSpacing: '.06em', textTransform: 'uppercase', marginTop: 2, fontWeight: 500 }}>
            Order Management
          </div>
        </div>
      )}
    </div>
  )
}
EOF
ok "src/components/ui/Logo.tsx"


# =============================================================================
# STEP 18 — src/main.tsx — clean entry point, no AuthContext provider
# =============================================================================
step 18 "src/main.tsx — clean entry point (NO AuthProvider wrapper)"

cat > src/main.tsx << 'EOF'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App'
import './index.css'

const rootEl = document.getElementById('root')
if (!rootEl) throw new Error('Root element not found')

createRoot(rootEl).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
EOF
ok "src/main.tsx"


# =============================================================================
# STEP 19 — src/App.tsx — all routes, /signup added
# =============================================================================
step 19 "src/App.tsx — BrowserRouter with /login /signup /dashboard"

cat > src/App.tsx << 'EOF'
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ProtectedRoute }  from '@/components/auth/ProtectedRoute'
import { DashboardLayout } from '@/components/layout/DashboardLayout'
import { LoginPage }       from '@/pages/LoginPage'
import { SignUpPage }      from '@/pages/SignUpPage'
import { DashboardPage }   from '@/pages/DashboardPage'
import {
  OrdersPage, PortfolioPage, MarketPage,
  WatchlistPage, ReportsPage,
  ForbiddenPage, NotFoundPage,
} from '@/pages/PlaceholderPages'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* ── Public ─────────────────────────────────────── */}
        <Route path="/login"     element={<LoginPage />} />
        <Route path="/signup"    element={<SignUpPage />} />
        <Route path="/register"  element={<Navigate to="/signup" replace />} />
        <Route path="/forbidden" element={<ForbiddenPage />} />
        <Route path="/"          element={<Navigate to="/dashboard" replace />} />

        {/* ── Authenticated (any role) ────────────────────── */}
        <Route element={<ProtectedRoute />}>
          <Route element={<DashboardLayout />}>
            <Route path="/dashboard" element={<DashboardPage />} />
            <Route path="/orders"    element={<OrdersPage />} />
            <Route path="/portfolio" element={<PortfolioPage />} />
            <Route path="/market"    element={<MarketPage />} />
            <Route path="/watchlist" element={<WatchlistPage />} />
            <Route path="/reports"   element={<ReportsPage />} />
          </Route>
        </Route>

        {/* ── Admin-only ──────────────────────────────────── */}
        <Route element={<ProtectedRoute allowedRoles={['Admin', 'SuperAdmin']} />}>
          <Route element={<DashboardLayout />}>
            <Route path="/admin/users"      element={<div style={{ padding: 24, color: 'var(--text-secondary)' }}>Admin: Users — Day 51</div>} />
            <Route path="/admin/compliance" element={<div style={{ padding: 24, color: 'var(--text-secondary)' }}>Compliance — Day 51</div>} />
            <Route path="/admin/settings"   element={<div style={{ padding: 24, color: 'var(--text-secondary)' }}>Settings — Day 51</div>} />
          </Route>
        </Route>

        <Route path="*" element={<NotFoundPage />} />
      </Routes>
    </BrowserRouter>
  )
}
EOF
ok "src/App.tsx"


# =============================================================================
# STEP 20 — src/pages/PlaceholderPages.tsx
# =============================================================================
step 20 "src/pages/PlaceholderPages.tsx — 403/404 + coming-soon pages"

cat > src/pages/PlaceholderPages.tsx << 'EOF'
import { Link } from 'react-router-dom'

function ComingSoon({ title, day }: { title: string; day: number }) {
  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <h1 style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 22, color: 'var(--text-primary)', letterSpacing: '-0.02em', marginBottom: 6 }}>{title}</h1>
      <div style={{ background: 'var(--bg-surface)', border: '1px solid var(--border-subtle)', borderRadius: 'var(--r-xl)', padding: '48px 32px', textAlign: 'center', marginTop: 24 }}>
        <div style={{ width: 64, height: 64, borderRadius: '50%', background: 'var(--accent-glow)', border: '1px solid color-mix(in srgb,var(--accent-500) 30%,transparent)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px' }}>
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--accent-400)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
        </div>
        <p style={{ fontSize: 15, color: 'var(--text-secondary)', marginBottom: 8 }}><strong style={{ color: 'var(--text-primary)' }}>{title}</strong> is coming in Day {day}</p>
        <p style={{ fontSize: 12, color: 'var(--text-tertiary)' }}>Full implementation with real-time data, charts, and advanced filtering.</p>
      </div>
    </div>
  )
}

export const OrdersPage    = () => <ComingSoon title="Orders"       day={50} />
export const PortfolioPage = () => <ComingSoon title="Portfolio"    day={50} />
export const MarketPage    = () => <ComingSoon title="Market Watch" day={50} />
export const WatchlistPage = () => <ComingSoon title="Watchlist"    day={51} />
export const ReportsPage   = () => <ComingSoon title="Reports"      day={51} />

export function ForbiddenPage() {
  return (
    <div style={{ minHeight:'100vh', background:'var(--bg-base)', display:'flex', alignItems:'center', justifyContent:'center', padding:24 }}>
      <div style={{ textAlign:'center' }}>
        <div style={{ fontFamily:'var(--font-mono)', fontSize:72, fontWeight:700, color:'var(--bear-muted)', lineHeight:1, marginBottom:8 }}>403</div>
        <h1 style={{ fontFamily:'var(--font-display)', fontWeight:700, fontSize:22, color:'var(--text-primary)', marginBottom:8 }}>Access Forbidden</h1>
        <p style={{ color:'var(--text-secondary)', fontSize:14, marginBottom:24 }}>You don't have permission to view this page.</p>
        <Link to="/dashboard" className="btn btn-primary">← Back to Dashboard</Link>
      </div>
    </div>
  )
}

export function NotFoundPage() {
  return (
    <div style={{ minHeight:'100vh', background:'var(--bg-base)', display:'flex', alignItems:'center', justifyContent:'center', padding:24 }}>
      <div style={{ textAlign:'center' }}>
        <div style={{ fontFamily:'var(--font-mono)', fontSize:72, fontWeight:700, lineHeight:1, marginBottom:8, background:'linear-gradient(135deg,var(--accent-400),var(--accent-600))', WebkitBackgroundClip:'text', WebkitTextFillColor:'transparent' }}>404</div>
        <h1 style={{ fontFamily:'var(--font-display)', fontWeight:700, fontSize:22, color:'var(--text-primary)', marginBottom:8 }}>Page Not Found</h1>
        <p style={{ color:'var(--text-secondary)', fontSize:14, marginBottom:24 }}>The page you're looking for doesn't exist.</p>
        <Link to="/dashboard" className="btn btn-primary">← Back to Dashboard</Link>
      </div>
    </div>
  )
}
EOF
ok "src/pages/PlaceholderPages.tsx"


# =============================================================================
# STEP 21 — Copy large files from the outputs folder
#            (LoginPage, SignUpPage, Sidebar, Topbar, ThemePanel, DashboardLayout,
#             DashboardPage, MarketTickerBar, Alert, Spinner)
# =============================================================================
step 21 "Copy pre-built files from Claude outputs"

# Define outputs base path — ADJUST THIS to match your actual outputs location
OUTPUTS="/mnt/user-data/outputs/BdStockOMS.Client"

copy_file() {
  local src="$OUTPUTS/$1"
  local dst="$1"
  if [ -f "$src" ]; then
    mkdir -p "$(dirname "$dst")"
    cp "$src" "$dst"
    ok "$dst"
  else
    warn "NOT FOUND in outputs: $src — you will need to create this manually"
  fi
}

copy_file "src/pages/LoginPage.tsx"
copy_file "src/pages/SignUpPage.tsx"
copy_file "src/pages/DashboardPage.tsx"
copy_file "src/components/layout/DashboardLayout.tsx"
copy_file "src/components/layout/Sidebar.tsx"
copy_file "src/components/layout/Topbar.tsx"
copy_file "src/components/ui/ThemePanel.tsx"
copy_file "src/components/ui/Alert.tsx"
copy_file "src/components/ui/Spinner.tsx"
copy_file "src/components/widgets/MarketTickerBar.tsx"


# =============================================================================
# STEP 22 — Verify: grep for AuthContext references (should be zero)
# =============================================================================
step 22 "Verify — no AuthContext references remain in new files"

echo ""
REMAINING=$(grep -r "AuthContext\|useContext\|AuthProvider" src/ \
  --include="*.tsx" --include="*.ts" \
  --exclude-dir=".archive_day48" \
  -l 2>/dev/null | grep -v ".archive_day48" || true)

if [ -z "$REMAINING" ]; then
  ok "Zero AuthContext references in src/ ✓"
else
  warn "AuthContext still referenced in:"
  echo "$REMAINING" | while read f; do echo "    $f"; done
  echo ""
  info "These files need their imports updated to use useAuthStore instead:"
  info "  Old: import { useAuth } from '../context/AuthContext'"
  info "  New: import { useAuthStore } from '@/store/authStore'"
fi


# =============================================================================
# STEP 23 — Check all critical files exist
# =============================================================================
step 23 "File verification"

echo ""
MISSING=0
FILES=(
  "index.html"
  "tailwind.config.js"
  "public/favicon.svg"
  "src/styles/themes.css"
  "src/index.css"
  "src/main.tsx"
  "src/App.tsx"
  "src/types/index.ts"
  "src/store/authStore.ts"
  "src/store/themeStore.ts"
  "src/api/client.ts"
  "src/api/auth.ts"
  "src/api/orders.ts"
  "src/hooks/useAuth.ts"
  "src/hooks/useSignalR.ts"
  "src/components/auth/ProtectedRoute.tsx"
  "src/components/ui/Logo.tsx"
  "src/components/ui/ThemePanel.tsx"
  "src/components/ui/Alert.tsx"
  "src/components/ui/Spinner.tsx"
  "src/components/layout/DashboardLayout.tsx"
  "src/components/layout/Sidebar.tsx"
  "src/components/layout/Topbar.tsx"
  "src/components/widgets/MarketTickerBar.tsx"
  "src/pages/LoginPage.tsx"
  "src/pages/SignUpPage.tsx"
  "src/pages/DashboardPage.tsx"
  "src/pages/PlaceholderPages.tsx"
)

for f in "${FILES[@]}"; do
  if [ -f "$f" ]; then
    echo -e "  ${GREEN}✓${NC} $f"
  else
    echo -e "  ${RED}✗ MISSING: $f${NC}"
    MISSING=$((MISSING+1))
  fi
done

echo ""
if [ $MISSING -eq 0 ]; then
  ok "All $((${#FILES[@]})) files present!"
else
  warn "$MISSING file(s) missing — fix before running dev server"
fi


# =============================================================================
# STEP 24 — Git commit
# =============================================================================
step 24 "Git commit"

git add -A
git status --short
git commit -m "feat(ui): day-50 — AuthContext→Zustand migration + design system v2

Design & Developed by Eshan Barua

Architecture:
- REMOVED: src/context/AuthContext.tsx (backed up to .archive_day48/)
- REMOVED: old src/api/axios.ts (replaced by src/api/client.ts)
- REPLACED: useContext(AuthContext) → useAuthStore (Zustand + persist)
- REPLACED: src/components/ProtectedRoute → src/components/auth/ProtectedRoute

New files:
- src/store/authStore.ts (Zustand auth state)
- src/store/themeStore.ts (5 themes × 6 accents × 3 densities)
- src/api/client.ts (Axios + proactive JWT refresh queue)
- src/hooks/useAuth.ts (login/logout/hasRole/hasPermission)
- src/components/ui/Logo.tsx (crystalline hexagon SVG)
- src/pages/SignUpPage.tsx (3-step registration)

Design System v2:
- Fonts: Outfit + Space Grotesk + Space Mono
- 5 themes: Obsidian, Midnight, Slate, Aurora, Arctic
- 6 accents: Azure, Cyan, Emerald, Violet, Rose, Amber
- LoginPage: canvas animation, floating chips, credit footer
- SignUpPage: 3-step flow, role selector, password strength
- ThemePanel: visual swatches, toggle switch for ticker
- Sidebar: Logo, role-colored avatars, density-aware padding
- Topbar: live BDT clock, market status, notification bell"

ok "Git commit complete"


# =============================================================================
# STEP 25 — Run dev server
# =============================================================================
step 25 "Launch dev server"

echo ""
echo -e "${CYAN}Starting dev server…${NC}"
echo -e "  ${YELLOW}➜  http://localhost:5173/login${NC}"
echo -e "  ${YELLOW}➜  http://localhost:5173/signup${NC}"
echo -e "  ${YELLOW}➜  http://localhost:5173/dashboard${NC}  (redirects to /login if not auth'd)"
echo ""
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo -e "${GREEN}  TROUBLESHOOTING GUIDE${NC}"
echo -e "${GREEN}━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━${NC}"
echo ""
echo -e "  ${RED}ERROR:${NC} Cannot find module '@/store/authStore'"
echo -e "    → Check vite.config.ts has: alias: { '@': path.resolve(__dirname, './src') }"
echo ""
echo -e "  ${RED}ERROR:${NC} 'zustand' not found"
echo -e "    → Run: npm install zustand"
echo ""
echo -e "  ${RED}ERROR:${NC} Fonts not loading"
echo -e "    → Check index.html has the Google Fonts <link> tags"
echo -e "    → Hard refresh: Ctrl+Shift+R"
echo ""
echo -e "  ${RED}ERROR:${NC} Theme not applying / wrong colours"
echo -e "    → Clear localStorage in DevTools console:"
echo -e "      localStorage.removeItem('bd_oms_theme_v2'); location.reload()"
echo ""
echo -e "  ${RED}ERROR:${NC} AuthContext import error in an old file"
echo -e "    → Replace: import { useAuth } from '../context/AuthContext'"
echo -e "      With:    import { useAuthStore } from '@/store/authStore'"
echo ""
echo -e "  ${YELLOW}VERIFY no AuthContext left:${NC}"
echo -e "    grep -r \"AuthContext\" src/ --include=\"*.tsx\" --include=\"*.ts\""
echo -e "    # Expected: zero output"
echo ""
npm run dev
