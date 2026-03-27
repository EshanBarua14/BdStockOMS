import AdminSettingsPage from './pages/AdminSettingsPage';
import BrokerManagementPage from '@/pages/BrokerManagementPage';
import BranchManagementPage  from '@/pages/BranchManagementPage';
import BOManagementPage       from '@/pages/BOManagementPage';
import { AdminPlaceholderPage } from './pages/admin/PlaceholderPage';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { ProtectedRoute }    from '@/components/auth/ProtectedRoute'
import { LoginPage }         from '@/pages/LoginPage'
import { SignUpPage }        from '@/pages/SignUpPage'
import { DashboardLayout }   from '@/components/layout/DashboardLayout'
import DashboardPage         from "@/pages/DashboardPage"
import { BuySellConsole }    from '@/components/trading/BuySellConsole'
import { TradeMonitorPage } from '@/pages/TradeMonitorPage'
import { RMSPage } from '@/pages/PlaceholderPages'
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
            <Route path="/rms" element={<RMSPage />} />
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
        <Route element={<ProtectedRoute allowedRoles={['SuperAdmin','Admin','BrokerageAdmin','CCD','Trader','Investor']} />}>
          <Route element={<DashboardLayout />}>
            <Route path="/admin" element={<SuperAdminPage />} />
          </Route>
        </Route>

        {/* Redirects */}
        <Route path="/"    element={<Navigate to="/dashboard" replace />} />
        <Route path="/403" element={<ForbiddenPage />} />
        <Route path="*"    element={<NotFoundPage />} />
                {/* Admin Settings — Day 65 */}
          <Route path="/settings" element={<Navigate to="/settings/general" replace />} />
          <Route path="/settings/:section" element={<AdminSettingsPage />} />
          {/* Admin placeholders */}
          <Route path="/admin/brokers"      element={<BrokerManagementPage />} />
          <Route path="/admin/branches"     element={<BranchManagementPage />} />
          <Route path="/admin/bo-accounts"  element={<BOManagementPage />} />
          <Route path="/admin/users"        element={<AdminPlaceholderPage title="User Management" />} />
          <Route path="/admin/fix"          element={<AdminPlaceholderPage title="FIX Gateway" />} />
          <Route path="/admin/activities"   element={<AdminPlaceholderPage title="Activity Log" />} />
        </Routes>

      {/* Global Buy/Sell Console — renders as portal above all routes */}
      <BuySellConsole />
    </BrowserRouter>
  )
}
