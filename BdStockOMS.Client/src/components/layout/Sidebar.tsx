import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { authApi } from '@/api/auth'
import type { UserRole } from '@/types'

// ── Icons ─────────────────────────────────────────────────────────────────
const Icon = {
  Dashboard: () => (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="none">
      <rect x="3" y="3" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5"/>
      <rect x="14" y="3" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5"/>
      <rect x="3" y="14" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5"/>
      <rect x="14" y="14" width="7" height="7" rx="1" stroke="currentColor" strokeWidth="1.5"/>
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

// ── Nav item definition ───────────────────────────────────────────────────
interface NavItem {
  label: string
  path:  string
  Icon:  () => React.ReactElement
  roles?: UserRole[]
  badge?: number
  section?: string
}

const NAV_ITEMS: NavItem[] = [
  // All roles
  { label: 'Dashboard', path: '/dashboard', Icon: Icon.Dashboard },
  { label: 'Orders',    path: '/orders',    Icon: Icon.Orders },
  { label: 'Portfolio', path: '/portfolio', Icon: Icon.Portfolio,
    roles: ['Investor', 'Trader', 'Admin', 'SuperAdmin', 'BrokerageHouse', 'BrokerageAdmin'] },
  { label: 'Market',    path: '/market',    Icon: Icon.Market },

  // Admin section
  { label: 'Admin Panel', path: '/admin',  Icon: Icon.Admin,
    roles: ['SuperAdmin', 'Admin'], section: 'Administration' },

  // SuperAdmin only
  { label: 'Tenant Manager', path: '/tenants', Icon: Icon.Tenants,
    roles: ['SuperAdmin'], section: 'Administration' },
  { label: 'RBAC & Roles',   path: '/rbac',    Icon: Icon.RBAC,
    roles: ['SuperAdmin'], section: 'Administration' },
]

// ── Role badge colour ─────────────────────────────────────────────────────
function roleBadgeColor(role: UserRole) {
  const map: Record<UserRole, string> = {
    SuperAdmin:     '#FF6B6B',
    Admin:          '#FFA500',
    BrokerageHouse: '#A78BFA',
    BrokerageAdmin: '#C084FC',
    Trader:         '#00D4AA',
    Investor:       '#38BDF8',
    ITSupport:      '#94A3B8',
    CCD:            '#FB7185',
  }
  return map[role] ?? '#64748B'
}

// ── NavItem component ─────────────────────────────────────────────────────
function SideNavItem({ item, collapsed }: { item: NavItem; collapsed: boolean }) {
  return (
    <NavLink
      to={item.path}
      style={({ isActive }) => ({
        display:        'flex',
        alignItems:     'center',
        gap:            10,
        padding:        collapsed ? '10px 0' : '9px 12px',
        justifyContent: collapsed ? 'center' : 'flex-start',
        borderRadius:   8,
        textDecoration: 'none',
        fontSize:       13,
        fontWeight:     500,
        color:          isActive ? '#00D4AA' : 'rgba(255,255,255,0.5)',
        background:     isActive ? 'rgba(0,212,170,0.08)' : 'transparent',
        border:         isActive ? '1px solid rgba(0,212,170,0.15)' : '1px solid transparent',
        transition:     'all 0.15s',
        position:       'relative',
        whiteSpace:     'nowrap',
      })}
      title={collapsed ? item.label : undefined}
    >
      <item.Icon />
      {!collapsed && <span style={{ flex: 1 }}>{item.label}</span>}
      {!collapsed && item.badge != null && (
        <span style={{
          background: '#FF6B6B', color: '#fff',
          fontSize: 10, fontWeight: 700,
          padding: '1px 6px', borderRadius: 10,
          minWidth: 18, textAlign: 'center',
        }}>{item.badge}</span>
      )}
    </NavLink>
  )
}

// ── Sidebar ───────────────────────────────────────────────────────────────
export function Sidebar() {
  const [collapsed, setCollapsed] = useState(false)
  const navigate = useNavigate()
  const user = useAuthStore(s => s.user)
  const logout = useAuthStore(s => s.logout)

  if (!user) return null

  const visibleItems = NAV_ITEMS.filter(item =>
    !item.roles || item.roles.includes(user.role)
  )

  // Group into sections
  const mainItems  = visibleItems.filter(i => !i.section)
  const adminItems = visibleItems.filter(i => i.section === 'Administration')

  const handleLogout = async () => {
    try { await authApi.logout() } catch { /* swallow */ }
    logout()
    navigate('/login')
  }

  return (
    <aside style={{
      width:          collapsed ? 56 : 220,
      minHeight:      '100vh',
      background:     '#0D1320',
      borderRight:    '1px solid rgba(255,255,255,0.06)',
      display:        'flex',
      flexDirection:  'column',
      transition:     'width 0.2s',
      flexShrink:     0,
      position:       'sticky',
      top:            0,
    }}>
      {/* Logo */}
      <div style={{
        padding:     collapsed ? '20px 0' : '20px 16px',
        display:     'flex',
        alignItems:  'center',
        justifyContent: collapsed ? 'center' : 'space-between',
        borderBottom: '1px solid rgba(255,255,255,0.06)',
      }}>
        {!collapsed && (
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <svg width="24" height="24" viewBox="0 0 28 28">
              <polygon points="14,2 24,8 24,20 14,26 4,20 4,8" fill="none" stroke="#00D4AA" strokeWidth="1.5"/>
              <text x="14" y="17" textAnchor="middle" fill="#00D4AA" fontSize="6" fontFamily="Space Mono" fontWeight="bold">OMS</text>
            </svg>
            <span style={{ color: '#fff', fontWeight: 700, fontSize: 13, letterSpacing: '0.02em' }}>BD Stock OMS</span>
          </div>
        )}
        {collapsed && (
          <svg width="24" height="24" viewBox="0 0 28 28">
            <polygon points="14,2 24,8 24,20 14,26 4,20 4,8" fill="none" stroke="#00D4AA" strokeWidth="1.5"/>
          </svg>
        )}
        <button onClick={() => setCollapsed(v => !v)} style={{
          background: 'none', border: 'none', cursor: 'pointer',
          color: 'rgba(255,255,255,0.3)', padding: 4, display: 'flex',
          transform: collapsed ? 'rotate(180deg)' : 'none',
          transition: 'transform 0.2s',
        }}>
          <Icon.Collapse />
        </button>
      </div>

      {/* User info */}
      {!collapsed && (
        <div style={{
          padding: '12px 16px',
          borderBottom: '1px solid rgba(255,255,255,0.06)',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
            <div style={{
              width: 32, height: 32, borderRadius: '50%',
              background: 'rgba(0,212,170,0.15)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: '#00D4AA', fontWeight: 700, fontSize: 13,
              border: '1px solid rgba(0,212,170,0.3)',
              flexShrink: 0,
            }}>
              {user.fullName.charAt(0)}
            </div>
            <div style={{ overflow: 'hidden' }}>
              <div style={{ color: '#fff', fontSize: 12, fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                {user.fullName}
              </div>
              <div style={{
                display: 'inline-block',
                background: roleBadgeColor(user.role) + '22',
                color: roleBadgeColor(user.role),
                fontSize: 10, fontWeight: 700, padding: '1px 6px',
                borderRadius: 4, fontFamily: "'Space Mono', monospace",
                letterSpacing: '0.04em',
              }}>
                {user.role}
              </div>
            </div>
          </div>
          {user.role !== 'SuperAdmin' && (
            <div style={{ marginTop: 6, color: 'rgba(255,255,255,0.3)', fontSize: 11, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
              {user.brokerageHouseName}
            </div>
          )}
        </div>
      )}

      {/* Nav */}
      <nav style={{ flex: 1, padding: collapsed ? '12px 8px' : '12px 12px', display: 'flex', flexDirection: 'column', gap: 2 }}>
        {/* Main items */}
        {mainItems.map(item => (
          <SideNavItem key={item.path} item={item} collapsed={collapsed} />
        ))}

        {/* Admin section */}
        {adminItems.length > 0 && (
          <>
            {!collapsed && (
              <div style={{
                color: 'rgba(255,255,255,0.2)', fontSize: 10,
                fontFamily: "'Space Mono', monospace",
                letterSpacing: '0.1em', textTransform: 'uppercase',
                padding: '12px 4px 4px',
              }}>
                Administration
              </div>
            )}
            {collapsed && <div style={{ height: 1, background: 'rgba(255,255,255,0.06)', margin: '8px 0' }} />}
            {adminItems.map(item => (
              <SideNavItem key={item.path} item={item} collapsed={collapsed} />
            ))}
          </>
        )}
      </nav>

      {/* Bottom: brokerage + logout */}
      <div style={{
        padding: collapsed ? '12px 8px' : '12px 12px',
        borderTop: '1px solid rgba(255,255,255,0.06)',
        display: 'flex', flexDirection: 'column', gap: 8,
      }}>
        {!collapsed && user.role === 'SuperAdmin' && (
          <div style={{
            background: 'rgba(255,107,107,0.08)',
            border: '1px solid rgba(255,107,107,0.2)',
            borderRadius: 6, padding: '6px 10px',
            color: '#FF6B6B', fontSize: 11,
            fontFamily: "'Space Mono', monospace",
          }}>
            ⬡ Master DB Owner
          </div>
        )}
        <button onClick={handleLogout} style={{
          display:        'flex',
          alignItems:     'center',
          gap:            8,
          justifyContent: collapsed ? 'center' : 'flex-start',
          background:     'none',
          border:         '1px solid rgba(255,255,255,0.06)',
          borderRadius:   8,
          padding:        collapsed ? '10px 0' : '9px 12px',
          color:          'rgba(255,255,255,0.4)',
          fontSize:       13,
          cursor:         'pointer',
          width:          '100%',
          transition:     'all 0.15s',
        }}>
          <Icon.Logout />
          {!collapsed && <span>Sign out</span>}
        </button>
      </div>
    </aside>
  )
}
