/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — LoginPage
   Premium dark glass login screen
   ═══════════════════════════════════════════════════════════════ */

import React, { useState } from 'react';
import { useAuth } from '../stores/AuthStore';
import './LoginPage.css';

export default function LoginPage() {
  const { login, isLoading } = useAuth();
  const [email, setEmail] = useState('admin@bdstockoms.com');
  const [password, setPassword] = useState('Admin@1234');
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await login(email, password);
    } catch (err: any) {
      setError(err.message || 'Login failed');
    }
  };

  return (
    <div className="login-page">
      <div className="ambient-blob ambient-blob-1" />
      <div className="ambient-blob ambient-blob-2" />

      <form className="login-card glass-panel-heavy" onSubmit={handleSubmit}>
        <div className="login-logo">
          <span className="login-logo-icon">◆</span>
          <span className="login-logo-text">
            BdStock<span className="login-logo-accent">OMS</span>
          </span>
        </div>

        <p className="login-subtitle">Professional Trading Workstation</p>

        {error && <div className="login-error">{error}</div>}

        <label className="login-field">
          <span className="login-label">Email</span>
          <input
            type="email"
            className="login-input"
            value={email}
            onChange={e => setEmail(e.target.value)}
            autoFocus
          />
        </label>

        <label className="login-field">
          <span className="login-label">Password</span>
          <input
            type="password"
            className="login-input"
            value={password}
            onChange={e => setPassword(e.target.value)}
          />
        </label>

        <button className="login-btn" type="submit" disabled={isLoading}>
          {isLoading ? 'Connecting...' : 'Sign In'}
        </button>

        <div className="login-footer">
          <span>DSE + CSE Real-Time Trading Platform</span>
        </div>
      </form>
    </div>
  );
}
