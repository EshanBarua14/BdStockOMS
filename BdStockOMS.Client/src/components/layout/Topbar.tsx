// @ts-nocheck
import { useState, useEffect } from 'react'
import { useLocation } from 'react-router-dom'
import { ThemePanel } from '@/components/ui/ThemePanel'
import { useAuthStore } from '@/store/authStore'

const ROUTE_LABELS: Record<string, string> = {
  '/dashboard':        'Overview',
  '/orders':           'Orders',
  '/portfolio':        'Portfolio',
  '/market':           'Market Watch',
  '/watchlist':        'Watchlist',
  '/reports':          'Reports',
  '/admin/users':      'User Management',
  '/admin/compliance': 'Compliance',
  '/admin/settings':   'System Settings',
}

/* ── Live Clock ──────────────────────────────────────────────────────────── */
function Clock() {
  const [time, setTime] = useState(new Date())
  useEffect(() => {
    const id = setInterval(() => setTime(new Date()), 1000)
    return () => clearInterval(id)
  }, [])

  const timeStr = time.toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit', second: '2-digit', timeZone: 'Asia/Dhaka' })
  const dateStr = time.toLocaleDateString('en-GB', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'Asia/Dhaka' })

  return (
    <div style={{ textAlign: 'right', lineHeight: 1.2 }}>
      <div style={{ fontFamily: 'var(--font-mono)', fontSize: 12, fontWeight: 700, color: 'var(--text-primary)', letterSpacing: '.04em' }}>
        {timeStr}
      </div>
      <div style={{ fontSize: 9.5, color: 'var(--text-tertiary)', letterSpacing: '.03em' }}>
        {dateStr} BDT
      </div>
    </div>
  )
}

/* ── Market Status ───────────────────────────────────────────────────────── */
function MarketStatus() {
  const h = new Date().getHours()
  const isOpen = h >= 10 && h < 14
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 6,
      padding: '4px 11px', borderRadius: 99,
      background: isOpen ? 'var(--bull-muted)' : 'var(--neutral-muted)',
      border: `1px solid ${isOpen ? 'var(--bull-border)' : 'rgba(100,116,139,.22)'}`,
      fontSize: 11, fontWeight: 600, letterSpacing: '.02em', whiteSpace: 'nowrap',
      color: isOpen ? 'var(--bull-strong)' : 'var(--neutral-base)',
    }}>
      <span style={{
        width: 6, height: 6, borderRadius: '50%',
        background: isOpen ? 'var(--bull-strong)' : 'var(--neutral-base)',
        animation: isOpen ? 'pulse 2s ease-in-out infinite' : undefined,
        flexShrink: 0,
      }} />
      {isOpen ? 'DSEX Open' : 'Market Closed'}
    </div>
  )
}

/* ── Search bar ──────────────────────────────────────────────────────────── */
function SearchBar() {
  const [focused, setFocused] = useState(false)
  return (
    <div className="input-group" style={{ width: 220 }}>
      <span className="input-group-icon">
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
          <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
        </svg>
      </span>
      <input
        className="input"
        placeholder="Search symbol, order… "
        style={{ height: 32, fontSize: 12 }}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
      />
      {!focused && (
        <kbd style={{
          position: 'absolute', right: 10,
          fontSize: 9.5, color: 'var(--text-tertiary)',
          background: 'var(--bg-hover)', border: '1px solid var(--border-default)',
          borderRadius: 4, padding: '1px 5px', pointerEvents: 'none',
          fontFamily: 'var(--font-mono)',
        }}>⌘K</kbd>
      )}
    </div>
  )
}

/* ── Notifications ───────────────────────────────────────────────────────── */
const NOTIFS = [
  { type: 'bull', msg: 'BATBC order filled — 50 × ৳716.00', time: '2m ago',  unread: true },
  { type: 'warn', msg: 'Session expiring in 15 minutes',   time: '13m ago', unread: true },
  { type: 'info', msg: 'Market opens in 45 minutes',        time: '1h ago',  unread: false },
]
const notifColors: Record<string, string> = { bull: 'var(--bull-strong)', warn: 'var(--warn-base)', info: 'var(--info-base)' }

