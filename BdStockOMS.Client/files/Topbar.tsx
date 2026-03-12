/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — Topbar
   Global search | Indexes | Market Status | Notifications | News | Theme | Profile
   ═══════════════════════════════════════════════════════════════ */

import React, { useState, useRef, useEffect, useCallback } from 'react';
import { useMarketData } from '../stores/MarketDataStore';
import { useAuth } from '../stores/AuthStore';
import { apiService } from '../services/apiService';
import './Topbar.css';

interface TopbarProps {
  onToggleSidebar: () => void;
  sidebarExpanded: boolean;
}

export default function Topbar({ onToggleSidebar, sidebarExpanded }: TopbarProps) {
  const { state } = useMarketData();
  const { user, logout } = useAuth();

  // ── Search ──────────────────────────────────────────────
  const [searchQuery, setSearchQuery] = useState('');
  const [searchResults, setSearchResults] = useState<any[]>([]);
  const [searchOpen, setSearchOpen] = useState(false);
  const searchRef = useRef<HTMLDivElement>(null);
  const searchTimerRef = useRef<ReturnType<typeof setTimeout>>();

  const handleSearch = useCallback((query: string) => {
    setSearchQuery(query);
    if (query.length < 1) {
      setSearchResults([]);
      return;
    }
    clearTimeout(searchTimerRef.current);
    searchTimerRef.current = setTimeout(async () => {
      try {
        // Search from local stock data first
        const localResults = Object.values(state.stocks)
          .filter(s =>
            s.symbol.toLowerCase().includes(query.toLowerCase()) ||
            s.companyName.toLowerCase().includes(query.toLowerCase())
          )
          .slice(0, 8);
        setSearchResults(localResults);
        setSearchOpen(true);
      } catch (err) {
        console.error('Search error:', err);
      }
    }, 200);
  }, [state.stocks]);

  // Close search on click outside
  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (searchRef.current && !searchRef.current.contains(e.target as Node)) {
        setSearchOpen(false);
      }
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  // ── Notification badge count ────────────────────────────
  const [notifCount, setNotifCount] = useState(3);
  const [notifOpen, setNotifOpen] = useState(false);

  // ── News badge count ────────────────────────────────────
  const [newsCount, setNewsCount] = useState(5);
  const [newsOpen, setNewsOpen] = useState(false);

  // Connection indicator color
  const connColor = state.connectionState === 1 ? 'var(--color-buy)' :
    state.connectionState === 2 ? 'var(--color-warning)' : 'var(--color-sell)';
  const connLabel = state.connectionState === 1 ? 'LIVE' :
    state.connectionState === 2 ? 'RECONNECTING' : 'OFFLINE';

  return (
    <header className="topbar">
      {/* ── Left: Menu + Logo ──────────────────────────────── */}
      <div className="topbar__left">
        <button className="topbar__menu-btn" onClick={onToggleSidebar}>
          <span className={`topbar__hamburger ${sidebarExpanded ? 'open' : ''}`}>
            <span /><span /><span />
          </span>
        </button>

        <div className="topbar__logo">
          <span className="topbar__logo-icon">◆</span>
          <span className="topbar__logo-text">BdStock<span className="topbar__logo-accent">OMS</span></span>
        </div>
      </div>

      {/* ── Center: Search + Indexes + Status ──────────────── */}
      <div className="topbar__center">
        {/* Search */}
        <div className="topbar__search" ref={searchRef}>
          <div className="topbar__search-box">
            <span className="topbar__search-icon">⌕</span>
            <input
              type="text"
              className="topbar__search-input"
              placeholder="Search symbol, company... (Ctrl+K)"
              value={searchQuery}
              onChange={e => handleSearch(e.target.value)}
              onFocus={() => searchQuery && setSearchOpen(true)}
            />
            <kbd className="topbar__search-kbd">⌘K</kbd>
          </div>
          {searchOpen && searchResults.length > 0 && (
            <div className="topbar__search-dropdown glass-panel-heavy">
              {searchResults.map(s => (
                <div key={s.symbol} className="topbar__search-item">
                  <div className="topbar__search-item-left">
                    <span className="topbar__search-symbol">{s.symbol}</span>
                    <span className="topbar__search-name">{s.companyName}</span>
                  </div>
                  <div className="topbar__search-item-right">
                    <span className="topbar__search-price">{s.lastTradePrice?.toFixed(2)}</span>
                    <span className={`topbar__search-change ${s.changePercent >= 0 ? 'up' : 'down'}`}>
                      {s.changePercent >= 0 ? '+' : ''}{s.changePercent?.toFixed(2)}%
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Indexes mini display */}
        <div className="topbar__indexes">
          {state.indexes.length > 0 ? state.indexes.slice(0, 4).map(idx => (
            <div key={idx.name} className="topbar__index-item">
              <span className="topbar__index-name">{idx.name}</span>
              <span className="topbar__index-value">{idx.value?.toLocaleString(undefined, { maximumFractionDigits: 1 })}</span>
              <span className={`topbar__index-change ${idx.changePercent >= 0 ? 'up' : 'down'}`}>
                {idx.changePercent >= 0 ? '+' : ''}{idx.changePercent?.toFixed(2)}%
              </span>
            </div>
          )) : (
            <>
              <div className="topbar__index-item">
                <span className="topbar__index-name">DSEX</span>
                <span className="topbar__index-value skeleton" style={{ width: 48, height: 12 }} />
              </div>
              <div className="topbar__index-item">
                <span className="topbar__index-name">DS30</span>
                <span className="topbar__index-value skeleton" style={{ width: 48, height: 12 }} />
              </div>
            </>
          )}
        </div>

        {/* Market Status */}
        <div className="topbar__market-status">
          {state.marketStatus.length > 0 ? state.marketStatus.map(ms => (
            <div key={ms.exchange} className="topbar__status-item">
              <span className="topbar__status-exchange">{ms.exchange}</span>
              <span className={`topbar__status-badge ${ms.status === 'OPEN' ? 'open' : 'closed'}`}>
                {ms.status}
              </span>
            </div>
          )) : (
            <>
              <div className="topbar__status-item">
                <span className="topbar__status-exchange">DSE</span>
                <span className="topbar__status-badge open">OPEN</span>
              </div>
              <div className="topbar__status-item">
                <span className="topbar__status-exchange">CSE</span>
                <span className="topbar__status-badge open">OPEN</span>
              </div>
            </>
          )}
        </div>
      </div>

      {/* ── Right: Actions ─────────────────────────────────── */}
      <div className="topbar__right">
        {/* Connection status */}
        <div className="topbar__conn" title={connLabel}>
          <span className="topbar__conn-dot" style={{ background: connColor, boxShadow: `0 0 6px ${connColor}` }} />
          <span className="topbar__conn-label">{connLabel}</span>
        </div>

        {/* Notifications */}
        <button className="topbar__action-btn" onClick={() => setNotifOpen(!notifOpen)}>
          🔔
          {notifCount > 0 && <span className="topbar__badge">{notifCount}</span>}
        </button>

        {/* News */}
        <button className="topbar__action-btn" onClick={() => setNewsOpen(!newsOpen)}>
          📰
          {newsCount > 0 && <span className="topbar__badge topbar__badge--blue">{newsCount}</span>}
        </button>

        {/* Theme toggle */}
        <button className="topbar__action-btn" title="Theme">
          🎨
        </button>

        {/* Profile */}
        <div className="topbar__profile">
          <div className="topbar__avatar">
            {user?.fullName?.charAt(0) || 'A'}
          </div>
          <div className="topbar__profile-info">
            <span className="topbar__profile-name">{user?.fullName || 'Admin'}</span>
            <span className="topbar__profile-role">{user?.role || 'SuperAdmin'}</span>
          </div>
        </div>
      </div>
    </header>
  );
}
