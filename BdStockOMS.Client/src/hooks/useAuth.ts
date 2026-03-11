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
