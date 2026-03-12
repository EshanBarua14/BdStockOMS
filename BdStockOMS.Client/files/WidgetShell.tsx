/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — WidgetShell
   Universal wrapper for every trading widget with:
   - Glass panel styling
   - Header with title, exchange tabs, controls
   - Drag handle, minimize/maximize/detach/close buttons
   - Loading/error states
   - Exchange dropdown (DSE/CSE)
   ═══════════════════════════════════════════════════════════════ */

import React, { useState, ReactNode } from 'react';
import { useMarketData, Exchange } from '../stores/MarketDataStore';
import './WidgetShell.css';

interface WidgetShellProps {
  id: string;
  title: string;
  icon?: ReactNode;
  children: ReactNode;
  showExchangeTabs?: boolean;
  exchange?: Exchange;
  onExchangeChange?: (exchange: Exchange) => void;
  isLoading?: boolean;
  error?: string | null;
  headerRight?: ReactNode;
  footer?: ReactNode;
  className?: string;
  noPadding?: boolean;
  onClose?: () => void;
  onDetach?: () => void;
}

export default function WidgetShell({
  id,
  title,
  icon,
  children,
  showExchangeTabs = false,
  exchange: externalExchange,
  onExchangeChange,
  isLoading = false,
  error = null,
  headerRight,
  footer,
  className = '',
  noPadding = false,
  onClose,
  onDetach,
}: WidgetShellProps) {
  const { state, selectExchange } = useMarketData();
  const [isMinimized, setIsMinimized] = useState(false);

  const currentExchange = externalExchange ?? state.selectedExchange;

  const handleExchangeChange = (ex: Exchange) => {
    if (onExchangeChange) {
      onExchangeChange(ex);
    } else {
      selectExchange(ex);
    }
  };

  return (
    <div
      className={`widget ${isMinimized ? 'widget--minimized' : ''} ${className}`}
      data-widget-id={id}
    >
      {/* ── Header ────────────────────────────────────────── */}
      <div className="widget-header">
        <div className="widget-title">
          <span className="dot" />
          {icon && <span className="widget-icon">{icon}</span>}
          <span>{title}</span>
        </div>

        <div className="widget-controls">
          {showExchangeTabs && (
            <div className="exchange-tabs">
              {(['DSE', 'CSE', 'ALL'] as Exchange[]).map(ex => (
                <button
                  key={ex}
                  className={`exchange-tab ${currentExchange === ex ? 'active' : ''}`}
                  onClick={() => handleExchangeChange(ex)}
                >
                  {ex}
                </button>
              ))}
            </div>
          )}

          {headerRight}

          <button
            className="widget-btn"
            onClick={() => setIsMinimized(!isMinimized)}
            title={isMinimized ? 'Expand' : 'Minimize'}
          >
            {isMinimized ? '□' : '−'}
          </button>

          {onDetach && (
            <button className="widget-btn" onClick={onDetach} title="Detach">
              ⧉
            </button>
          )}

          {onClose && (
            <button className="widget-btn widget-btn--close" onClick={onClose} title="Close">
              ×
            </button>
          )}
        </div>
      </div>

      {/* ── Body ──────────────────────────────────────────── */}
      {!isMinimized && (
        <div className={`widget-body ${noPadding ? 'widget-body--no-pad' : ''}`}>
          {isLoading ? (
            <div className="widget-loading">
              <div className="widget-loading-spinner" />
              <span>Loading...</span>
            </div>
          ) : error ? (
            <div className="widget-error">
              <span className="widget-error-icon">⚠</span>
              <span>{error}</span>
            </div>
          ) : (
            children
          )}
        </div>
      )}

      {/* ── Footer ────────────────────────────────────────── */}
      {!isMinimized && footer && (
        <div className="widget-footer">{footer}</div>
      )}
    </div>
  );
}
