import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import ChangePasswordPage from './pages/ChangePasswordPage';
import ProfilePage from './pages/ProfilePage';

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Public routes */}
          <Route path="/login" element={<LoginPage />} />

          {/* Protected routes — any authenticated user */}
          <Route path="/dashboard" element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          } />

          <Route path="/change-password" element={
            <ProtectedRoute>
              <ChangePasswordPage />
            </ProtectedRoute>
          } />

          <Route path="/profile" element={
            <ProtectedRoute>
              <ProfilePage />
            </ProtectedRoute>
          } />

          {/* Admin routes */}
          <Route path="/admin/dashboard" element={
            <ProtectedRoute allowedRoles={['SuperAdmin', 'Admin']}>
              <DashboardPage />
            </ProtectedRoute>
          } />

          {/* Trader routes */}
          <Route path="/trader/dashboard" element={
            <ProtectedRoute allowedRoles={['Trader']}>
              <DashboardPage />
            </ProtectedRoute>
          } />

          {/* CCD routes */}
          <Route path="/ccd/dashboard" element={
            <ProtectedRoute allowedRoles={['CCD']}>
              <DashboardPage />
            </ProtectedRoute>
          } />

          {/* IT Support routes */}
          <Route path="/it/dashboard" element={
            <ProtectedRoute allowedRoles={['ITSupport']}>
              <DashboardPage />
            </ProtectedRoute>
          } />

          {/* Default redirects */}
          <Route path="/" element={<Navigate to="/dashboard" replace />} />
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
