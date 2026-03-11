import { createContext, useContext, useState } from 'react';
import type { ReactNode } from 'react';
import api from '../api/axios';
import type { AuthUser, AuthContextType, AuthResponse } from '../types';

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser]   = useState<AuthUser | null>(null);
  const [token, setToken] = useState<string | null>(null);

  const login = async (email: string, password: string) => {
    const response = await api.post<AuthResponse>('/auth/login', { email, password });
    const data = response.data;

    // Store token in memory only — no localStorage
    (window as any).__authToken = data.token;
    (window as any).__authUser = { userId: data.userId, fullName: data.fullName, email: data.email, role: data.role, brokerageHouseId: data.brokerageHouseId, brokerageHouseName: data.brokerageHouseName, expiresAt: data.expiresAt };

    setToken(data.token);
    setUser({
      userId:            data.userId,
      fullName:          data.fullName,
      email:             data.email,
      role:              data.role,
      brokerageHouseId:  data.brokerageHouseId,
      brokerageHouseName: data.brokerageHouseName,
      expiresAt:         data.expiresAt
    });
  };

  const logout = async () => {
    try {
      // Tell backend to blacklist the token
      await api.post('/auth/logout');
    } catch {
      // Ignore errors on logout
    } finally {
      (window as any).__authToken = null;
      setToken(null);
      setUser(null);
    }
  };

  return (
    <AuthContext.Provider value={{
      user,
      token,
      login,
      logout,
      isAuthenticated: !!token
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) throw new Error('useAuth must be used inside AuthProvider');
  return context;
}

// Helper: get redirect path based on role
export function getRoleRedirect(role: string): string {
  switch (role) {
    case 'SuperAdmin':
    case 'Admin':       return '/admin/dashboard';
    case 'Trader':      return '/trader/dashboard';
    case 'CCD':         return '/ccd/dashboard';
    case 'ITSupport':   return '/it/dashboard';
    case 'Investor':    return '/dashboard';
    default:            return '/dashboard';
  }
}
