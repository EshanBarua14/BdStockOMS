/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — AppLayout
   Master layout shell: Ticker → Topbar → Sidebar + Workspace → Bottom
   ═══════════════════════════════════════════════════════════════ */

import React, { useState, useCallback } from 'react';
import { useMarketData } from '../stores/MarketDataStore';
import { useAuth } from '../stores/AuthStore';
import PriceTicker from './PriceTicker';
import Topbar from './Topbar';
import Sidebar from './Sidebar';
import TradingWorkspace from './TradingWorkspace';
import BottomBar from './BottomBar';
import './AppLayout.css';

export default function AppLayout() {
  const { state } = useMarketData();
  const { user } = useAuth();
  const [sidebarExpanded, setSidebarExpanded] = useState(false);

  const toggleSidebar = useCallback(() => {
    setSidebarExpanded(prev => !prev);
  }, []);

  return (
    <div className="app-layout">
      {/* Ambient background blobs for depth */}
      <div className="ambient-blob ambient-blob-1" />
      <div className="ambient-blob ambient-blob-2" />
      <div className="ambient-blob ambient-blob-3" />

      {/* ── Tier 1: Price Ticker ───────────────────────────── */}
      <PriceTicker />

      {/* ── Tier 2: Topbar ─────────────────────────────────── */}
      <Topbar
        onToggleSidebar={toggleSidebar}
        sidebarExpanded={sidebarExpanded}
      />

      {/* ── Tier 3: Sidebar + Workspace ────────────────────── */}
      <div className="app-main">
        <Sidebar
          expanded={sidebarExpanded}
          onToggle={toggleSidebar}
        />
        <TradingWorkspace />
      </div>

      {/* ── Tier 4: Bottom Activity Bar ────────────────────── */}
      <BottomBar />
    </div>
  );
}
