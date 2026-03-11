import React from 'react'
import { NavLink } from 'react-router-dom'
import { useThemeStore } from '@/store/themeStore'
import { useAuthStore } from '@/store/authStore'
import { Logo } from '@/components/ui/Logo'

/* ── Icons ───────────────────────────────────────────────────────────────── */
const I = {
  Overview:  () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><rect x="3" y="3" width="7" height="7" rx="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5"/></svg>,
  Orders:    () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2"/><rect x="9" y="3" width="6" height="4" rx="1"/><line x1="9" y1="12" x2="15" y2="12"/><line x1="9" y1="16" x2="12" y2="16"/></svg>,
  Portfolio: () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><polyline points="22 7 13.5 15.5 8.5 10.5 2 17"/><polyline points="16 7 22 7 22 13"/></svg>,
  Market:    () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M3 3v18h18"/><path d="M18.5 8l-5.5 5.5-3-3L5 16"/></svg>,
  Watchlist: () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><polygon points="12 2 15.09 8.26 22 9.27 17 14.14 18.18 21.02 12 17.77 5.82 21.02 7 14.14 2 9.27 8.91 8.26 12 2"/></svg>,
  Reports:   () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M14 2H6a2 2 0 00-2 2v16a2 2 0 002 2h12a2 2 0 002-2V8z"/><polyline points="14 2 14 8 20 8"/><line x1="16" y1="13" x2="8" y2="13"/><line x1="16" y1="17" x2="8" y2="17"/></svg>,
  Users:     () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M23 21v-2a4 4 0 00-3-3.87"/><path d="M16 3.13a4 4 0 010 7.75"/></svg>,
  Shield:    () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>,
  Settings:  () => <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z"/></svg>,
  Logout:    () => <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"><path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>,
  Expand:    () => <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M9 18l6-6-6-6"/></svg>,
  Collapse:  () => <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"><path d="M15 18l-6-6 6-6"/></svg>,
}

const PRIMARY_NAV = [
  { label: 'Overview',  path: '/dashboard',  Icon: I.Overview  },
  { label: 'Orders',    path: '/orders',     Icon: I.Orders,   badge: 3 },
  { label: 'Portfolio', path: '/portfolio',  Icon: I.Portfolio },
  { label: 'Market',    path: '/market',     Icon: I.Market    },
  { label: 'Watchlist', path: '/watchlist',  Icon: I.Watchlist },
  { label: 'Reports',   path: '/reports',    Icon: I.Reports   },
]
const ADMIN_NAV = [
  { label: 'Users',      path: '/admin/users',      Icon: I.Users   },
  { label: 'Compliance', path: '/admin/compliance', Icon: I.Shield  },
  { label: 'Settings',   path: '/admin/settings',   Icon: I.Settings },
]

const ROLE_COLORS: Record<string, string> = {
  SuperAdmin: '#3B82F6', Admin: '#6366F1', BrokerageAdmin: '#7C3AED',
  Broker: '#10B981', Investor: '#F59E0B',
}

function NavRow({ to, Icon, label, badge, collapsed }: {
  to: string; Icon: () => React.ReactElement; label: string; badge?: number; collapsed: boolean
}) {
  return (
    <NavLink to={to} end={to === '/dashboard'} title={collapsed ? label : undefined}
      style={({ isActive }) => ({
        display: 'flex', alignItems: 'center',
        gap: 9,
        padding: collapsed ? `var(--sidebar-item-py, 8px) 0` : `var(--sidebar-item-py, 8px) 10px`,
        borderRadius: 'var(--r-md)',
        border: '1px solid',
        borderColor: isActive ? 'color-mix(in srgb, var(--accent-500) 30%, transparent)' : 'transparent',
        background: isActive ? 'color-mix(in srgb, var(--accent-600) 10%, transparent)' : 'transparent',
        color: isActive ? 'var(--accent-300)' : 'var(--text-secondary)',
        fontSize: 13, fontWeight: isActive ? 600 : 400,
        textDecoration: 'none',
        justifyContent: collapsed ? 'center' : undefined,
        position: 'relative',
        transition: 'all var(--dur-fast) var(--ease-smooth)',
      })}>
      {({ isActive }) => (<>
        <span style={{ flexShrink: 0, display: 'flex', color: isActive ? 'var(--accent-400)' : 'inherit', transition: 'color var(--dur-fast)' }}>
          <Icon />
        </span>
        {!collapsed && <span style={{ flex: 1, lineHeight: 1 }}>{label}</span>}
        {!collapsed && badge ? (
          <span style={{
            background: 'var(--accent-600)', color: '#fff',
            borderRadius: 99, fontSize: 9.5, fontWeight: 700,
            padding: '2px 6px', lineHeight: '15px',
            minWidth: 18, textAlign: 'center',
          }}>{badge}</span>
        ) : null}
        {collapsed && badge ? (
          <span style={{
            position: 'absolute', top: 4, right: 4,
            width: 6, height: 6, borderRadius: '50%',
            background: 'var(--accent-500)',
            border: '1.5px solid var(--bg-surface)',
          }} />
        ) : null}
      </>)}
    </NavLink>
  )
}

