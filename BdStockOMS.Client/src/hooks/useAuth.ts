import { useState, useCallback } from 'react'
import { useNavigate } from 'react-router-dom'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import type { LoginRequest, AuthUser } from '@/types'

export function useAuth() {
  const navigate = useNavigate()
  const { user, isAuthenticated, setUser, logout: storeLogout } = useAuthStore()
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const login = useCallback(async (credentials: LoginRequest) => {
    setIsLoading(true)
    setError(null)
    try {
      const data = await authApi.login(credentials)
      const authUser: AuthUser = {
        userId:             data.userId,
        fullName:           data.fullName,
        email:              data.email,
        role:               data.role,
        brokerageHouseId:   data.brokerageHouseId,
        brokerageHouseName: data.brokerageHouseName,
        token:              data.token,
        expiresAt:          new Date(data.expiresAt).getTime(),
      }
      setUser(authUser)
      navigate('/dashboard')
      return true
    } catch (err: unknown) {
      const msg =
        (err as any)?.response?.data?.message ??
        (err instanceof Error ? err.message : 'Login failed')
      setError(msg)
      return false
    } finally {
      setIsLoading(false)
    }
  }, [navigate, setUser])

  const logout = useCallback(async () => {
    try { await authApi.logout() } catch { /* swallow */ }
    finally { storeLogout(); navigate('/login') }
  }, [storeLogout, navigate])

  const hasRole = useCallback(
    (...roles: AuthUser['role'][]) => (user ? roles.includes(user.role) : false),
    [user]
  )

  return {
    user, isAuthenticated, isLoading, error,
    login, logout, hasRole,
    clearError: () => setError(null),
  }
}
