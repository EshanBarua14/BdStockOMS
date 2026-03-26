import { TopbarIconBtn } from "./TopbarIconBtn";
import { NotificationsPanel } from "./NotificationsPanel";
import { SettingsPanel } from "./SettingsPanel";
// @ts-nocheck
import { useState, useRef, useEffect, useMemo } from 'react'
import { AppSettingsBtn } from "./AppSettingsBtn";
import { useAuthStore } from '@/store/authStore'
import { useMarketData } from '@/hooks/useMarketData'
import { ThemeMenu } from '@/components/ui/ThemeMenu'

// ── Bangladesh Standard Market Sessions ─────────────────────
// DSE & CSE follow BSEC-defined sessions (Sun–Thu, BST UTC+6):
//   Pre-Opening:  09:30 – 10:00 (order entry, no matching)
//   Opening:      10:00 – 10:01 (opening auction)
//   Continuous:   10:01 – 14:00 (regular trading)
//   Closing:      14:00 – 14:01 (closing auction)
//   Post-Closing: 14:01 – 14:30 (trade reporting)
//   Closed:       everything else
function getBDMarketSession(): { label: string; color: string; glow: string; dot: string } {
  const now = new Date()
  const utcDay = now.getUTCDay()
  // BD weekends: Friday(5) & Saturday(6)
  const isFriSat = utcDay === 5 || utcDay === 6
  if (isFriSat) return { label: 'CLOSED', color: '#666', glow: 'none', dot: '#555' }

  const bstH = (now.getUTCHours() + 6) % 24
  const bstM = now.getUTCMinutes()
  const t = bstH * 60 + bstM // minutes since midnight BST

  if (t >= 570 && t < 600)  return { label: 'PRE-OPENING',  color: '#ffd740', glow: 'rgba(255,215,64,0.3)', dot: '#ffd740' }
  if (t >= 600 && t < 601)  return { label: 'OPENING',      color: '#00e676', glow: 'rgba(0,230,118,0.3)', dot: '#00e676' }
  if (t >= 601 && t < 840)  return { label: 'CONTINUOUS',   color: '#00e676', glow: 'rgba(0,230,118,0.3)', dot: '#00e676' }
  if (t >= 840 && t < 841)  return { label: 'CLOSING',      color: '#ff9100', glow: 'rgba(255,145,0,0.3)', dot: '#ff9100' }
  if (t >= 841 && t < 870)  return { label: 'POST-CLOSING', color: '#ff9100', glow: 'rgba(255,145,0,0.3)', dot: '#ff9100' }
  return { label: 'CLOSED', color: '#666', glow: 'none', dot: '#555' }
}

function useSignalRStatus() {
  const { ticksArray, loading } = useMarketData()
  const [ok, setOk] = useState(false)
  useEffect(() => { if (ticksArray.length > 0) setOk(true) }, [ticksArray.length])
  return { isConnected: ok || ticksArray.length > 0, loading, count: ticksArray.length }
}

const Div = () => <div style={{ width: 1, height: 20, background: 'var(--t-border)', flexShrink: 0 }} />

function Pill({ children, style = {} }: { children: React.ReactNode; style?: React.CSSProperties }) {
  return <div style={{
    display: 'flex', alignItems: 'center', gap: 5,
    padding: '4px 10px', borderRadius: 20, flexShrink: 0,
    background: 'var(--t-hover)', border: '1px solid var(--t-border)', ...style,
  }}>{children}</div>
}

