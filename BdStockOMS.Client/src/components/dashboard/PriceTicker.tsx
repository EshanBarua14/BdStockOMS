// @ts-nocheck
// src/components/dashboard/PriceTicker.tsx — Day 56
// Full settings: exchange filter, dual DSE+CSE ticker, speed, direction, sort, persist

import { useState, useEffect, useRef, useMemo, useCallback } from 'react'
import { useMarketData } from '../../hooks/useMarketData'
import { useThemeStore } from '../../store/themeStore'

const mono = "'JetBrains Mono', monospace"
const SETTINGS_KEY = 'bd_oms_ticker_v2'

const DEFAULT: TickerSettings = {
  enabled: true, speed: 50, direction: 'rtl',
  exchange: 'Both', dualTicker: true,
  sortBy: 'ltp', showVolume: false, showChange: true,
}

interface TickerSettings {
  enabled: boolean
  speed: number
  direction: 'ltr' | 'rtl'
  exchange: 'DSE' | 'CSE' | 'Both'
  dualTicker: boolean
  sortBy: 'ltp' | 'abc' | 'change' | 'volume'
  showVolume: boolean
  showChange: boolean
}

function loadSettings(): TickerSettings {
  try { return { ...DEFAULT, ...JSON.parse(localStorage.getItem(SETTINGS_KEY) ?? '{}') } } catch { return DEFAULT }
}

function saveSettings(s: TickerSettings) {
  try { localStorage.setItem(SETTINGS_KEY, JSON.stringify(s)) } catch {}
}

function fmt(n: number): string {
  if (!n && n !== 0) return '—'
  return n >= 1000 ? n.toLocaleString('en-BD', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : n.toFixed(2)
}
function fmtVol(n: number): string {
  if (!n) return ''
  if (n >= 1e6) return (n/1e6).toFixed(1)+'M'
  if (n >= 1e3) return (n/1e3).toFixed(0)+'K'
  return String(n)
}

function sortStocks(stocks: any[], sortBy: string) {
  const s = [...stocks]
  if (sortBy === 'abc')    return s.sort((a,b) => (a.tradingCode??'').localeCompare(b.tradingCode??''))
  if (sortBy === 'change') return s.sort((a,b) => (b.changePercent??0)-(a.changePercent??0))
  if (sortBy === 'volume') return s.sort((a,b) => (b.volume??0)-(a.volume??0))
  return s.sort((a,b) => (b.lastPrice??b.lastTradePrice??0)-(a.lastPrice??a.lastTradePrice??0))
}

function TickerTrack({ stocks, speed, direction, showVolume, showChange, label }: any) {
  const trackRef = useRef<HTMLDivElement>(null)
  const posRef   = useRef(0)
  const lastRef  = useRef(0)
  const [hovered, setHovered] = useState(false)
  const doubled  = useMemo(() => [...stocks, ...stocks], [stocks])

  useEffect(() => {
    if (!trackRef.current || stocks.length === 0) return
    let raf: number
    const step = (ts: number) => {
      const dt = Math.min(ts - (lastRef.current || ts), 100)
      lastRef.current = ts
      if (!hovered && trackRef.current) {
        const halfW = trackRef.current.scrollWidth / 2
        const delta = (speed * dt) / 1000
        posRef.current += direction === 'rtl' ? -delta : delta
        if (direction === 'rtl' && posRef.current <= -halfW) posRef.current = 0
        if (direction === 'ltr' && posRef.current >= halfW)  posRef.current = 0
        trackRef.current.style.transform = `translateX(${posRef.current}px)`
      }
      raf = requestAnimationFrame(step)
    }
    raf = requestAnimationFrame(step)
    return () => cancelAnimationFrame(raf)
  }, [stocks.length, speed, direction, hovered])

  if (stocks.length === 0) return null

  return (
    <div style={{ flex: 1, overflow: 'hidden', display: 'flex', alignItems: 'center', position: 'relative', minWidth: 0 }}
      onMouseEnter={() => setHovered(true)} onMouseLeave={() => setHovered(false)}>
      {label && (
        <div style={{ flexShrink: 0, padding: '0 8px', fontSize: 8, fontWeight: 800, fontFamily: mono,
          color: label === 'DSE' ? '#60a5fa' : '#a78bfa',
          borderRight: '1px solid var(--t-border)', height: '100%', display: 'flex', alignItems: 'center' }}>
          {label}
        </div>
      )}
      <div style={{ flex: 1, overflow: 'hidden' }}>
        <div ref={trackRef} style={{ display: 'flex', alignItems: 'center', gap: 0, willChange: 'transform', whiteSpace: 'nowrap' }}>
          {doubled.map((tick, i) => {
            const chg = tick.changePercent ?? tick.change ?? 0
            const up  = chg >= 0
            const ltp = tick.lastPrice ?? tick.lastTradePrice ?? 0
            return (
              <div key={i} style={{ display: 'inline-flex', alignItems: 'center', gap: 5, padding: '0 12px',
                borderRight: '1px solid var(--t-border)', height: '100%', cursor: 'pointer' }}
                onClick={() => {}} title={tick.companyName ?? tick.stockName}>
                <span style={{ fontSize: 10, fontWeight: 700, color: 'var(--t-text1)', fontFamily: mono }}>{tick.tradingCode}</span>
                <span style={{ fontSize: 10, fontWeight: 600, color: 'var(--t-text1)', fontFamily: mono }}>৳{fmt(ltp)}</span>
                {showChange && (
                  <span style={{ fontSize: 9, fontWeight: 700, color: up ? 'var(--t-buy)' : 'var(--t-sell)', fontFamily: mono }}>
                    {up ? '▲' : '▼'}{Math.abs(chg).toFixed(2)}%
                  </span>
                )}
                {showVolume && tick.volume > 0 && (
                  <span style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: mono }}>{fmtVol(tick.volume)}</span>
                )}
              </div>
            )
          })}
        </div>
      </div>
    </div>
  )
}

