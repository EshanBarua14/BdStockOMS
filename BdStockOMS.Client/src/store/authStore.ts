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