export function Sidebar() {
  const { sidebarCollapsed, toggleSidebar } = useThemeStore()
  const { user, logout } = useAuthStore()
  const isAdmin = user && ['Admin', 'SuperAdmin'].includes(user.role)
  const W = sidebarCollapsed ? 56 : 224

  return (
    <aside style={{
      width: W, minWidth: W, maxWidth: W,
      height: '100%',
      display: 'flex', flexDirection: 'column',
      background: 'var(--bg-surface)',
      borderRight: '1px solid var(--border-subtle)',
      transition: 'width 280ms var(--ease-out-expo), min-width 280ms var(--ease-out-expo), max-width 280ms var(--ease-out-expo)',
      overflow: 'hidden', userSelect: 'none', flexShrink: 0,
    }}>

      {/* Logo header */}
      <div style={{
        height: 52, display: 'flex', alignItems: 'center',
        padding: sidebarCollapsed ? '0' : '0 14px',
        justifyContent: sidebarCollapsed ? 'center' : undefined,
        borderBottom: '1px solid var(--border-subtle)',
        gap: 10, flexShrink: 0,
      }}>
        <Logo size={28} animated={false} />
        {!sidebarCollapsed && (
          <>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontFamily: 'var(--font-display)', fontWeight: 800, fontSize: 15, color: 'var(--text-primary)', letterSpacing: '-0.03em', lineHeight: 1 }}>
                BD<span style={{ color: 'var(--accent-400)' }}>OMS</span>
              </div>
              <div style={{ fontSize: 9, color: 'var(--text-tertiary)', letterSpacing: '.06em', textTransform: 'uppercase', marginTop: 2 }}>
                Order Management
              </div>
            </div>
            <button onClick={toggleSidebar} className="btn btn-ghost btn-icon btn-sm" title="Collapse sidebar" style={{ flexShrink: 0, color: 'var(--text-tertiary)', marginRight: -4 }}>
              <I.Collapse />
            </button>
          </>
        )}
      </div>

      {/* Nav */}
      <nav style={{ flex: 1, overflowY: 'auto', padding: '8px 6px', display: 'flex', flexDirection: 'column', gap: 1 }} className="no-scrollbar">
        {PRIMARY_NAV.map(n => <NavRow key={n.path} to={n.path} Icon={n.Icon} label={n.label} badge={(n as any).badge} collapsed={sidebarCollapsed} />)}

        {isAdmin && (
          <>
            <hr className="sep" style={{ margin: '8px 4px' }} />
            {!sidebarCollapsed && (
              <span style={{
                fontSize: 9, fontWeight: 700, letterSpacing: '.10em', textTransform: 'uppercase',
                color: 'var(--text-tertiary)', padding: '2px 6px 4px', display: 'block',
              }}>Administration</span>
            )}
            {ADMIN_NAV.map(n => <NavRow key={n.path} to={n.path} Icon={n.Icon} label={n.label} badge={(n as any).badge} collapsed={sidebarCollapsed} />)}
          </>
        )}
      </nav>

      {/* Footer */}
      <div style={{
        borderTop: '1px solid var(--border-subtle)',
        padding: sidebarCollapsed ? '8px 5px' : '8px 10px',
        flexShrink: 0,
      }}>
        {!sidebarCollapsed ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            {/* Avatar */}
            <div style={{
              width: 30, height: 30, borderRadius: '50%', flexShrink: 0,
              background: `color-mix(in srgb, ${ROLE_COLORS[user?.role ?? 'Investor']} 18%, transparent)`,
              border: `1.5px solid color-mix(in srgb, ${ROLE_COLORS[user?.role ?? 'Investor']} 45%, transparent)`,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 12, fontWeight: 700,
              color: ROLE_COLORS[user?.role ?? 'Investor'],
            }}>
              {user?.email?.[0]?.toUpperCase() ?? '?'}
            </div>
            <div style={{ flex: 1, minWidth: 0 }}>
              <div style={{ fontSize: 12, fontWeight: 500, color: 'var(--text-primary)', overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {user?.email?.split('@')[0] ?? 'User'}
              </div>
              <div style={{
                display: 'inline-flex', marginTop: 2,
                padding: '1px 5px', borderRadius: 99,
                fontSize: 9, fontWeight: 700, letterSpacing: '.04em', textTransform: 'uppercase',
                background: `color-mix(in srgb, ${ROLE_COLORS[user?.role ?? 'Investor']} 12%, transparent)`,
                border: `1px solid color-mix(in srgb, ${ROLE_COLORS[user?.role ?? 'Investor']} 30%, transparent)`,
                color: ROLE_COLORS[user?.role ?? 'Investor'],
              }}>
                {user?.role}
              </div>
            </div>
            <button onClick={() => logout()} className="btn btn-ghost btn-icon btn-sm" title="Sign out"
              style={{ flexShrink: 0, color: 'var(--text-tertiary)', transition: 'color var(--dur-fast)' }}
              onMouseEnter={e => { (e.currentTarget as HTMLButtonElement).style.color = 'var(--bear-strong)' }}
              onMouseLeave={e => { (e.currentTarget as HTMLButtonElement).style.color = 'var(--text-tertiary)' }}>
              <I.Logout />
            </button>
          </div>
        ) : (
          <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 6 }}>
            <button onClick={toggleSidebar} className="btn btn-ghost btn-icon btn-sm" title="Expand sidebar" style={{ color: 'var(--text-tertiary)' }}>
              <I.Expand />
            </button>
            <div style={{
              width: 26, height: 26, borderRadius: '50%',
              background: `color-mix(in srgb, ${ROLE_COLORS[user?.role ?? 'Investor']} 18%, transparent)`,
              border: `1.5px solid color-mix(in srgb, ${ROLE_COLORS[user?.role ?? 'Investor']} 45%, transparent)`,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              fontSize: 11, fontWeight: 700, color: ROLE_COLORS[user?.role ?? 'Investor'],
              cursor: 'default',
            }}>
              {user?.email?.[0]?.toUpperCase() ?? '?'}
            </div>
          </div>
        )}
      </div>
    </aside>
  )
}
