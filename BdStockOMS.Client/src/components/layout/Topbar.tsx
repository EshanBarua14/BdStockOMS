// @ts-nocheck
// src/components/layout/Topbar.tsx
// v4 — Pure flexbox, no overlap, auto-adjustable at any zoom/screen

import { useState, useRef, useEffect, useMemo } from 'react'
import { useAuthStore } from '@/store/authStore'
import { useMarketData } from '@/hooks/useMarketData'
import { ThemeMenu } from '@/components/ui/ThemeMenu'
import type { UserRole } from '@/types'

function roleBadgeColor(role: UserRole) {
  const map: Record<string, string> = {
    SuperAdmin: '#00D4AA', Admin: '#FFA500', BrokerageHouse: '#A78BFA',
    BrokerageAdmin: '#C084FC', Trader: '#00D4AA', Investor: '#38BDF8',
    ITSupport: '#94A3B8', CCD: '#FB7185',
  }
  return map[role] ?? '#64748B'
}

function getMarketPhase() {
  const now = new Date()
  const bstH = (now.getUTCHours() + 6) % 24
  const bstM = now.getUTCMinutes()
  const t = bstH * 60 + bstM
  const day = now.getUTCDay()
  const isWeekday = day !== 5 && day !== 6
  if (!isWeekday) return { label: 'CLOSED', color: 'rgba(255,255,255,0.3)', glow: 'none', dot: '#555' }
  if (t >= 570 && t < 600) return { label: 'PRE-OPEN', color: '#ffd740', glow: 'rgba(255,215,64,0.3)', dot: '#ffd740' }
  if (t >= 600 && t < 870) return { label: 'OPEN', color: '#00e676', glow: 'rgba(0,230,118,0.3)', dot: '#00e676' }
  if (t >= 870 && t < 900) return { label: 'POST-CLOSE', color: '#ff9100', glow: 'rgba(255,145,0,0.3)', dot: '#ff9100' }
  return { label: 'CLOSED', color: 'rgba(255,255,255,0.3)', glow: 'none', dot: '#555' }
}

function useSignalRStatus() {
  const { ticksArray, loading } = useMarketData()
  const [isConnected, setIsConnected] = useState(false)
  useEffect(() => { if (ticksArray.length > 0) setIsConnected(true) }, [ticksArray.length])
  return { isConnected: isConnected || ticksArray.length > 0, loading, count: ticksArray.length }
}

// ── Tiny reusable pieces ────────────────────────────────────
const Div = () => <div style={{ width: 1, height: 20, background: 'rgba(255,255,255,0.05)', flexShrink: 0 }} />

function Pill({ children, style = {} }: { children: React.ReactNode; style?: React.CSSProperties }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 5,
      padding: '4px 10px', borderRadius: 20, flexShrink: 0,
      background: 'rgba(255,255,255,0.025)',
      border: '1px solid rgba(255,255,255,0.05)',
      ...style,
    }}>{children}</div>
  )
}

function Dot({ color, glow, animate }: { color: string; glow?: string; animate?: boolean }) {
  return (
    <span style={{
      width: 5, height: 5, borderRadius: '50%', flexShrink: 0,
      background: color,
      boxShadow: glow ? `0 0 6px ${glow}` : 'none',
      animation: animate ? 'oms-pulse 2s ease-in-out infinite' : 'none',
    }} />
  )
}

function Label({ text, color, mono = true }: { text: string; color: string; mono?: boolean }) {
  return (
    <span style={{
      fontSize: 9, fontWeight: 700, color,
      fontFamily: mono ? "'JetBrains Mono', monospace" : "'Outfit', sans-serif",
      letterSpacing: '0.05em',
    }}>{text}</span>
  )
}

