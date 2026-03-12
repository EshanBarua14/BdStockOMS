/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — TradingWorkspace
   Main modular widget area — responsive grid layout
   All widgets render here, connected to backend data
   ═══════════════════════════════════════════════════════════════ */

import React, { useState, useMemo } from 'react';
import { useMarketData, Exchange } from '../stores/MarketDataStore';
import WidgetShell from './common/WidgetShell';
import './TradingWorkspace.css';

// ── Inline Widget Content Components ─────────────────────────
// These will be replaced by full widget modules in later phases.
// For now they display live data from MarketDataStore.

function WatchlistWidget() {
  const { state, selectSymbol } = useMarketData();
  const [exchange, setExchange] = useState<Exchange>('DSE');

  const stocks = useMemo(() => {
    return Object.values(state.stocks)
      .filter(s => exchange === 'ALL' || s.exchange === exchange)
      .sort((a, b) => a.symbol.localeCompare(b.symbol));
  }, [state.stocks, exchange]);

  return (
    <WidgetShell
      id="watchlist"
      title="Watchlist"
      icon="☰"
      showExchangeTabs
      exchange={exchange}
      onExchangeChange={setExchange}
      noPadding
      footer={<span>{stocks.length} symbols</span>}
    >
      <div className="data-header">
        <span style={{ flex: 2 }}>Symbol</span>
        <span style={{ flex: 2, textAlign: 'right' }}>Last</span>
        <span style={{ flex: 1.5, textAlign: 'right' }}>Chg%</span>
        <span style={{ flex: 2, textAlign: 'right' }}>Volume</span>
      </div>
      {stocks.map(s => {
        const dirClass = s.changePercent > 0 ? 'text-buy' : s.changePercent < 0 ? 'text-sell' : '';
        const flash = s.priceDirection === 'up' ? 'flash-green' : s.priceDirection === 'down' ? 'flash-red' : '';
        return (
          <div
            key={s.symbol}
            className={`data-row ${flash} ${state.selectedSymbol === s.symbol ? 'selected' : ''}`}
            onClick={() => selectSymbol(s.symbol)}
          >
            <span style={{ flex: 2, fontWeight: 600, color: 'var(--text-primary)' }}>
              {s.symbol}
            </span>
            <span style={{ flex: 2, textAlign: 'right', color: 'var(--text-secondary)' }}>
              {s.lastTradePrice.toFixed(2)}
            </span>
            <span className={dirClass} style={{ flex: 1.5, textAlign: 'right', fontWeight: 600 }}>
              {s.changePercent > 0 ? '+' : ''}{s.changePercent.toFixed(2)}%
            </span>
            <span style={{ flex: 2, textAlign: 'right', color: 'var(--text-tertiary)' }}>
              {s.volume?.toLocaleString() || '—'}
            </span>
          </div>
        );
      })}
    </WidgetShell>
  );
}

function TopMoversWidget() {
  const { getTopGainers, getTopLosers } = useMarketData();
  const [tab, setTab] = useState<'gainers' | 'losers'>('gainers');
  const [exchange, setExchange] = useState<Exchange>('DSE');

  const items = tab === 'gainers' ? getTopGainers(exchange, 8) : getTopLosers(exchange, 8);

  return (
    <WidgetShell
      id="movers"
      title="Top Movers"
      icon="🔥"
      showExchangeTabs
      exchange={exchange}
      onExchangeChange={setExchange}
      noPadding
      headerRight={
        <div className="exchange-tabs" style={{ marginRight: 8 }}>
          <button
            className={`exchange-tab ${tab === 'gainers' ? 'active' : ''}`}
            onClick={() => setTab('gainers')}
            style={{ color: tab === 'gainers' ? 'var(--color-buy)' : undefined }}
          >
            Gain
          </button>
          <button
            className={`exchange-tab ${tab === 'losers' ? 'active' : ''}`}
            onClick={() => setTab('losers')}
            style={{ color: tab === 'losers' ? 'var(--color-sell)' : undefined }}
          >
            Loss
          </button>
        </div>
      }
    >
      <div className="data-header">
        <span style={{ flex: 2 }}>Symbol</span>
        <span style={{ flex: 2, textAlign: 'right' }}>Last</span>
        <span style={{ flex: 1.5, textAlign: 'right' }}>Chg%</span>
      </div>
      {items.map(s => (
        <div key={s.symbol} className="data-row">
          <span style={{ flex: 2, fontWeight: 600, color: 'var(--text-primary)' }}>{s.symbol}</span>
          <span style={{ flex: 2, textAlign: 'right', color: 'var(--text-secondary)' }}>
            {s.lastTradePrice.toFixed(2)}
          </span>
          <span
            className={s.changePercent >= 0 ? 'text-buy' : 'text-sell'}
            style={{ flex: 1.5, textAlign: 'right', fontWeight: 600 }}
          >
            {s.changePercent >= 0 ? '+' : ''}{s.changePercent.toFixed(2)}%
          </span>
        </div>
      ))}
      {items.length === 0 && (
        <div style={{ padding: 16, textAlign: 'center', color: 'var(--text-tertiary)', fontSize: 'var(--font-size-sm)' }}>
          No data available
        </div>
      )}
    </WidgetShell>
  );
}

