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
