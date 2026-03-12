// @ts-nocheck
// src/components/dashboard/PriceTicker.tsx
// Day 52 — Full-width price scroll ticker under topbar
// Real SignalR data when connected, mock fallback, fully customizable

import { useState, useEffect, useRef, useMemo } from 'react'
import { useMarketData } from '../../hooks/useMarketData'
import { useThemeStore } from '../../store/themeStore'

// ─── Mock data fallback ───────────────────────────────────────
const MOCK_STOCKS = [
  { tradingCode:'GP',stockName:'Grameenphone',lastPrice:350.40,change:5.20,changePercent:1.51,volume:245000 },
  { tradingCode:'BATBC',stockName:'BAT Bangladesh',lastPrice:520.00,change:-3.80,changePercent:-0.73,volume:180000 },
  { tradingCode:'SQURPHARMA',stockName:'Square Pharma',lastPrice:198.50,change:2.10,changePercent:1.07,volume:320000 },
  { tradingCode:'BEXIMCO',stockName:'Beximco Ltd',lastPrice:142.30,change:-1.90,changePercent:-1.32,volume:510000 },
  { tradingCode:'ROBI',stockName:'Robi Axiata',lastPrice:45.80,change:0.60,changePercent:1.33,volume:890000 },
  { tradingCode:'BRAC',stockName:'BRAC Bank',lastPrice:38.20,change:0.40,changePercent:1.06,volume:620000 },
  { tradingCode:'ICB',stockName:'ICB',lastPrice:95.00,change:-0.50,changePercent:-0.52,volume:150000 },
  { tradingCode:'RENATA',stockName:'Renata Ltd',lastPrice:1320.00,change:15.00,changePercent:1.15,volume:42000 },
  { tradingCode:'MARICO',stockName:'Marico Bangladesh',lastPrice:2180.00,change:-25.00,changePercent:-1.13,volume:18000 },
  { tradingCode:'LHBL',stockName:'LafargeHolcim BD',lastPrice:52.40,change:0.80,changePercent:1.55,volume:750000 },
  { tradingCode:'Olympic',stockName:'Olympic Industries',lastPrice:168.50,change:3.20,changePercent:1.94,volume:95000 },
  { tradingCode:'UPGDCL',stockName:'United Power',lastPrice:245.00,change:-2.00,changePercent:-0.81,volume:110000 },
  { tradingCode:'EBL',stockName:'Eastern Bank',lastPrice:28.50,change:0.30,changePercent:1.06,volume:430000 },
  { tradingCode:'DUTCHBANGL',stockName:'Dutch-Bangla Bank',lastPrice:58.90,change:1.10,changePercent:1.90,volume:380000 },
  { tradingCode:'WALTONHIL',stockName:'Walton Hi-Tech',lastPrice:1450.00,change:-18.00,changePercent:-1.23,volume:28000 },
  { tradingCode:'SUMITPOWER',stockName:'Summit Power',lastPrice:34.60,change:0.20,changePercent:0.58,volume:560000 },
]

interface TickerSettings {
  speed: number        // px per second
  direction: 'ltr' | 'rtl'
  showVolume: boolean
  paused: boolean
  exchange: 'all' | 'dse' | 'cse'
}

const DEFAULT_SETTINGS: TickerSettings = {
  speed: 50, direction: 'rtl', showVolume: false, paused: false, exchange: 'all',
}

function fmt(n: number | undefined): string {
  if (n == null || isNaN(n)) return '—'
  if (n >= 1000) return n.toLocaleString('en-BD', { minimumFractionDigits: 2, maximumFractionDigits: 2 })
  return n.toFixed(2)
}

function fmtVol(n: number | undefined): string {
  if (!n) return ''
  if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M'
  if (n >= 1_000) return (n / 1_000).toFixed(0) + 'K'
  return String(n)
}

