import { describe, it, expect } from 'vitest'
import { render, screen } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { ProtectedRoute } from '@/components/auth/ProtectedRoute'
import { useAuthStore } from '@/store/authStore'
import type { AuthUser } from '@/types'

const mockUser = (role: AuthUser['role']): AuthUser => ({
  userId: 'u1', email: 'test@test.com', role,
  permissions: [], accessToken: 'tok', refreshToken: 'ref',
  expiresAt: Date.now() + 3600_000,
})

function renderRoute(user: AuthUser | null, allowedRoles?: AuthUser['role'][]) {
  useAuthStore.setState({ user, isAuthenticated: !!user })
  return render(
    <MemoryRouter initialEntries={['/dashboard']}>
      <Routes>
        <Route element={<ProtectedRoute allowedRoles={allowedRoles} />}>
          <Route path="/dashboard" element={<div>Protected Content</div>} />
        </Route>
        <Route path="/login" element={<div>Login Page</div>} />
        <Route path="/forbidden" element={<div>Forbidden</div>} />
      </Routes>
    </MemoryRouter>
  )
}

describe('ProtectedRoute', () => {
  it('redirects unauthenticated user to /login', () => {
    renderRoute(null)
    expect(screen.getByText('Login Page')).toBeInTheDocument()
  })

  it('renders protected content for authenticated user', () => {
    renderRoute(mockUser('Investor'))
    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })

  it('redirects to /forbidden when role not allowed', () => {
    renderRoute(mockUser('Investor'), ['Admin', 'SuperAdmin'])
    expect(screen.getByText('Forbidden')).toBeInTheDocument()
  })

  it('allows Admin through role-restricted route', () => {
    renderRoute(mockUser('Admin'), ['Admin', 'SuperAdmin'])
    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })

  it('allows SuperAdmin through any role-restricted route', () => {
    renderRoute(mockUser('SuperAdmin'), ['Admin', 'SuperAdmin'])
    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })
})