function SettingsPanel({ s, update, onClose }: any) {
  return (
    <>
      <div onClick={onClose} style={{ position: 'fixed', inset: 0, zIndex: 998 }} />
      <div style={{
        position: 'fixed', top: 40, right: 12, zIndex: 999,
        background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
        borderRadius: 10, padding: 14, width: 260,
        boxShadow: '0 12px 40px rgba(0,0,0,0.5)', fontFamily: mono,
      }}>
        <div style={{ fontSize: 10, fontWeight: 800, color: 'var(--t-text1)', marginBottom: 12 }}>TICKER SETTINGS</div>

        {/* Enable/disable */}
        <Row label="Show Ticker">
          <Toggle val={s.enabled} set={v => update('enabled', v)} />
        </Row>

        {/* Exchange */}
        <Row label="Exchange">
          <Seg opts={['DSE','CSE','Both']} val={s.exchange} set={v => update('exchange', v)} />
        </Row>

        {/* Dual ticker */}
        {s.exchange === 'Both' && (
          <Row label="Dual Row (DSE + CSE)">
            <Toggle val={s.dualTicker} set={v => update('dualTicker', v)} />
          </Row>
        )}

        {/* Sort by */}
        <Row label="Sort By">
          <Seg opts={['ltp','abc','change','volume']} val={s.sortBy} set={v => update('sortBy', v)} />
        </Row>

        {/* Direction */}
        <Row label="Direction">
          <Seg opts={['rtl','ltr']} val={s.direction} set={v => update('direction', v)}
            labels={['← RTL','LTR →']} />
        </Row>

        {/* Speed */}
        <Row label={`Speed: ${s.speed}px/s`}>
          <input type="range" min={10} max={200} value={s.speed}
            onChange={e => update('speed', +e.target.value)}
            style={{ width: '100%', accentColor: 'var(--t-accent)' }} />
        </Row>

        {/* Show volume */}
        <Row label="Show Volume">
          <Toggle val={s.showVolume} set={v => update('showVolume', v)} />
        </Row>

        {/* Show change % */}
        <Row label="Show Change %">
          <Toggle val={s.showChange} set={v => update('showChange', v)} />
        </Row>

        <button onClick={onClose} style={{ width: '100%', marginTop: 10, padding: '6px', fontSize: 10, borderRadius: 6, border: '1px solid var(--t-border)', background: 'var(--t-hover)', color: 'var(--t-text2)', cursor: 'pointer', fontFamily: mono }}>
          Close
        </button>
      </div>
    </>
  )
}

