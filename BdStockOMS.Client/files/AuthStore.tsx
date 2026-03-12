/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — Auth Store (React Context)
   User auth, role-based access, session management
   ═══════════════════════════════════════════════════════════════ */

import React, { createContext, useContext, useState, useCallback, useEffect } from 'react';
import { apiService } from '../services/apiService';
import { signalRService } from '../services/signalRService';

export type UserRole = 'SuperAdmin' | 'Admin' | 'Trader' | 'Client' | 'RiskOfficer';

export interface AuthUser {
  userId: number;
  email: string;
  fullName: string;
  role: UserRole;
  permissions: string[];
  avatar?: string;
}

interface AuthContextType {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  hasPermission: (permission: string) => boolean;
  hasRole: (...roles: UserRole[]) => boolean;
}

const AuthContext = createContext<AuthContextType | null>(null);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Check existing token on mount
  useEffect(() => {
    const token = apiService.getToken();
    if (token) {
      // Validate token by fetching user profile
      apiService.get<AuthUser>('/auth/me')
        .then(userData => {
          setUser(userData);
          signalRService.startNotificationHub(token);
        })
        .catch(() => {
          apiService.clearToken();
        })
        .finally(() => setIsLoading(false));
    } else {
      setIsLoading(false);
    }
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    setIsLoading(true);
    try {
      const result = await apiService.login(email, password);
      setUser(result.user);
      signalRService.startNotificationHub(result.token);
    } finally {
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(() => {
    apiService.clearToken();
    signalRService.stopAll();
    setUser(null);
    window.location.href = '/login';
  }, []);

  const hasPermission = useCallback((permission: string) => {
    if (!user) return false;
    if (user.role === 'SuperAdmin') return true;
    return user.permissions.includes(permission);
  }, [user]);

  const hasRole = useCallback((...roles: UserRole[]) => {
    if (!user) return false;
    return roles.includes(user.role);
  }, [user]);

  return (
    <AuthContext.Provider value={{
      user,
      isAuthenticated: !!user,
      isLoading,
      login,
      logout,
      hasPermission,
      hasRole,
    }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextType {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return context;
}