function NotificationBell() {
  const [open, setOpen] = useState(false)
  const unread = NOTIFS.filter(n => n.unread).length

  return (
    <div style={{ position: 'relative' }}>
      <button className="btn btn-ghost btn-icon btn-sm" onClick={() => setOpen(o => !o)}
        style={{ position: 'relative', color: open ? 'var(--accent-400)' : 'var(--text-secondary)' }}>
        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
          <path d="M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9"/>
          <path d="M13.73 21a2 2 0 01-3.46 0"/>
        </svg>
        {unread > 0 && (
          <span style={{
            position: 'absolute', top: 3, right: 3,
            width: 8, height: 8, borderRadius: '50%',
            background: 'var(--accent-500)',
            border: '1.5px solid var(--bg-surface)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
            fontSize: 7, color: '#fff', fontWeight: 700,
          }} />
        )}
      </button>

      {open && (
        <div className="animate-slide-down" style={{
          position: 'absolute', right: 0, top: 'calc(100% + 10px)',
          width: 320, zIndex: 200,
          background: 'var(--bg-overlay)',
          border: '1px solid var(--border-strong)',
          borderRadius: 'var(--r-xl)', boxShadow: 'var(--shadow-xl)', overflow: 'hidden',
        }}>
          <div style={{
            padding: '12px 16px', borderBottom: '1px solid var(--border-subtle)',
            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          }}>
            <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)' }}>Notifications</span>
            <button className="btn btn-ghost btn-sm" style={{ fontSize: 11, height: 24, padding: '0 8px', color: 'var(--accent-400)' }}>
              Mark all read
            </button>
          </div>
          {NOTIFS.map((n, i) => (
            <div key={i} style={{
              display: 'flex', gap: 12, padding: '11px 16px',
              borderBottom: '1px solid var(--border-subtle)',
              background: n.unread ? 'color-mix(in srgb, var(--accent-600) 4%, transparent)' : 'transparent',
              transition: 'background var(--dur-fast)',
              cursor: 'pointer',
            }}
            onMouseEnter={e => { (e.currentTarget as HTMLDivElement).style.background = 'var(--bg-hover)' }}
            onMouseLeave={e => { (e.currentTarget as HTMLDivElement).style.background = n.unread ? 'color-mix(in srgb, var(--accent-600) 4%, transparent)' : 'transparent' }}>
              <div style={{
                width: 7, height: 7, borderRadius: '50%', marginTop: 5, flexShrink: 0,
                background: notifColors[n.type] ?? 'var(--neutral-base)',
                boxShadow: `0 0 6px ${notifColors[n.type]}60`,
              }} />
              <div style={{ flex: 1 }}>
                <div style={{ fontSize: 12.5, color: 'var(--text-primary)', lineHeight: 1.4 }}>{n.msg}</div>
                <div style={{ fontSize: 10.5, color: 'var(--text-tertiary)', marginTop: 3 }}>{n.time}</div>
              </div>
              {n.unread && (
                <div style={{ width: 6, height: 6, borderRadius: '50%', background: 'var(--accent-500)', flexShrink: 0, marginTop: 5 }} />
              )}
            </div>
          ))}
          <div style={{ padding: '10px 16px' }}>
            <button className="btn btn-ghost btn-sm" style={{ width: '100%', fontSize: 12 }}>View all notifications</button>
          </div>
        </div>
      )}
    </div>
  )
}

/* ── Main Topbar ─────────────────────────────────────────────────────────── */
export function Topbar() {
  const { pathname } = useLocation()
  const user = useAuthStore(s => s.user)
  const label = ROUTE_LABELS[pathname] ?? (pathname.split('/').pop() ?? 'Page')

  return (
    <header style={{
      height: 52, display: 'flex', alignItems: 'center',
      justifyContent: 'space-between', gap: 16,
      padding: '0 20px',
      background: 'var(--bg-surface)',
      borderBottom: '1px solid var(--border-subtle)',
      flexShrink: 0,
      transition: 'background var(--dur-slow)',
    }}>
      {/* Left: page title */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
        <div>
          <h1 style={{
            fontFamily: 'var(--font-display)',
            fontWeight: 700, fontSize: 15,
            color: 'var(--text-primary)',
            letterSpacing: '-0.02em', lineHeight: 1,
          }}>
            {label}
          </h1>
        </div>
      </div>

      {/* Center: search */}
      <div style={{ flex: 1, maxWidth: 260, display: 'flex', justifyContent: 'center' }}>
        <SearchBar />
      </div>

      {/* Right: status + tools */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
        <MarketStatus />
        <div style={{ width: 1, height: 18, background: 'var(--border-default)', margin: '0 4px' }} />
        <Clock />
        <div style={{ width: 1, height: 18, background: 'var(--border-default)', margin: '0 4px' }} />
        <ThemePanel />
        <NotificationBell />
        {/* Avatar */}
        <div style={{
          width: 30, height: 30, borderRadius: '50%',
          background: 'color-mix(in srgb, var(--accent-600) 18%, transparent)',
          border: '1.5px solid color-mix(in srgb, var(--accent-600) 45%, transparent)',
          display: 'flex', alignItems: 'center', justifyContent: 'center',
          fontSize: 12, fontWeight: 700, color: 'var(--accent-300)',
          marginLeft: 2, cursor: 'default', flexShrink: 0,
        }} title={user?.email ?? ''}>
          {user?.email?.[0]?.toUpperCase() ?? '?'}
        </div>
      </div>
    </header>
  )
}