function Row({ label, children }: any) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 10, gap: 8 }}>
      <span style={{ fontSize: 9, color: 'var(--t-text3)', fontWeight: 600, flexShrink: 0 }}>{label}</span>
      {children}
    </div>
  )
}

function Toggle({ val, set }: any) {
  return (
    <div onClick={() => set(!val)} style={{
      width: 32, height: 16, borderRadius: 8, cursor: 'pointer', position: 'relative',
      background: val ? 'var(--t-accent)' : 'var(--t-border)', transition: 'background 0.2s',
    }}>
      <div style={{ position: 'absolute', top: 2, left: val ? 18 : 2, width: 12, height: 12, borderRadius: '50%', background: '#fff', transition: 'left 0.2s' }} />
    </div>
  )
}

function Seg({ opts, val, set, labels }: any) {
  return (
    <div style={{ display: 'flex', borderRadius: 5, overflow: 'hidden', border: '1px solid var(--t-border)', flexShrink: 0 }}>
      {opts.map((o: string, i: number) => (
        <button key={o} onClick={() => set(o)} style={{
          padding: '3px 7px', border: 'none', cursor: 'pointer', fontSize: 9, fontWeight: 700, fontFamily: mono,
          background: val === o ? 'var(--t-accent)' : 'var(--t-hover)',
          color: val === o ? '#000' : 'var(--t-text3)', transition: 'all 0.1s',
        }}>{labels ? labels[i] : o}</button>
      ))}
    </div>
  )
}

