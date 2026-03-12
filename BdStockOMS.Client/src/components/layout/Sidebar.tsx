// @ts-nocheck
// src/components/layout/Sidebar.tsx
// Premium OMS Sidebar — Glass + Neon + 3D + Your Logo

import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { authApi } from '@/api/auth'
import type { UserRole } from '@/types'
import logoImg from '@/assets/images/logo.png'

// ── Icons (same SVGs, kept compact) ──────────────────────────────────────
const Icon = {
  Dashboard: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <rect x="3" y="3" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/>
      <rect x="14" y="3" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/>
      <rect x="3" y="14" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/>
      <rect x="14" y="14" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/>
    </svg>
  ),
  Orders: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
      <rect x="9" y="3" width="6" height="4" rx="1" stroke="currentColor" strokeWidth="1.5"/>
      <path d="M9 12h6M9 16h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
    </svg>
  ),
  Portfolio: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M3 17l4-8 4 5 3-3 4 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
      <path d="M3 20h18" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
    </svg>
  ),
  Market: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M2 12h2l3-7 4 14 3-9 2 5 2-3h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  ),
  Admin: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/>
    </svg>
  ),
  Tenants: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/>
      <path d="M9 22V12h6v10" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/>
    </svg>
  ),
  RBAC: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1.5"/>
      <path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/>
      <path d="M18 11l2 2 4-4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  ),
  Logout: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
      <path d="M16 17l5-5-5-5M21 12H9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  ),
  Collapse: () => (
    <svg width="16" height="16" viewBox="0 0 24 24" fill="none">
      <path d="M15 18l-6-6 6-6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  ),
}

// ── Nav items (unchanged) ────────────────────────────────────────────────
interface NavItem {
  label: string; path: string; Icon: () => React.ReactElement
  roles?: UserRole[]; badge?: number; section?: string
}

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', Icon: Icon.Dashboard },
  { label: 'Orders',    path: '/orders',    Icon: Icon.Orders },
  { label: 'Portfolio', path: '/portfolio', Icon: Icon.Portfolio,
    roles: ['Investor','Trader','Admin','SuperAdmin','BrokerageHouse','BrokerageAdmin'] },
  { label: 'Market',    path: '/market',    Icon: Icon.Market },
  { label: 'Admin Panel',   path: '/admin',   Icon: Icon.Admin,
    roles: ['SuperAdmin','Admin'], section: 'Administration' },
  { label: 'Tenant Manager', path: '/tenants', Icon: Icon.Tenants,
    roles: ['SuperAdmin'], section: 'Administration' },
  { label: 'RBAC & Roles',   path: '/rbac',    Icon: Icon.RBAC,
    roles: ['SuperAdmin'], section: 'Administration' },
]

// ── Role colors ──────────────────────────────────────────────────────────
function roleBadgeColor(role: UserRole) {
  const map: Record<string, string> = {
    SuperAdmin: '#00D4AA', Admin: '#FFA500', BrokerageHouse: '#A78BFA',
    BrokerageAdmin: '#C084FC', Trader: '#00D4AA', Investor: '#38BDF8',
    ITSupport: '#94A3B8', CCD: '#FB7185',
  }
  return map[role] ?? '#64748B'
}