function ChartWidget() {
  const { state } = useMarketData();
  const symbol = state.selectedSymbol;
  const stock = symbol ? state.stocks[symbol] : null;

  return (
    <WidgetShell id="chart" title={symbol ? `Chart — ${symbol}` : 'Chart'} icon="📈">
      <div className="chart-placeholder">
        {stock ? (
          <div className="chart-info">
            <div className="chart-symbol">{stock.symbol}</div>
            <div className="chart-price">
              <span className="chart-price-value">{stock.lastTradePrice.toFixed(2)}</span>
              <span className={`chart-price-change ${stock.changePercent >= 0 ? 'text-buy' : 'text-sell'}`}>
                {stock.changePercent >= 0 ? '▲' : '▼'} {Math.abs(stock.changeAmount).toFixed(2)} ({Math.abs(stock.changePercent).toFixed(2)}%)
              </span>
            </div>
            <div className="chart-meta">
              <span>O: {stock.openingPrice?.toFixed(2)}</span>
              <span>H: {stock.dayHigh?.toFixed(2)}</span>
              <span>L: {stock.dayLow?.toFixed(2)}</span>
              <span>V: {stock.volume?.toLocaleString()}</span>
            </div>
            <div className="chart-canvas">
              {/* TradingView or custom chart will go here in Phase 4 */}
              <div className="chart-placeholder-text">Chart rendering in Phase 4</div>
            </div>
          </div>
        ) : (
          <div className="chart-placeholder-text">Select a symbol to view chart</div>
        )}
      </div>
    </WidgetShell>
  );
}

function OrderConsoleWidget() {
  const { state } = useMarketData();
  const symbol = state.selectedSymbol;
  const stock = symbol ? state.stocks[symbol] : null;

  return (
    <WidgetShell id="order" title="Order Console" icon="📋">
      <div className="order-console">
        <div className="order-console__symbol">
          {stock ? (
            <>
              <span className="font-mono" style={{ fontWeight: 700, fontSize: 'var(--font-size-md)' }}>{stock.symbol}</span>
              <span className="font-mono text-muted" style={{ fontSize: 'var(--font-size-xs)' }}>
                {stock.lastTradePrice.toFixed(2)}
              </span>
            </>
          ) : (
            <span className="text-muted">No symbol selected</span>
          )}
        </div>

        <div className="order-console__tabs">
          <button className="order-tab order-tab--buy active">BUY</button>
          <button className="order-tab order-tab--sell">SELL</button>
        </div>

        <div className="order-console__fields">
          <label className="order-field">
            <span className="order-field__label">Quantity</span>
            <input type="number" className="order-field__input" placeholder="0" />
          </label>
          <label className="order-field">
            <span className="order-field__label">Price</span>
            <input
              type="number"
              className="order-field__input"
              placeholder={stock?.lastTradePrice?.toFixed(2) || '0.00'}
            />
          </label>
          <label className="order-field">
            <span className="order-field__label">Type</span>
            <select className="order-field__input">
              <option>Limit</option>
              <option>Market</option>
            </select>
          </label>
        </div>

        <button className="order-submit order-submit--buy">
          PLACE BUY ORDER
        </button>
      </div>
    </WidgetShell>
  );
}

function MarketDepthWidget() {
  const { state } = useMarketData();
  const symbol = state.selectedSymbol;

  return (
    <WidgetShell id="depth" title={`Depth${symbol ? ' — ' + symbol : ''}`} icon="📊">
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
        <span className="text-muted" style={{ fontSize: 'var(--font-size-sm)' }}>
          {symbol ? `Market depth for ${symbol}` : 'Select a symbol'}
        </span>
        <span className="text-muted" style={{ fontSize: 'var(--font-size-xs)', marginTop: 4 }}>
          Full implementation in Phase 4
        </span>
      </div>
    </WidgetShell>
  );
}