export function PriceTicker() {
  const { ticksArray } = useMarketData()
  const tickerEnabled = useThemeStore(s => s.tickerEnabled)
  const [settings, setSettings] = useState<TickerSettings>(loadSettings)

  // Re-read settings when changed from app settings panel
  useEffect(() => {
    const h = () => setSettings(loadSettings())
    window.addEventListener('ticker-settings-changed', h)
    return () => window.removeEventListener('ticker-settings-changed', h)
  }, [])
  const [showSettings, setShowSettings] = useState(false)

  const update = useCallback((key: string, val: any) => {
    setSettings(s => {
      const n = { ...s, [key]: val }
      saveSettings(n)
      return n
    })
  }, [])


const DEMO_STOCKS = [
  { tradingCode:'GP',        lastTradePrice:380.50,  change:2.30,  changePercent:0.61,  volume:1823400, exchange:'DSE' },
  { tradingCode:'BATBC',     lastTradePrice:615.92,  change:-4.10, changePercent:-0.66, volume:432100,  exchange:'DSE' },
  { tradingCode:'BERGERPBL', lastTradePrice:1131.95, change:11.20, changePercent:1.00,  volume:98200,   exchange:'DSE' },
  { tradingCode:'BRACBANK',  lastTradePrice:48.30,   change:0.80,  changePercent:1.68,  volume:3241000, exchange:'DSE' },
  { tradingCode:'DUTCHBANGL',lastTradePrice:182.40,  change:-1.60, changePercent:-0.87, volume:654300,  exchange:'DSE' },
  { tradingCode:'SQURPHARMA',lastTradePrice:242.10,  change:3.40,  changePercent:1.42,  volume:876500,  exchange:'DSE' },
  { tradingCode:'ISLAMIBANK',lastTradePrice:35.60,   change:0.40,  changePercent:1.14,  volume:4123000, exchange:'DSE' },
  { tradingCode:'RENATA',    lastTradePrice:1243.00, change:-8.50, changePercent:-0.68, volume:76400,   exchange:'DSE' },
  { tradingCode:'MARICO',    lastTradePrice:98.70,   change:1.20,  changePercent:1.23,  volume:234500,  exchange:'CSE' },
  { tradingCode:'CITYBANK',  lastTradePrice:28.40,   change:-0.30, changePercent:-1.05, volume:2341000, exchange:'CSE' },
  { tradingCode:'NBL',       lastTradePrice:14.20,   change:0.10,  changePercent:0.71,  volume:5432000, exchange:'CSE' },
  { tradingCode:'BXPHARMA',  lastTradePrice:67.80,   change:2.10,  changePercent:3.19,  volume:432100,  exchange:'CSE' },
]

  const dseStocks = useMemo(() => {
    const src = ticksArray.length > 0 ? ticksArray : DEMO_STOCKS
    return sortStocks(src.filter(t => (t.exchange ?? '').toUpperCase() === 'DSE'), settings.sortBy)
  }, [ticksArray, settings.sortBy])

  const cseStocks = useMemo(() => {
    const src = ticksArray.length > 0 ? ticksArray : DEMO_STOCKS
    return sortStocks(src.filter(t => (t.exchange ?? '').toUpperCase() === 'CSE'), settings.sortBy)
  }, [ticksArray, settings.sortBy])

  const allStocks = useMemo(() => sortStocks(ticksArray.length > 0 ? ticksArray : DEMO_STOCKS, settings.sortBy), [ticksArray, settings.sortBy])

  if (!settings.enabled || !tickerEnabled) return (
    <div style={{ height: 0, overflow: 'visible', position: 'relative', zIndex: 10 }}>
      <button onClick={() => { update('enabled', true); if (!tickerEnabled) useThemeStore.getState().toggleTicker() }}
        style={{ position: 'absolute', top: 2, right: 48, fontSize: 8, padding: '2px 7px', borderRadius: 4,
          background: 'var(--t-hover)', border: '1px solid var(--t-border)', cursor: 'pointer',
          color: 'var(--t-text3)', fontFamily: "'JetBrains Mono',monospace" }}>
        Show Ticker
      </button>
    </div>
  )

  const isDual = settings.exchange === 'Both'
  const tickerH = isDual ? 54 : 28

  const dseVisible = settings.exchange === 'DSE' || settings.exchange === 'Both'
  const cseVisible = settings.exchange === 'CSE' || settings.exchange === 'Both'
  const singleStocks = settings.exchange === 'DSE' ? dseStocks : settings.exchange === 'CSE' ? cseStocks : allStocks

  return (
    <div style={{
      height: tickerH, flexShrink: 0,
      background: 'var(--t-surface)',
      borderBottom: '1px solid var(--t-border)',
      display: 'flex', flexDirection: 'column',
      overflow: 'hidden', position: 'relative',
    }}>
      {isDual ? (
        <>
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', borderBottom: '1px solid var(--t-border)', overflow: 'hidden' }}>
            <TickerTrack stocks={dseStocks} speed={settings.speed} direction={settings.direction}
              showVolume={settings.showVolume} showChange={settings.showChange} label="DSE" />
          </div>
          <div style={{ flex: 1, display: 'flex', alignItems: 'center', overflow: 'hidden' }}>
            <TickerTrack stocks={cseStocks} speed={settings.speed} direction={settings.direction}
              showVolume={settings.showVolume} showChange={settings.showChange} label="CSE" />
          </div>
        </>
      ) : (
        <div style={{ height: '100%', display: 'flex', alignItems: 'center', overflow: 'hidden' }}>
          <TickerTrack stocks={singleStocks} speed={settings.speed} direction={settings.direction}
            showVolume={settings.showVolume} showChange={settings.showChange}
            label={settings.exchange === 'Both' ? null : settings.exchange} />
        </div>
      )}

      {/* Settings gear */}
      <button onClick={() => setShowSettings(s => !s)} style={{
        position: 'absolute', right: 4, top: '50%', transform: 'translateY(-50%)',
        background: showSettings ? 'var(--t-hover)' : 'transparent',
        border: `1px solid ${showSettings ? 'var(--t-border)' : 'transparent'}`,
        borderRadius: 4, cursor: 'pointer', color: 'var(--t-text3)', fontSize: 11,
        padding: '2px 5px', zIndex: 10, lineHeight: 1,
      }} title="Ticker settings">⚙</button>

      {showSettings && <SettingsPanel s={settings} update={update} onClose={() => setShowSettings(false)} />}
    </div>
  )
}