// ── Styles ───────────────────────────────────────────────────────────────
const S = {
  aside: (collapsed: boolean): React.CSSProperties => ({
    width:          collapsed ? 56 : 240,
    minHeight:      '100vh',
    background:     'linear-gradient(180deg, rgba(13,19,32,0.95) 0%, rgba(8,12,20,0.98) 100%)',
    backdropFilter: 'blur(20px)',
    borderRight:    '1px solid rgba(255,255,255,0.06)',
    display:        'flex',
    flexDirection:  'column',
    transition:     'width 0.3s cubic-bezier(0.16,1,0.3,1)',
    flexShrink:     0,
    position:       'sticky' as const,
    top:            0,
    zIndex:         100,
    overflow:       'hidden',
  }),

  logoArea: (collapsed: boolean): React.CSSProperties => ({
    padding:        collapsed ? '16px 0' : '16px',
    display:        'flex',
    alignItems:     'center',
    justifyContent: collapsed ? 'center' : 'space-between',
    borderBottom:   '1px solid rgba(255,255,255,0.06)',
    minHeight:      60,
    position:       'relative' as const,
  }),

  logoGlow: {
    position: 'absolute' as const,
    bottom: 0, left: '10%', right: '10%', height: 1,
    background: 'linear-gradient(90deg, transparent, rgba(0,212,170,0.3), transparent)',
  },

  navLink: (isActive: boolean, collapsed: boolean): React.CSSProperties => ({
    display:        'flex',
    alignItems:     'center',
    gap:            10,
    padding:        collapsed ? '10px 0' : '9px 12px',
    justifyContent: collapsed ? 'center' : 'flex-start',
    borderRadius:   8,
    textDecoration: 'none',
    fontSize:       13,
    fontWeight:     isActive ? 600 : 500,
    color:          isActive ? '#00D4AA' : 'rgba(255,255,255,0.45)',
    background:     isActive ? 'rgba(0,212,170,0.08)' : 'transparent',
    borderLeft:     isActive && !collapsed ? '2px solid #00D4AA' : '2px solid transparent',
    boxShadow:      isActive ? 'inset 0 0 20px rgba(0,212,170,0.04)' : 'none',
    transition:     'all 0.15s cubic-bezier(0.16,1,0.3,1)',
    whiteSpace:     'nowrap' as const,
    position:       'relative' as const,
  }),

  userCard: {
    margin:       '0 12px',
    padding:      '12px',
    background:   'rgba(255,255,255,0.02)',
    border:       '1px solid rgba(255,255,255,0.04)',
    borderRadius: 10,
  },

  avatar: (role: UserRole) => ({
    width: 34, height: 34, borderRadius: '50%',
    background: `linear-gradient(135deg, ${roleBadgeColor(role)}33, ${roleBadgeColor(role)}11)`,
    border: `1px solid ${roleBadgeColor(role)}44`,
    display: 'flex', alignItems: 'center', justifyContent: 'center',
    color: roleBadgeColor(role), fontWeight: 700, fontSize: 14,
    fontFamily: "'JetBrains Mono', 'Space Mono', monospace",
    flexShrink: 0,
  }),
}

// ── SideNavItem ──────────────────────────────────────────────────────────
function SideNavItem({ item, collapsed }: { item: NavItem; collapsed: boolean }) {
  const [hovered, setHovered] = useState(false)

  return (
    <NavLink
      to={item.path}
      style={({ isActive }) => ({
        ...S.navLink(isActive, collapsed),
        ...(hovered && !collapsed ? { background: 'rgba(255,255,255,0.04)', color: 'rgba(255,255,255,0.7)' } : {}),
      })}
      title={collapsed ? item.label : undefined}
      onMouseEnter={() => setHovered(true)}
      onMouseLeave={() => setHovered(false)}
    >
      {({ isActive }) => (
        <>
          <span style={{
            display: 'flex', alignItems: 'center',
            color: isActive ? '#00D4AA' : hovered ? 'rgba(255,255,255,0.6)' : 'rgba(255,255,255,0.35)',
            transition: 'color 0.15s',
            filter: isActive ? 'drop-shadow(0 0 4px rgba(0,212,170,0.4))' : 'none',
          }}>
            <item.Icon />
          </span>
          {!collapsed && (
            <span style={{ flex: 1, overflow: 'hidden', textOverflow: 'ellipsis' }}>{item.label}</span>
          )}
          {!collapsed && item.badge != null && (
            <span style={{
              background: '#FF6B6B', color: '#fff',
              fontSize: 9, fontWeight: 700,
              padding: '1px 6px', borderRadius: 10,
              minWidth: 18, textAlign: 'center',
              fontFamily: "'JetBrains Mono', monospace",
            }}>{item.badge}</span>
          )}
        </>
      )}
    </NavLink>
  )
}

