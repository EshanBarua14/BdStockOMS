import { createContext, useContext, useState } from "react";
import type { ReactNode } from 'react';
import api from '../api/axios';
import type { User, AuthContextType } from '../types';

// Create the context with a default empty value
const AuthContext = createContext<AuthContextType | null>(null);

// Provider wraps the entire app and makes auth available everywhere
export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);

  const login = async (email: string, password: string) => {
    const response = await api.post('/auth/login', { email, password });
    const data = response.data;

    // Store token in memory (window object) — NOT localStorage per your rules
    (window as any).__authToken = data.token;

    setToken(data.token);
    setUser(data.user);
  };

  const logout = () => {
    // Clear everything from memory
    (window as any).__authToken = null;
    setToken(null);
    setUser(null);
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

// Custom hook — any component can call useAuth() to get user/login/logout
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used inside AuthProvider');
  }
  return context;
}