function IndexWidget() {
  const { state } = useMarketData();

  return (
    <WidgetShell id="index" title="Market Indexes" icon="📊" noPadding>
      <div className="data-header">
        <span style={{ flex: 2 }}>Index</span>
        <span style={{ flex: 2, textAlign: 'right' }}>Value</span>
        <span style={{ flex: 1.5, textAlign: 'right' }}>Chg%</span>
      </div>
      {(state.indexes.length > 0 ? state.indexes : [
        { name: 'DSEX', value: 5432.10, change: 12.5, changePercent: 0.23, volume: 0, exchange: 'DSE' },
        { name: 'DS30', value: 1987.30, change: -5.2, changePercent: -0.26, volume: 0, exchange: 'DSE' },
        { name: 'DSES', value: 1245.60, change: 3.1, changePercent: 0.25, volume: 0, exchange: 'DSE' },
        { name: 'CSE30', value: 15678.40, change: 45.2, changePercent: 0.29, volume: 0, exchange: 'CSE' },
      ]).map(idx => (
        <div key={idx.name} className="data-row">
          <span style={{ flex: 2, fontWeight: 600, color: 'var(--text-primary)' }}>{idx.name}</span>
          <span style={{ flex: 2, textAlign: 'right', color: 'var(--text-secondary)' }}>
            {idx.value?.toLocaleString(undefined, { maximumFractionDigits: 1 })}
          </span>
          <span
            className={idx.changePercent >= 0 ? 'text-buy' : 'text-sell'}
            style={{ flex: 1.5, textAlign: 'right', fontWeight: 600 }}
          >
            {idx.changePercent >= 0 ? '+' : ''}{idx.changePercent?.toFixed(2)}%
          </span>
        </div>
      ))}
    </WidgetShell>
  );
}

function PortfolioWidget() {
  return (
    <WidgetShell id="portfolio" title="Portfolio" icon="💼">
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
        <span className="text-muted" style={{ fontSize: 'var(--font-size-sm)' }}>Portfolio summary</span>
        <span className="text-muted" style={{ fontSize: 'var(--font-size-xs)', marginTop: 4 }}>Full implementation in Phase 5</span>
      </div>
    </WidgetShell>
  );
}

function AISentimentWidget() {
  const { state } = useMarketData();
  const stocks = Object.values(state.stocks).slice(0, 6);

  return (
    <WidgetShell id="ai" title="AI Sentiment" icon="🤖" noPadding>
      <div className="data-header">
        <span style={{ flex: 2 }}>Symbol</span>
        <span style={{ flex: 2, textAlign: 'center' }}>Sentiment</span>
        <span style={{ flex: 1.5, textAlign: 'right' }}>Conf</span>
      </div>
      {stocks.map(s => {
        // Simulated AI sentiment based on change%
        const sentiment = s.changePercent > 1 ? 'Bullish' : s.changePercent < -1 ? 'Bearish' : 'Neutral';
        const sentClass = sentiment === 'Bullish' ? 'badge-buy' : sentiment === 'Bearish' ? 'badge-sell' : 'badge-neutral';
        const confidence = Math.min(95, Math.max(45, 60 + Math.abs(s.changePercent) * 10));
        return (
          <div key={s.symbol} className="data-row">
            <span style={{ flex: 2, fontWeight: 600, color: 'var(--text-primary)' }}>{s.symbol}</span>
            <span style={{ flex: 2, textAlign: 'center' }}>
              <span className={`badge ${sentClass}`}>{sentiment}</span>
            </span>
            <span style={{ flex: 1.5, textAlign: 'right', color: 'var(--text-secondary)' }}>
              {confidence.toFixed(0)}%
            </span>
          </div>
        );
      })}
    </WidgetShell>
  );
}

function HeatmapWidget() {
  return (
    <WidgetShell id="heatmap" title="Sector Heatmap" icon="🗺">
      <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', height: '100%' }}>
        <span className="text-muted" style={{ fontSize: 'var(--font-size-sm)' }}>Sector heatmap visualization</span>
        <span className="text-muted" style={{ fontSize: 'var(--font-size-xs)', marginTop: 4 }}>Full implementation in Phase 6</span>
      </div>
    </WidgetShell>
  );
}

// ── Main Workspace Grid ──────────────────────────────────────
export default function TradingWorkspace() {
  return (
    <div className="workspace">
      <div className="workspace__grid">
        {/* Row 1: Watchlist + Chart + Order Console */}
        <div className="workspace__cell workspace__cell--watchlist">
          <WatchlistWidget />
        </div>
        <div className="workspace__cell workspace__cell--chart">
          <ChartWidget />
        </div>
        <div className="workspace__cell workspace__cell--order">
          <OrderConsoleWidget />
        </div>

        {/* Row 2: Indexes + Top Movers + Market Depth + AI Sentiment */}
        <div className="workspace__cell workspace__cell--index">
          <IndexWidget />
        </div>
        <div className="workspace__cell workspace__cell--movers">
          <TopMoversWidget />
        </div>
        <div className="workspace__cell workspace__cell--depth">
          <MarketDepthWidget />
        </div>
        <div className="workspace__cell workspace__cell--ai">
          <AISentimentWidget />
        </div>

        {/* Row 3: Portfolio + Heatmap */}
        <div className="workspace__cell workspace__cell--portfolio">
          <PortfolioWidget />
        </div>
        <div className="workspace__cell workspace__cell--heatmap">
          <HeatmapWidget />
        </div>
      </div>
    </div>
  );
}
