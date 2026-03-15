import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ProtectedRoute }    from '@/components/auth/ProtectedRoute'
import { LoginPage }         from '@/pages/LoginPage'
import { SignUpPage }        from '@/pages/SignUpPage'
import { DashboardLayout }   from '@/components/layout/DashboardLayout'
import DashboardPage         from "@/pages/DashboardPage"
import { BuySellConsole }    from '@/components/trading/BuySellConsole'
import { TradeMonitorPage } from '@/pages/TradeMonitorPage'
import {
  OrdersPage, PortfolioPage, MarketPage,
  SuperAdminPage, RbacPage, TenantPage,
  ForbiddenPage, NotFoundPage,
} from '@/pages/PlaceholderPages'

export default function App() {
  return (
    <BrowserRouter>
      <Routes>
        {/* Public */}
        <Route path="/login"  element={<LoginPage />} />
        <Route path="/signup" element={<SignUpPage />} />

        {/* All authenticated users */}
        <Route element={<ProtectedRoute />}>
          <Route element={<DashboardLayout />}>
            <Route path="/dashboard"  element={<DashboardPage />} />
            <Route path="/orders"     element={<OrdersPage />} />
            <Route path="/portfolio"  element={<PortfolioPage />} />
            <Route path="/market"     element={<MarketPage />} />
            <Route path="/trade-monitor" element={<TradeMonitorPage />} />
          </Route>
        </Route>

        {/* SuperAdmin only */}
        <Route element={<ProtectedRoute allowedRoles={['SuperAdmin']} />}>
          <Route element={<DashboardLayout />}>
            <Route path="/super-admin"  element={<SuperAdminPage />} />
            <Route path="/tenants"      element={<TenantPage />} />
            <Route path="/rbac"         element={<RbacPage />} />
          </Route>
        </Route>

        {/* SuperAdmin + Admin */}
        <Route element={<ProtectedRoute allowedRoles={['SuperAdmin','Admin']} />}>
          <Route element={<DashboardLayout />}>
            <Route path="/admin" element={<SuperAdminPage />} />
          </Route>
        </Route>

        {/* Redirects */}
        <Route path="/"    element={<Navigate to="/dashboard" replace />} />
        <Route path="/403" element={<ForbiddenPage />} />
        <Route path="*"    element={<NotFoundPage />} />
      </Routes>

      {/* Global Buy/Sell Console — renders as portal above all routes */}
      <BuySellConsole />
    </BrowserRouter>
  )
}
