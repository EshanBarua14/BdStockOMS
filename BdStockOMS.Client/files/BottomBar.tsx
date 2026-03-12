/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — BottomBar
   Activity, last update, connection info
   ═══════════════════════════════════════════════════════════════ */

import React, { useState, useEffect } from 'react';
import { useMarketData } from '../stores/MarketDataStore';
import './BottomBar.css';

export default function BottomBar() {
  const { state } = useMarketData();
  const [time, setTime] = useState(new Date());

  useEffect(() => {
    const interval = setInterval(() => setTime(new Date()), 1000);
    return () => clearInterval(interval);
  }, []);

  const stockCount = Object.keys(state.stocks).length;
  const connStatus = state.connectionState === 1 ? 'Connected' :
    state.connectionState === 2 ? 'Reconnecting...' : 'Disconnected';
  const connColor = state.connectionState === 1 ? 'var(--color-buy)' :
    state.connectionState === 2 ? 'var(--color-warning)' : 'var(--color-sell)';

  return (
    <footer className="bottombar">
      <div className="bottombar__left">
        <span className="bottombar__item">
          <span className="bottombar__dot" style={{ background: connColor }} />
          {connStatus}
        </span>
        <span className="bottombar__divider">|</span>
        <span className="bottombar__item">
          {stockCount} symbols streaming
        </span>
        {state.lastUpdate && (
          <>
            <span className="bottombar__divider">|</span>
            <span className="bottombar__item">
              Last: {new Date(state.lastUpdate).toLocaleTimeString()}
            </span>
          </>
        )}
      </div>

      <div className="bottombar__center">
        <span className="bottombar__item">
          Exchange: {state.selectedExchange}
        </span>
      </div>

      <div className="bottombar__right">
        <span className="bottombar__item">
          {time.toLocaleDateString('en-BD', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' })}
        </span>
        <span className="bottombar__divider">|</span>
        <span className="bottombar__item bottombar__clock">
          {time.toLocaleTimeString('en-BD', { hour12: false })}
        </span>
      </div>
    </footer>
  );
}
