import { describe, it, expect, beforeEach } from 'vitest'
import { useAuthStore } from '@/store/authStore'
import type { AuthUser } from '@/types'

const mockUser: AuthUser = {
  userId: 'u1',
  email: 'eshan@bdstockoms.com',
  role: 'Admin',
  permissions: ['orders:read', 'orders:write'],
  accessToken: 'access-token-abc',
  refreshToken: 'refresh-token-xyz',
  expiresAt: Date.now() + 3600_000,
}

beforeEach(() => {
  useAuthStore.setState({ user: null, isAuthenticated: false })
})

describe('authStore', () => {
  it('initialises with null user and unauthenticated', () => {
    const { user, isAuthenticated } = useAuthStore.getState()
    expect(user).toBeNull()
    expect(isAuthenticated).toBe(false)
  })

  it('setUser sets user and marks authenticated', () => {
    useAuthStore.getState().setUser(mockUser)
    const { user, isAuthenticated } = useAuthStore.getState()
    expect(user).toEqual(mockUser)
    expect(isAuthenticated).toBe(true)
  })

  it('logout clears user and sets unauthenticated', () => {
    useAuthStore.getState().setUser(mockUser)
    useAuthStore.getState().logout()
    expect(useAuthStore.getState().user).toBeNull()
    expect(useAuthStore.getState().isAuthenticated).toBe(false)
  })

  it('setUser stores correct role', () => {
    useAuthStore.getState().setUser(mockUser)
    expect(useAuthStore.getState().user?.role).toBe('Admin')
  })

  it('setUser stores permissions array', () => {
    useAuthStore.getState().setUser(mockUser)
    expect(useAuthStore.getState().user?.permissions).toContain('orders:read')
  })

  it('logout is idempotent — calling twice does not throw', () => {
    useAuthStore.getState().setUser(mockUser)
    useAuthStore.getState().logout()
    expect(() => useAuthStore.getState().logout()).not.toThrow()
    expect(useAuthStore.getState().isAuthenticated).toBe(false)
  })
})
