import { Navigate, Outlet } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import type { UserRole } from '@/types'

interface Props {
  allowedRoles?: UserRole[]
}

export function ProtectedRoute({ allowedRoles }: Props) {
  const { user, isAuthenticated } = useAuthStore()
  if (!isAuthenticated || !user) return <Navigate to="/login" replace />
  if (allowedRoles && !allowedRoles.includes(user.role))
    return <Navigate to="/403" replace />
  return <Outlet />
}