// ═══════════════════════════════════════════════════════════
export function Topbar() {
  const user = useAuthStore(s => s.user)
  const { ticksArray, marketStatus } = useMarketData()
  const sr = useSignalRStatus()

  const [searchFocused, setSearchFocused] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const searchRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    const h = (e: KeyboardEvent) => { if ((e.ctrlKey || e.metaKey) && e.key === 'k') { e.preventDefault(); searchRef.current?.focus() } }
    window.addEventListener('keydown', h); return () => window.removeEventListener('keydown', h)
  }, [])

  const [time, setTime] = useState(new Date())
  useEffect(() => { const iv = setInterval(() => setTime(new Date()), 1000); return () => clearInterval(iv) }, [])
  const bdtTime = time.toLocaleTimeString('en-US', { timeZone: 'Asia/Dhaka', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true })

  const [phase, setPhase] = useState(getMarketPhase())
  useEffect(() => { const iv = setInterval(() => setPhase(getMarketPhase()), 30000); return () => clearInterval(iv) }, [])
  const dp = marketStatus.isOpen ? { label: 'OPEN', color: '#00e676', glow: 'rgba(0,230,118,0.3)', dot: '#00e676' } : phase

  const indexData = useMemo(() => {
    const stocks = ticksArray.slice(0, 30)
    if (!stocks.length) return [
      { name: 'DSEX', value: '—', change: null },
      { name: 'DS30', value: '—', change: null },
      { name: 'DSES', value: '—', change: null },
    ]
    const avg = stocks.reduce((s, t) => s + (t.changePercent || 0), 0) / stocks.length
    return [
      { name: 'DSEX', value: '5,432', change: avg * 0.8 },
      { name: 'DS30', value: '1,987', change: avg * 1.2 },
      { name: 'DSES', value: '1,245', change: avg * 0.6 },
    ]
  }, [ticksArray])

  const bc = user ? roleBadgeColor(user.role) : '#64748B'

  return (
    <header style={{
      height: 'var(--oms-topbar-h, 48px)',
      background: 'linear-gradient(180deg, rgba(13,19,32,0.95), rgba(8,12,20,0.90))',
      backdropFilter: 'blur(20px)', WebkitBackdropFilter: 'blur(20px)',
      borderBottom: '1px solid rgba(255,255,255,0.06)',
      display: 'flex', alignItems: 'center',
      padding: '0 14px', gap: 8,
      position: 'sticky', top: 0, zIndex: 50,
      /* overflow removed — dropdown must not be clipped */
    }}>

      {/* ── LEFT: Phase + Indexes ── */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexShrink: 0 }}>
        <Pill>
          <Dot color={dp.dot} glow={dp.glow !== 'none' ? dp.glow : undefined} animate={dp.label === 'OPEN'} />
          <Label text={dp.label} color={dp.color} />
        </Pill>

        {indexData.map(idx => (
          <div key={idx.name} style={{
            display: 'flex', alignItems: 'center', gap: 5,
            padding: '4px 8px', borderRadius: 6, flexShrink: 0,
            background: 'rgba(255,255,255,0.02)',
            cursor: 'default',
          }}>
            <span style={{ fontSize: 9, fontWeight: 700, color: 'rgba(255,255,255,0.25)', fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.04em' }}>{idx.name}</span>
            <span style={{ fontSize: 11, fontWeight: 600, color: 'rgba(255,255,255,0.6)', fontFamily: "'JetBrains Mono', monospace" }}>{idx.value}</span>
            {idx.change !== null && (
              <span style={{ fontSize: 9, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", color: idx.change >= 0 ? '#00e676' : '#ff1744' }}>
                {idx.change >= 0 ? '▲' : '▼'}{Math.abs(idx.change).toFixed(2)}%
              </span>
            )}
          </div>
        ))}
      </div>

      {/* ── CENTER: Search (flex:1 with max-width, NOT absolute) ── */}
      <div style={{ flex: 1, display: 'flex', justifyContent: 'center', minWidth: 0, padding: '0 8px' }}>
        <div style={{
          width: '100%', maxWidth: 320,
          display: 'flex', alignItems: 'center', gap: 8,
          height: 30, padding: '0 12px',
          background: searchFocused ? 'rgba(255,255,255,0.06)' : 'rgba(255,255,255,0.025)',
          border: `1px solid ${searchFocused ? 'rgba(0,212,170,0.25)' : 'rgba(255,255,255,0.05)'}`,
          borderRadius: 8,
          transition: 'all 0.25s cubic-bezier(0.16,1,0.3,1)',
          boxShadow: searchFocused ? '0 0 16px rgba(0,212,170,0.05)' : 'none',
        }}>
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0, opacity: 0.3 }}>
            <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="1.5"/>
            <path d="M16 16l4 4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
          </svg>
          <input ref={searchRef} type="text" placeholder="Search symbol, company..."
            value={searchQuery} onChange={e => setSearchQuery(e.target.value)}
            onFocus={() => setSearchFocused(true)} onBlur={() => setSearchFocused(false)}
            style={{
              flex: 1, minWidth: 0, background: 'transparent', border: 'none', outline: 'none',
              color: 'rgba(255,255,255,0.85)', fontSize: 12, fontFamily: "'Outfit', sans-serif",
            }}
          />
          <kbd style={{
            fontSize: 8, color: 'rgba(255,255,255,0.15)', background: 'rgba(255,255,255,0.03)',
            padding: '2px 6px', borderRadius: 4, border: '1px solid rgba(255,255,255,0.04)',
            fontFamily: "'JetBrains Mono', monospace", fontWeight: 600, flexShrink: 0,
          }}>⌘K</kbd>
        </div>
      </div>

      {/* ── RIGHT: Exchanges + SignalR + Clock + Theme + User ── */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6, flexShrink: 0 }}>
        {['DSE', 'CSE'].map(ex => (
          <Pill key={ex}>
            <Dot color={dp.dot} glow={dp.glow !== 'none' ? dp.glow : undefined} />
            <Label text={ex} color={dp.label === 'OPEN' ? '#00e676' : 'rgba(255,255,255,0.25)'} />
          </Pill>
        ))}

        <Div />

        {/* SignalR */}
        <Pill style={{
          background: sr.isConnected ? 'rgba(0,212,170,0.04)' : 'rgba(255,23,68,0.04)',
          border: `1px solid ${sr.isConnected ? 'rgba(0,212,170,0.10)' : 'rgba(255,23,68,0.10)'}`,
        }}>
          <Dot color={sr.isConnected ? '#00D4AA' : sr.loading ? '#ffd740' : '#ff1744'}
               glow={sr.isConnected ? 'rgba(0,212,170,0.4)' : undefined}
               animate={sr.isConnected || sr.loading} />
          <Label text={sr.isConnected ? 'LIVE' : sr.loading ? 'SYNC' : 'OFF'}
                 color={sr.isConnected ? '#00D4AA' : sr.loading ? '#ffd740' : '#ff1744'} />
          {sr.isConnected && sr.count > 0 && (
            <span style={{ fontSize: 8, color: 'rgba(255,255,255,0.18)', fontFamily: "'JetBrains Mono', monospace" }}>{sr.count}</span>
          )}
        </Pill>

        <Div />

        {/* Clock */}
        <Pill>
          <span style={{ fontSize: 10, fontWeight: 600, color: 'rgba(255,255,255,0.4)', fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.02em' }}>{bdtTime}</span>
          <span style={{ fontSize: 7, fontWeight: 700, color: 'rgba(255,255,255,0.15)', fontFamily: "'JetBrains Mono', monospace", background: 'rgba(255,255,255,0.03)', padding: '1px 4px', borderRadius: 3, letterSpacing: '0.06em' }}>BDT</span>
        </Pill>

        <Div />
        <ThemeMenu variant="compact" />

        {/* User */}
        {user && (
          <>
            <Div />
            <Pill style={{ padding: '3px 10px 3px 4px', cursor: 'default' }}>
              <div style={{
                width: 26, height: 26, borderRadius: '50%',
                background: `linear-gradient(135deg, ${bc}33, ${bc}11)`,
                border: `1.5px solid ${bc}44`,
                display: 'flex', alignItems: 'center', justifyContent: 'center',
                color: bc, fontWeight: 700, fontSize: 11, fontFamily: "'JetBrains Mono', monospace",
              }}>{user.fullName.charAt(0)}</div>
              <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.1 }}>
                <span style={{ fontSize: 10, fontWeight: 600, color: 'rgba(255,255,255,0.7)', fontFamily: "'Outfit', sans-serif", maxWidth: 80, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{user.fullName}</span>
                <span style={{ fontSize: 8, fontWeight: 700, color: bc, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.04em', opacity: 0.7 }}>{user.role}</span>
              </div>
            </Pill>
          </>
        )}
      </div>
    </header>
  )
}