// ── Main Sidebar ─────────────────────────────────────────────────────────
export function Sidebar() {
  const [collapsed, setCollapsed] = useState(false)
  const navigate   = useNavigate()
  const user       = useAuthStore(s => s.user)
  const logout     = useAuthStore(s => s.logout)

  if (!user) return null

  const visibleItems = NAV_ITEMS.filter(item => !item.roles || item.roles.includes(user.role))
  const mainItems    = visibleItems.filter(i => !i.section)
  const adminItems   = visibleItems.filter(i => i.section === 'Administration')

  const handleLogout = async () => {
    try { await authApi.logout() } catch {}
    logout()
    navigate('/login')
  }

  return (
    <aside style={S.aside(collapsed)}>

      {/* ═══ Logo ═══ */}
      <div style={S.logoArea(collapsed)}>
        {!collapsed ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <img
              src={logoImg}
              alt="BD Stock OMS"
              style={{
                height: 34, width: 'auto', objectFit: 'contain',
                filter: 'drop-shadow(0 0 8px rgba(0,212,170,0.2))',
              }}
            />
            <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.2 }}>
              <span style={{
                color: '#fff', fontWeight: 700, fontSize: 13,
                fontFamily: "'Outfit', 'Inter', sans-serif",
                letterSpacing: '-0.01em',
              }}>
                BD Stock <span style={{ color: '#00D4AA' }}>OMS</span>
              </span>
              <span style={{
                fontSize: 8, color: 'rgba(255,255,255,0.25)',
                fontFamily: "'JetBrains Mono', monospace",
                letterSpacing: '0.1em', textTransform: 'uppercase',
              }}>
                BSEC · DSE · CSE · CDBL
              </span>
            </div>
          </div>
        ) : (
          <img
            src={logoImg}
            alt="BD Stock OMS"
            style={{
              height: 28, width: 'auto', objectFit: 'contain',
              filter: 'drop-shadow(0 0 6px rgba(0,212,170,0.15))',
            }}
          />
        )}
        <button onClick={() => setCollapsed(v => !v)} style={{
          background: 'none', border: 'none', cursor: 'pointer',
          color: 'rgba(255,255,255,0.25)', padding: 4, display: 'flex',
          transform: collapsed ? 'rotate(180deg)' : 'none',
          transition: 'all 0.3s cubic-bezier(0.16,1,0.3,1)',
          borderRadius: 6,
        }}
          onMouseEnter={e => { e.currentTarget.style.color = 'rgba(255,255,255,0.6)'; e.currentTarget.style.background = 'rgba(255,255,255,0.04)' }}
          onMouseLeave={e => { e.currentTarget.style.color = 'rgba(255,255,255,0.25)'; e.currentTarget.style.background = 'none' }}
        >
          <Icon.Collapse />
        </button>
        {/* Neon line under logo */}
        <div style={S.logoGlow} />
      </div>

      {/* ═══ User Card ═══ */}
      {!collapsed && (
        <div style={S.userCard}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={S.avatar(user.role)}>
              {user.fullName.charAt(0)}
            </div>
            <div style={{ overflow: 'hidden', flex: 1 }}>
              <div style={{
                color: 'rgba(255,255,255,0.85)', fontSize: 12, fontWeight: 600,
                overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
                fontFamily: "'Outfit', sans-serif",
              }}>
                {user.fullName}
              </div>
              <div style={{
                display: 'inline-flex', alignItems: 'center', gap: 4, marginTop: 3,
                background: roleBadgeColor(user.role) + '15',
                color: roleBadgeColor(user.role),
                fontSize: 9, fontWeight: 700, padding: '2px 8px',
                borderRadius: 6, fontFamily: "'JetBrains Mono', monospace",
                letterSpacing: '0.06em',
                border: `1px solid ${roleBadgeColor(user.role)}25`,
              }}>
                <span style={{
                  width: 4, height: 4, borderRadius: '50%',
                  background: roleBadgeColor(user.role),
                  boxShadow: `0 0 4px ${roleBadgeColor(user.role)}60`,
                }} />
                {user.role}
              </div>
            </div>
          </div>
          {user.role !== 'SuperAdmin' && user.brokerageHouseName && (
            <div style={{
              marginTop: 8, color: 'rgba(255,255,255,0.20)', fontSize: 10,
              overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
              fontFamily: "'JetBrains Mono', monospace",
              paddingLeft: 44,
            }}>
              {user.brokerageHouseName}
            </div>
          )}
        </div>
      )}

      {/* ═══ Navigation ═══ */}
      <nav style={{
        flex: 1, padding: collapsed ? '12px 6px' : '12px 10px',
        display: 'flex', flexDirection: 'column', gap: 2,
        overflowY: 'auto', overflowX: 'hidden',
      }}>
        {mainItems.map(item => (
          <SideNavItem key={item.path} item={item} collapsed={collapsed} />
        ))}

        {adminItems.length > 0 && (
          <>
            {!collapsed ? (
              <div style={{
                color: 'rgba(255,255,255,0.15)', fontSize: 9,
                fontFamily: "'JetBrains Mono', monospace",
                letterSpacing: '0.12em', textTransform: 'uppercase',
                padding: '16px 4px 6px', fontWeight: 700,
              }}>
                Administration
              </div>
            ) : (
              <div style={{
                height: 1, margin: '10px 8px',
                background: 'linear-gradient(90deg, transparent, rgba(255,255,255,0.06), transparent)',
              }} />
            )}
            {adminItems.map(item => (
              <SideNavItem key={item.path} item={item} collapsed={collapsed} />
            ))}
          </>
        )}
      </nav>

      {/* ═══ Bottom ═══ */}
      <div style={{
        padding: collapsed ? '12px 6px' : '12px 10px',
        borderTop: '1px solid rgba(255,255,255,0.04)',
        display: 'flex', flexDirection: 'column', gap: 8,
      }}>
        {!collapsed && user.role === 'SuperAdmin' && (
          <div style={{
            background: 'rgba(255,107,107,0.06)',
            border: '1px solid rgba(255,107,107,0.12)',
            borderRadius: 8, padding: '7px 10px',
            color: '#FF6B6B', fontSize: 10,
            fontFamily: "'JetBrains Mono', monospace",
            fontWeight: 600, letterSpacing: '0.03em',
            display: 'flex', alignItems: 'center', gap: 6,
          }}>
            <span style={{
              width: 5, height: 5, borderRadius: '50%',
              background: '#FF6B6B',
              boxShadow: '0 0 6px rgba(255,107,107,0.4)',
              animation: 'oms-pulse 2s ease-in-out infinite',
            }} />
            Master DB Owner
          </div>
        )}

        <button onClick={handleLogout} style={{
          display:        'flex',
          alignItems:     'center',
          gap:            8,
          justifyContent: collapsed ? 'center' : 'flex-start',
          background:     'rgba(255,255,255,0.02)',
          border:         '1px solid rgba(255,255,255,0.05)',
          borderRadius:   8,
          padding:        collapsed ? '10px 0' : '9px 12px',
          color:          'rgba(255,255,255,0.35)',
          fontSize:       13,
          cursor:         'pointer',
          width:          '100%',
          transition:     'all 0.2s',
          fontFamily:     "'Outfit', sans-serif",
        }}
          onMouseEnter={e => {
            e.currentTarget.style.background = 'rgba(255,23,68,0.06)'
            e.currentTarget.style.borderColor = 'rgba(255,23,68,0.15)'
            e.currentTarget.style.color = '#ff6b6b'
          }}
          onMouseLeave={e => {
            e.currentTarget.style.background = 'rgba(255,255,255,0.02)'
            e.currentTarget.style.borderColor = 'rgba(255,255,255,0.05)'
            e.currentTarget.style.color = 'rgba(255,255,255,0.35)'
          }}
        >
          <Icon.Logout />
          {!collapsed && <span>Sign out</span>}
        </button>
      </div>
    </aside>
  )
}
