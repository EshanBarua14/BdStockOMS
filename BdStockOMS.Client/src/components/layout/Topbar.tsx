// @ts-nocheck
// src/components/layout/Topbar.tsx
// Premium OMS Topbar v3 — Properly distributed spacing
// Left: Market Phase · Indexes | Center: Search | Right: Exchange · SignalR · Clock · Theme · User

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

function getMarketPhase(): { label: string; color: string; glow: string; dot: string } {
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
  const [lastCount, setLastCount] = useState(0)
  const [isConnected, setIsConnected] = useState(false)

  useEffect(() => {
    if (ticksArray.length > 0 && ticksArray.length !== lastCount) {
      setIsConnected(true)
      setLastCount(ticksArray.length)
    }
    const timeout = setTimeout(() => {
      if (ticksArray.length === lastCount && !loading) {
        setIsConnected(ticksArray.length > 0)
      }
    }, 10000)
    return () => clearTimeout(timeout)
  }, [ticksArray.length, loading])

  return { isConnected: isConnected || ticksArray.length > 0, loading }
}

// ── Pill component ──────────────────────────────────────────
function StatusPill({ dotColor, dotGlow, label, labelColor, animate = false, sub = '' }: {
  dotColor: string; dotGlow?: string; label: string; labelColor: string; animate?: boolean; sub?: string
}) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 5,
      padding: '4px 10px', borderRadius: 20,
      background: 'rgba(255,255,255,0.025)',
      border: '1px solid rgba(255,255,255,0.05)',
    }}>
      <span style={{
        width: 5, height: 5, borderRadius: '50%',
        background: dotColor, flexShrink: 0,
        boxShadow: dotGlow ? `0 0 6px ${dotGlow}` : 'none',
        animation: animate ? 'oms-pulse 2s ease-in-out infinite' : 'none',
      }} />
      <span style={{
        fontSize: 9, fontWeight: 700, color: labelColor,
        fontFamily: "'JetBrains Mono', monospace",
        letterSpacing: '0.06em',
      }}>{label}</span>
      {sub && (
        <span style={{
          fontSize: 8, color: 'rgba(255,255,255,0.18)',
          fontFamily: "'JetBrains Mono', monospace",
        }}>{sub}</span>
      )}
    </div>
  )
}

// ── Divider ─────────────────────────────────────────────────
function Div() {
  return <div style={{ width: 1, height: 20, background: 'rgba(255,255,255,0.05)', flexShrink: 0 }} />
}

// ── Index chip ──────────────────────────────────────────────
function IndexChip({ name, value, change }: { name: string; value: string; change: number | null }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 6,
      padding: '4px 10px', borderRadius: 6,
      background: 'rgba(255,255,255,0.02)',
      cursor: 'default',
      transition: 'background 0.15s',
    }}
      onMouseEnter={e => e.currentTarget.style.background = 'rgba(255,255,255,0.045)'}
      onMouseLeave={e => e.currentTarget.style.background = 'rgba(255,255,255,0.02)'}
    >
      <span style={{
        fontSize: 9, fontWeight: 700, color: 'rgba(255,255,255,0.25)',
        fontFamily: "'JetBrains Mono', monospace",
        letterSpacing: '0.05em',
      }}>{name}</span>
      <span style={{
        fontSize: 11, fontWeight: 600, color: 'rgba(255,255,255,0.6)',
        fontFamily: "'JetBrains Mono', monospace",
      }}>{value}</span>
      {change !== null && (
        <span style={{
          fontSize: 9, fontWeight: 700,
          fontFamily: "'JetBrains Mono', monospace",
          color: change >= 0 ? '#00e676' : '#ff1744',
        }}>
          {change >= 0 ? '▲' : '▼'}{Math.abs(change).toFixed(2)}%
        </span>
      )}
    </div>
  )
}