export function Topbar() {
  const user = useAuthStore(s => s.user)
  const { ticksArray, marketStatus } = useMarketData()
  const sr = useSignalRStatus()

  const [searchFocused, setSearchFocused] = useState(false)
  const [showNotifs, setShowNotifs] = useState(false)
  const [showSettings, setShowSettings] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const searchRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    const h = (e: KeyboardEvent) => { if ((e.ctrlKey || e.metaKey) && e.key === 'k') { e.preventDefault(); searchRef.current?.focus() } }
    window.addEventListener('keydown', h); return () => window.removeEventListener('keydown', h)
  }, [])

  const [time, setTime] = useState(new Date())
  useEffect(() => { const iv = setInterval(() => setTime(new Date()), 1000); return () => clearInterval(iv) }, [])
  const bdtTime = time.toLocaleTimeString('en-US', { timeZone: 'Asia/Dhaka', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true })

  const [session, setSession] = useState(getBDMarketSession())
  useEffect(() => { const iv = setInterval(() => setSession(getBDMarketSession()), 15000); return () => clearInterval(iv) }, [])

  // Use hook market status if it says open, otherwise use session calculator
  const phase = marketStatus.isOpen
    ? { label: 'CONTINUOUS', color: '#00e676', glow: 'rgba(0,230,118,0.3)', dot: '#00e676' }
    : session

  const isTrading = phase.label === 'CONTINUOUS' || phase.label === 'OPENING'

  // DSE + CSE Indexes
  const indexes = useMemo(() => {
    const stocks = ticksArray
    if (!stocks.length) return [
      { name: 'DSEX', value: '—', chg: null, ex: 'DSE' },
      { name: 'DS30', value: '—', chg: null, ex: 'DSE' },
      { name: 'DSES', value: '—', chg: null, ex: 'DSE' },
      { name: 'CASPI', value: '—', chg: null, ex: 'CSE' },
      { name: 'CSE30', value: '—', chg: null, ex: 'CSE' },
    ]
    const avg = stocks.reduce((s, t) => s + (t.changePercent || 0), 0) / stocks.length
    return [
      { name: 'DSEX',  value: '5,432', chg: +(avg * 0.8).toFixed(2), ex: 'DSE' },
      { name: 'DS30',  value: '1,987', chg: +(avg * 1.2).toFixed(2), ex: 'DSE' },
      { name: 'DSES',  value: '1,245', chg: +(avg * 0.6).toFixed(2), ex: 'DSE' },
      { name: 'CASPI', value: '18,420', chg: +(avg * 0.9).toFixed(2), ex: 'CSE' },
      { name: 'CSE30', value: '9,841',  chg: +(avg * 1.1).toFixed(2), ex: 'CSE' },
    ]
  }, [ticksArray])

  return (
    <header style={{
      height: 'var(--oms-topbar-h, 48px)',
      background: 'var(--t-surface)',
      borderBottom: '1px solid var(--t-border)',
      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      padding: '0 14px',
      position: 'sticky', top: 0, zIndex: 50,
    }}>

      {/* ── LEFT: Session + Indexes ── */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexShrink: 0 }}>
        <Pill>
          <span style={{
            width: 5, height: 5, borderRadius: '50%', background: phase.dot, flexShrink: 0,
            boxShadow: phase.glow !== 'none' ? `0 0 6px ${phase.glow}` : 'none',
            animation: isTrading ? 'oms-pulse 2s ease-in-out infinite' : 'none',
          }} />
          <span style={{ fontSize: 9, fontWeight: 700, color: phase.color, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.05em' }}>
            {phase.label}
          </span>
        </Pill>

        {/* DSE indexes */}
        {indexes.filter(i => i.ex === 'DSE').map(idx => (
          <div key={idx.name} style={{
            display: 'flex', alignItems: 'center', gap: 5,
            padding: '4px 8px', borderRadius: 6, flexShrink: 0,
            background: 'var(--t-hover)',
          }}>
            <span style={{ fontSize: 9, fontWeight: 700, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>{idx.name}</span>
            <span style={{ fontSize: 11, fontWeight: 600, color: 'var(--t-text2)', fontFamily: "'JetBrains Mono', monospace" }}>{idx.value}</span>
            {idx.chg !== null && (
              <span style={{ fontSize: 9, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: idx.chg >= 0 ? 'var(--t-buy)' : 'var(--t-sell)' }}>
                {idx.chg >= 0 ? '▲' : '▼'}{Math.abs(idx.chg).toFixed(2)}%
              </span>
            )}
          </div>
        ))}

        <Div />

        {/* CSE indexes */}
        {indexes.filter(i => i.ex === 'CSE').map(idx => (
          <div key={idx.name} style={{
            display: 'flex', alignItems: 'center', gap: 5,
            padding: '4px 8px', borderRadius: 6, flexShrink: 0,
            background: 'var(--t-hover)',
          }}>
            <span style={{ fontSize: 9, fontWeight: 700, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>{idx.name}</span>
            <span style={{ fontSize: 11, fontWeight: 600, color: 'var(--t-text2)', fontFamily: "'JetBrains Mono', monospace" }}>{idx.value}</span>
            {idx.chg !== null && (
              <span style={{ fontSize: 9, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: idx.chg >= 0 ? 'var(--t-buy)' : 'var(--t-sell)' }}>
                {idx.chg >= 0 ? '▲' : '▼'}{Math.abs(idx.chg).toFixed(2)}%
              </span>
            )}
          </div>
        ))}
      </div>

      {/* ── CENTER: Search ── */}
      <div style={{ display: 'none' }}>
        <div style={{
          width: '100%', maxWidth: 300,
          display: 'flex', alignItems: 'center', gap: 8,
          height: 30, padding: '0 12px',
          background: searchFocused ? 'var(--t-elevated)' : 'var(--t-hover)',
          border: `1px solid ${searchFocused ? 'var(--t-accent)' : 'var(--t-border)'}`,
          borderRadius: 8, transition: 'all 0.2s',
        }}>
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0, color: 'var(--t-text3)' }}>
            <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="1.5"/>
            <path d="M16 16l4 4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
          </svg>
          <input ref={searchRef} type="text" placeholder="Search symbol..."
            value={searchQuery} onChange={e => setSearchQuery(e.target.value)}
            onFocus={() => setSearchFocused(true)} onBlur={() => setSearchFocused(false)}
            style={{ flex: 1, minWidth: 0, background: 'transparent', border: 'none', outline: 'none', color: 'var(--t-text1)', fontSize: 12 }}
          />
          <kbd style={{ fontSize: 8, color: 'var(--t-text3)', background: 'var(--t-hover)', padding: '2px 6px', borderRadius: 4, border: '1px solid var(--t-border)', fontFamily: "'JetBrains Mono', monospace", fontWeight: 600, flexShrink: 0 }}>⌘K</kbd>
        </div>
      </div>

      {/* ── RIGHT: Exchanges + SignalR + Clock + Theme + User ── */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexShrink: 0 }}>
        {['DSE', 'CSE'].map(ex => (
          <Pill key={ex}>
            <span style={{ width: 5, height: 5, borderRadius: '50%', background: phase.dot, boxShadow: phase.glow !== 'none' ? `0 0 4px ${phase.glow}` : 'none' }} />
            <span style={{ fontSize: 9, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: isTrading ? 'var(--t-buy)' : 'var(--t-text3)' }}>{ex}</span>
          </Pill>
        ))}

        <Div />

        <Pill style={{ background: sr.isConnected ? 'transparent' : 'transparent' }}>
          <span style={{
            width: 5, height: 5, borderRadius: '50%',
            background: sr.isConnected ? 'var(--t-accent)' : sr.loading ? '#ffd740' : 'var(--t-sell)',
            animation: sr.isConnected ? 'oms-pulse 2s ease-in-out infinite' : 'none',
          }} />
          <span style={{ fontSize: 9, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: sr.isConnected ? 'var(--t-accent)' : sr.loading ? '#ffd740' : 'var(--t-sell)' }}>
            {sr.isConnected ? 'LIVE' : sr.loading ? 'SYNC' : 'OFF'}
          </span>
          {sr.isConnected && sr.count > 0 && <span style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace" }}>{sr.count}</span>}
        </Pill>

        <Div />

        <Pill>
          <span style={{ fontSize: 10, fontWeight: 600, color: 'var(--t-text2)', fontFamily: "'JetBrains Mono', monospace" }}>{bdtTime}</span>
          <span style={{ fontSize: 7, fontWeight: 700, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace", background: 'var(--t-hover)', padding: '1px 4px', borderRadius: 3 }}>BDT</span>
        </Pill>

        <Div />
        <Div />

        {/* ── News icon ── */}
        <TopbarIconBtn icon="📰" title="News" count={0} onClick={() => {}} />

        {/* ── Notifications ── */}
        <TopbarIconBtn icon="🔔" title="Notifications" count={3} onClick={() => {}} />

        {/* ── App Settings ── */}
        <TopbarIconBtn icon="⚙" title="Settings" count={0} onClick={() => { setShowSettings(v => !v); setShowNotifs(false) }} />

        <Div />
        <ThemeMenu variant="compact" />

        {user && (
          <>
            <Div />
            <Pill style={{ padding: '3px 10px 3px 4px', cursor: 'default' }}>
              <div style={{
                width: 26, height: 26, borderRadius: '50%',
                background: 'var(--t-hover)', border: '1px solid var(--t-border)',
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                color: 'var(--t-accent)', fontWeight: 700, fontSize: 11, fontFamily: "'JetBrains Mono', monospace",
              }}>{user.fullName.charAt(0)}</div>
              <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.1 }}>
                <span style={{ fontSize: 10, fontWeight: 600, color: 'var(--t-text1)', maxWidth: 80, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{user.fullName}</span>
                <span style={{ fontSize: 8, fontWeight: 700, color: 'var(--t-accent)', fontFamily: "'JetBrains Mono', monospace", opacity: 0.7 }}>{user.role}</span>
              </div>
            </Pill>
          </>
        )}
      </div>
          {showNotifs   && <NotificationsPanel onClose={() => setShowNotifs(false)} />}
      {showSettings && <SettingsPanel onClose={() => setShowSettings(false)} />}
    </header>
  )
}
