/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — App Root
   ═══════════════════════════════════════════════════════════════ */

import React from 'react';
import { AuthProvider, useAuth } from './stores/AuthStore';
import { MarketDataProvider } from './stores/MarketDataStore';
import AppLayout from './components/AppLayout';
import LoginPage from './components/LoginPage';
import './styles/global.css';

function AppContent() {
  const { isAuthenticated, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="app-loading">
        <div className="app-loading__spinner" />
        <span className="app-loading__text">Initializing BdStockOMS...</span>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <LoginPage />;
  }

  return (
    <MarketDataProvider>
      <AppLayout />
    </MarketDataProvider>
  );
}

export default function App() {
  return (
    <AuthProvider>
      <AppContent />
    </AuthProvider>
  );
}