export function PriceTicker() {
  const { ticksArray, marketStatus } = useMarketData()
  const tickerEnabled = useThemeStore(s => s.tickerEnabled)
  const [settings, setSettings] = useState<TickerSettings>(DEFAULT_SETTINGS)
  const [showSettings, setShowSettings] = useState(false)
  const [hovered, setHovered] = useState(false)

  const trackRef = useRef<HTMLDivElement>(null)
  const animRef = useRef<number>(0)
  const posRef = useRef(0)
  const prevPrices = useRef<Map<string, number>>(new Map())
  const [flashes, setFlashes] = useState<Map<string, 'up' | 'down'>>(new Map())
  const settingsRef = useRef<HTMLDivElement>(null)

  // Use real data or mock fallback
  const stocks = useMemo(() => {
    const data = ticksArray.length > 0 ? ticksArray : MOCK_STOCKS
    return data
      .filter(t => t.tradingCode)
      .sort((a, b) => (b.volume ?? 0) - (a.volume ?? 0))
      .slice(0, 40)
  }, [ticksArray])

  // Flash detection
  useEffect(() => {
    let changed = false
    const newFlashes = new Map(flashes)
    stocks.forEach(t => {
      const prev = prevPrices.current.get(t.tradingCode)
      if (prev !== undefined && prev !== t.lastPrice) {
        newFlashes.set(t.tradingCode, t.lastPrice > prev ? 'up' : 'down')
        changed = true
        setTimeout(() => setFlashes(f => { const n = new Map(f); n.delete(t.tradingCode); return n }), 700)
      }
      prevPrices.current.set(t.tradingCode, t.lastPrice)
    })
    if (changed) setFlashes(newFlashes)
  }, [stocks])

  // Scroll animation
  useEffect(() => {
    if (!trackRef.current || stocks.length === 0) return
    let lastTime = performance.now()

    const animate = (now: number) => {
      const dt = now - lastTime
      lastTime = now
      if (!settings.paused && !hovered && trackRef.current) {
        const delta = (settings.speed * dt) / 1000
        posRef.current += settings.direction === 'rtl' ? -delta : delta
        const halfWidth = trackRef.current.scrollWidth / 2
        if (settings.direction === 'rtl' && Math.abs(posRef.current) >= halfWidth) posRef.current = 0
        if (settings.direction === 'ltr' && posRef.current >= halfWidth) posRef.current = 0
        trackRef.current.style.transform = `translateX(${posRef.current}px)`
      }
      animRef.current = requestAnimationFrame(animate)
    }
    animRef.current = requestAnimationFrame(animate)
    return () => cancelAnimationFrame(animRef.current)
  }, [stocks.length, settings.speed, settings.direction, settings.paused, hovered])

  // Close settings on outside click
  useEffect(() => {
    if (!showSettings) return
    const h = (e: MouseEvent) => {
      if (settingsRef.current && !settingsRef.current.contains(e.target as Node)) setShowSettings(false)
    }
    document.addEventListener('mousedown', h)
    return () => document.removeEventListener('mousedown', h)
  }, [showSettings])

  if (!tickerEnabled) return null

  const items = [...stocks, ...stocks] // duplicate for seamless loop
  const isLive = ticksArray.length > 0

  return (
    <div style={{
      height: 28, flexShrink: 0, position: 'relative',
      background: 'var(--t-panel)',
      borderBottom: '1px solid var(--t-border)',
      display: 'flex', alignItems: 'center', overflow: 'hidden',
    }}>
      {/* Live/Mock indicator */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 4,
        padding: '0 8px', borderRight: '1px solid var(--t-border)',
        flexShrink: 0, height: '100%',
      }}>
        <span style={{
          width: 5, height: 5, borderRadius: '50%',
          background: isLive ? 'var(--t-buy)' : 'var(--t-text3)',
          animation: isLive ? 'oms-pulse 2s ease-in-out infinite' : 'none',
        }} />
        <span style={{
          fontSize: 8, fontWeight: 700, color: isLive ? 'var(--t-buy)' : 'var(--t-text3)',
          fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.08em',
        }}>{isLive ? 'LIVE' : 'DEMO'}</span>
      </div>

      {/* Scrolling area */}
      <div
        style={{ flex: 1, overflow: 'hidden', height: '100%', position: 'relative' }}
        onMouseEnter={() => setHovered(true)}
        onMouseLeave={() => setHovered(false)}
      >
        {/* Fade edges */}
        <div style={{ position: 'absolute', left: 0, top: 0, bottom: 0, width: 20, zIndex: 2, pointerEvents: 'none', background: 'linear-gradient(to right, var(--t-panel), transparent)' }} />
        <div style={{ position: 'absolute', right: 0, top: 0, bottom: 0, width: 20, zIndex: 2, pointerEvents: 'none', background: 'linear-gradient(to left, var(--t-panel), transparent)' }} />

        <div ref={trackRef} style={{
          display: 'flex', alignItems: 'center', height: '100%',
          whiteSpace: 'nowrap', willChange: 'transform',
        }}>
          {items.map((tick, i) => {
            const flash = flashes.get(tick.tradingCode)
            const isUp = (tick.changePercent ?? tick.change ?? 0) >= 0
            const flashBg = flash === 'up' ? 'rgba(0,230,118,0.12)' : flash === 'down' ? 'rgba(255,23,68,0.12)' : 'transparent'

            return (
              <div key={`${tick.tradingCode}-${i}`} style={{
                display: 'flex', alignItems: 'center', gap: 6,
                padding: '0 12px', height: '100%',
                borderRight: '1px solid var(--t-border)',
                background: flashBg, transition: 'background 0.3s',
                cursor: 'default',
              }} title={tick.stockName ?? tick.tradingCode}>
                <span style={{
                  fontSize: 10, fontWeight: 700, color: 'var(--t-text2)',
                  fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.04em',
                }}>{tick.tradingCode}</span>

                <span style={{
                  fontSize: 10, fontWeight: 600,
                  fontFamily: "'JetBrains Mono', monospace",
                  color: flash ? (flash === 'up' ? 'var(--t-buy)' : 'var(--t-sell)') : 'var(--t-text1)',
                  transition: 'color 0.3s',
                }}>{fmt(tick.lastPrice)}</span>

                <span style={{
                  fontSize: 9, fontWeight: 700,
                  fontFamily: "'JetBrains Mono', monospace",
                  color: isUp ? 'var(--t-buy)' : 'var(--t-sell)',
                }}>
                  {isUp ? '▲' : '▼'}{Math.abs(tick.changePercent ?? 0).toFixed(2)}%
                </span>

                {settings.showVolume && tick.volume > 0 && (
                  <span style={{
                    fontSize: 8, color: 'var(--t-text3)',
                    fontFamily: "'JetBrains Mono', monospace",
                  }}>{fmtVol(tick.volume)}</span>
                )}
              </div>
            )
          })}
        </div>
      </div>

      {/* Controls */}
      <div style={{
        display: 'flex', alignItems: 'center', gap: 2,
        padding: '0 6px', borderLeft: '1px solid var(--t-border)',
        flexShrink: 0, height: '100%',
      }}>
        {/* Pause */}
        <button onClick={() => setSettings(s => ({ ...s, paused: !s.paused }))}
          title={settings.paused ? 'Resume' : 'Pause'}
          style={{
            width: 22, height: 22, borderRadius: 4, display: 'flex',
            alignItems: 'center', justifyContent: 'center', border: 'none',
            background: settings.paused ? 'var(--t-hover)' : 'transparent',
            color: settings.paused ? 'var(--t-accent)' : 'var(--t-text3)',
            cursor: 'pointer', fontSize: 10, transition: 'all 0.12s',
          }}>{settings.paused ? '▶' : '⏸'}</button>

        {/* Settings gear */}
        <div style={{ position: 'relative' }} ref={settingsRef}>
          <button onClick={() => setShowSettings(v => !v)}
            title="Ticker settings"
            style={{
              width: 22, height: 22, borderRadius: 4, display: 'flex',
              alignItems: 'center', justifyContent: 'center', border: 'none',
              background: showSettings ? 'var(--t-hover)' : 'transparent',
              color: showSettings ? 'var(--t-accent)' : 'var(--t-text3)',
              cursor: 'pointer', fontSize: 10, transition: 'all 0.12s',
            }}>⚙</button>

          {showSettings && (
            <div style={{
              position: 'absolute', right: 0, top: 26, zIndex: 300,
              width: 200, background: 'var(--t-elevated)',
              border: '1px solid var(--t-border)', borderRadius: 10,
              padding: 10, boxShadow: '0 12px 32px rgba(0,0,0,0.5)',
            }}>
              <div style={{ fontSize: 9, fontWeight: 700, color: 'var(--t-text3)', letterSpacing: '0.08em', marginBottom: 8, fontFamily: "'JetBrains Mono', monospace" }}>TICKER SETTINGS</div>

              {/* Speed */}
              <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <span style={{ fontSize: 10, color: 'var(--t-text2)' }}>Speed</span>
                <input type="range" min={15} max={120} value={settings.speed}
                  onChange={e => setSettings(s => ({ ...s, speed: +e.target.value }))}
                  style={{ width: 80, accentColor: 'var(--t-accent)' }} />
              </label>

              {/* Direction */}
              <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6 }}>
                <span style={{ fontSize: 10, color: 'var(--t-text2)' }}>Direction</span>
                <div style={{ display: 'flex', gap: 2 }}>
                  {(['rtl', 'ltr'] as const).map(d => (
                    <button key={d} onClick={() => setSettings(s => ({ ...s, direction: d }))}
                      style={{
                        padding: '2px 8px', fontSize: 9, borderRadius: 4,
                        background: settings.direction === d ? 'var(--t-accent)' : 'var(--t-hover)',
                        color: settings.direction === d ? '#000' : 'var(--t-text3)',
                        border: 'none', cursor: 'pointer', fontFamily: "'JetBrains Mono', monospace",
                      }}>{d === 'rtl' ? '← R→L' : '→ L→R'}</button>
                  ))}
                </div>
              </label>

              {/* Show volume */}
              <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 6, cursor: 'pointer' }}>
                <span style={{ fontSize: 10, color: 'var(--t-text2)' }}>Show Volume</span>
                <input type="checkbox" checked={settings.showVolume}
                  onChange={e => setSettings(s => ({ ...s, showVolume: e.target.checked }))}
                  style={{ accentColor: 'var(--t-accent)' }} />
              </label>

              {/* Exchange filter */}
              <label style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                <span style={{ fontSize: 10, color: 'var(--t-text2)' }}>Exchange</span>
                <div style={{ display: 'flex', gap: 2 }}>
                  {(['all', 'dse', 'cse'] as const).map(ex => (
                    <button key={ex} onClick={() => setSettings(s => ({ ...s, exchange: ex }))}
                      style={{
                        padding: '2px 6px', fontSize: 9, borderRadius: 4,
                        background: settings.exchange === ex ? 'var(--t-accent)' : 'var(--t-hover)',
                        color: settings.exchange === ex ? '#000' : 'var(--t-text3)',
                        border: 'none', cursor: 'pointer', fontFamily: "'JetBrains Mono', monospace",
                        textTransform: 'uppercase',
                      }}>{ex}</button>
                  ))}
                </div>
              </label>
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