// ═══════════════════════════════════════════════════════════════
// MAIN TOPBAR
// ═══════════════════════════════════════════════════════════════
export function Topbar() {
  const user = useAuthStore(s => s.user)
  const { ticksArray, marketStatus } = useMarketData()
  const { isConnected, loading } = useSignalRStatus()

  // Search
  const [searchFocused, setSearchFocused] = useState(false)
  const [searchQuery, setSearchQuery] = useState('')
  const searchRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault(); searchRef.current?.focus()
      }
    }
    window.addEventListener('keydown', handler)
    return () => window.removeEventListener('keydown', handler)
  }, [])

  // BDT Clock
  const [time, setTime] = useState(new Date())
  useEffect(() => {
    const iv = setInterval(() => setTime(new Date()), 1000)
    return () => clearInterval(iv)
  }, [])

  const bdtTime = time.toLocaleTimeString('en-US', {
    timeZone: 'Asia/Dhaka', hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: true,
  })

  // Market phase
  const [phase, setPhase] = useState(getMarketPhase())
  useEffect(() => {
    const iv = setInterval(() => setPhase(getMarketPhase()), 30000)
    return () => clearInterval(iv)
  }, [])

  const displayPhase = marketStatus.isOpen
    ? { label: 'OPEN', color: '#00e676', glow: 'rgba(0,230,118,0.3)', dot: '#00e676' }
    : phase

  // Index data
  const indexData = useMemo(() => {
    const stocks = ticksArray.slice(0, 30)
    if (stocks.length === 0) {
      return [
        { name: 'DSEX', value: '—', change: null },
        { name: 'DS30', value: '—', change: null },
        { name: 'DSES', value: '—', change: null },
      ]
    }
    const avg = stocks.reduce((s, t) => s + (t.changePercent || 0), 0) / stocks.length
    return [
      { name: 'DSEX',  value: '5,432', change: avg * 0.8 },
      { name: 'DS30',  value: '1,987', change: avg * 1.2 },
      { name: 'DSES',  value: '1,245', change: avg * 0.6 },
    ]
  }, [ticksArray])

  const badgeColor = user ? roleBadgeColor(user.role) : '#64748B'
  const stockCount = ticksArray.length

  return (
    <header style={{
      height: 'var(--oms-topbar-h, 48px)',
      background: 'linear-gradient(180deg, rgba(13,19,32,0.95) 0%, rgba(8,12,20,0.90) 100%)',
      backdropFilter: 'blur(20px)', WebkitBackdropFilter: 'blur(20px)',
      borderBottom: '1px solid rgba(255,255,255,0.06)',
      display: 'flex', alignItems: 'center', justifyContent: 'space-between',
      padding: '0 16px',
      position: 'sticky', top: 0, zIndex: 50,
    }}>

      {/* ═══════════ LEFT GROUP ═══════════ */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
        {/* Market Phase */}
        <StatusPill
          dotColor={displayPhase.dot}
          dotGlow={displayPhase.glow !== 'none' ? displayPhase.glow : undefined}
          label={displayPhase.label}
          labelColor={displayPhase.color}
          animate={displayPhase.label === 'OPEN'}
        />

        <Div />

        {/* Indexes */}
        <div style={{ display: 'flex', gap: 3 }}>
          {indexData.map(idx => (
            <IndexChip key={idx.name} name={idx.name} value={idx.value} change={idx.change} />
          ))}
        </div>
      </div>

      {/* ═══════════ CENTER — Search ═══════════ */}
      <div style={{ position: 'absolute', left: '50%', transform: 'translateX(-50%)', width: 320 }}>
        <div style={{
          display: 'flex', alignItems: 'center', gap: 8,
          height: 30, padding: '0 12px',
          background: searchFocused ? 'rgba(255,255,255,0.06)' : 'rgba(255,255,255,0.025)',
          border: `1px solid ${searchFocused ? 'rgba(0,212,170,0.25)' : 'rgba(255,255,255,0.05)'}`,
          borderRadius: 8,
          transition: 'all 0.25s cubic-bezier(0.16,1,0.3,1)',
          boxShadow: searchFocused ? '0 0 20px rgba(0,212,170,0.05)' : 'none',
        }}>
          <svg width="13" height="13" viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0, opacity: 0.3 }}>
            <circle cx="11" cy="11" r="7" stroke="currentColor" strokeWidth="1.5"/>
            <path d="M16 16l4 4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
          </svg>
          <input
            ref={searchRef}
            type="text"
            placeholder="Search symbol, company..."
            value={searchQuery}
            onChange={e => setSearchQuery(e.target.value)}
            onFocus={() => setSearchFocused(true)}
            onBlur={() => setSearchFocused(false)}
            style={{
              flex: 1, background: 'transparent', border: 'none', outline: 'none',
              color: 'rgba(255,255,255,0.85)', fontSize: 12,
              fontFamily: "'Outfit', sans-serif",
            }}
          />
          <kbd style={{
            fontSize: 8, color: 'rgba(255,255,255,0.15)',
            background: 'rgba(255,255,255,0.03)',
            padding: '2px 6px', borderRadius: 4,
            border: '1px solid rgba(255,255,255,0.04)',
            fontFamily: "'JetBrains Mono', monospace",
            fontWeight: 600, flexShrink: 0,
          }}>⌘K</kbd>
        </div>
      </div>

      {/* ═══════════ RIGHT GROUP ═══════════ */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>

        {/* DSE / CSE Exchanges */}
        <div style={{ display: 'flex', gap: 4 }}>
          {['DSE', 'CSE'].map(ex => (
            <StatusPill
              key={ex}
              dotColor={displayPhase.dot}
              dotGlow={displayPhase.glow !== 'none' ? displayPhase.glow : undefined}
              label={ex}
              labelColor={displayPhase.label === 'OPEN' ? '#00e676' : 'rgba(255,255,255,0.25)'}
              animate={displayPhase.label === 'OPEN'}
            />
          ))}
        </div>

        <Div />

        {/* SignalR Connection */}
        <div
          title={isConnected ? `SignalR Connected · ${stockCount} symbols` : loading ? 'Connecting...' : 'Disconnected'}
          style={{
            display: 'flex', alignItems: 'center', gap: 5,
            padding: '4px 10px', borderRadius: 20,
            background: isConnected ? 'rgba(0,212,170,0.04)' : 'rgba(255,23,68,0.04)',
            border: `1px solid ${isConnected ? 'rgba(0,212,170,0.10)' : 'rgba(255,23,68,0.10)'}`,
          }}
        >
          <span style={{
            width: 5, height: 5, borderRadius: '50%',
            background: isConnected ? '#00D4AA' : loading ? '#ffd740' : '#ff1744',
            boxShadow: isConnected ? '0 0 6px rgba(0,212,170,0.4)' : 'none',
            animation: (isConnected || loading) ? 'oms-pulse 2s ease-in-out infinite' : 'none',
          }} />
          <span style={{
            fontSize: 9, fontWeight: 700,
            fontFamily: "'JetBrains Mono', monospace",
            letterSpacing: '0.05em',
            color: isConnected ? '#00D4AA' : loading ? '#ffd740' : '#ff1744',
          }}>
            {isConnected ? 'LIVE' : loading ? 'SYNC' : 'OFF'}
          </span>
          {isConnected && stockCount > 0 && (
            <span style={{
              fontSize: 8, color: 'rgba(255,255,255,0.18)',
              fontFamily: "'JetBrains Mono', monospace",
            }}>{stockCount}</span>
          )}
        </div>

        <Div />

        {/* BDT Clock */}
        <div style={{
          display: 'flex', alignItems: 'center', gap: 5,
          padding: '4px 10px', borderRadius: 20,
          background: 'rgba(255,255,255,0.02)',
          border: '1px solid rgba(255,255,255,0.04)',
        }}>
          <span style={{
            fontSize: 10, fontWeight: 600, color: 'rgba(255,255,255,0.4)',
            fontFamily: "'JetBrains Mono', monospace",
            letterSpacing: '0.02em',
          }}>{bdtTime}</span>
          <span style={{
            fontSize: 7, fontWeight: 700, color: 'rgba(255,255,255,0.15)',
            fontFamily: "'JetBrains Mono', monospace",
            background: 'rgba(255,255,255,0.03)',
            padding: '1px 4px', borderRadius: 3,
            letterSpacing: '0.06em',
          }}>BDT</span>
        </div>

        <Div />

        {/* Theme */}
        <ThemeMenu variant="compact" />

        <Div />

        {/* User */}
        {user && (
          <div style={{
            display: 'flex', alignItems: 'center', gap: 8,
            padding: '3px 10px 3px 4px', borderRadius: 20,
            cursor: 'default', transition: 'background 0.15s',
            background: 'rgba(255,255,255,0.02)',
            border: '1px solid rgba(255,255,255,0.04)',
          }}
            onMouseEnter={e => e.currentTarget.style.background = 'rgba(255,255,255,0.04)'}
            onMouseLeave={e => e.currentTarget.style.background = 'rgba(255,255,255,0.02)'}
          >
            <div style={{
              width: 26, height: 26, borderRadius: '50%',
              background: `linear-gradient(135deg, ${badgeColor}33, ${badgeColor}11)`,
              border: `1.5px solid ${badgeColor}44`,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: badgeColor, fontWeight: 700, fontSize: 11,
              fontFamily: "'JetBrains Mono', monospace",
            }}>
              {user.fullName.charAt(0)}
            </div>
            <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.1 }}>
              <span style={{
                fontSize: 10, fontWeight: 600, color: 'rgba(255,255,255,0.7)',
                fontFamily: "'Outfit', sans-serif",
                maxWidth: 80, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
              }}>{user.fullName}</span>
              <span style={{
                fontSize: 8, fontWeight: 700, color: badgeColor,
                fontFamily: "'JetBrains Mono', monospace",
                letterSpacing: '0.04em', opacity: 0.7,
              }}>{user.role}</span>
            </div>
          </div>
        )}
      </div>
    </header>
  )
}
