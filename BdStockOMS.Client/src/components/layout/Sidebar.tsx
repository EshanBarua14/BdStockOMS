// @ts-nocheck
import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { authApi } from '@/api/auth'
import type { UserRole } from '@/types'
import logoImg from '@/assets/images/logo.png'

const Icon = {
  Dashboard: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><rect x="3" y="3" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/></svg>,
  Orders: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/><rect x="9" y="3" width="6" height="4" rx="1" stroke="currentColor" strokeWidth="1.5"/><path d="M9 12h6M9 16h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  Portfolio: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M3 17l4-8 4 5 3-3 4 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/><path d="M3 20h18" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  Market: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M2 12h2l3-7 4 14 3-9 2 5 2-3h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>,
  Admin: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M12 2l3.09 6.26L22 9.27l-5 4.87 1.18 6.88L12 17.77l-6.18 3.25L7 14.14 2 9.27l6.91-1.01L12 2z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/></svg>,
  Tenants: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/><path d="M9 22V12h6v10" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/></svg>,
  RBAC: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1.5"/><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  Logout: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/><path d="M16 17l5-5-5-5M21 12H9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>,
  Collapse: () => <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M15 18l-6-6 6-6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>,
}

interface NavItem { label: string; path: string; Icon: () => React.ReactElement; roles?: UserRole[]; section?: string }

const NAV_ITEMS: NavItem[] = [
  { label: 'Dashboard', path: '/dashboard', Icon: Icon.Dashboard },
  { label: 'Orders',    path: '/orders',    Icon: Icon.Orders },
  { label: 'Portfolio', path: '/portfolio', Icon: Icon.Portfolio, roles: ['Investor','Trader','Admin','SuperAdmin','BrokerageHouse','BrokerageAdmin'] },
  { label: 'Market',    path: '/market',    Icon: Icon.Market },
  { label: 'Trade Monitor', path: '/trade-monitor', Icon: Icon.Market, roles: ['SuperAdmin','Admin','BrokerageAdmin','CCD','Trader','Investor'] },
  { label: 'Admin Panel',    path: '/admin',   Icon: Icon.Admin,   roles: ['SuperAdmin','Admin'], section: 'Admin' },
  { label: 'Tenant Manager', path: '/tenants', Icon: Icon.Tenants, roles: ['SuperAdmin'], section: 'Admin' },
  { label: 'RBAC & Roles',   path: '/rbac',    Icon: Icon.RBAC,    roles: ['SuperAdmin'], section: 'Admin' },
]

export function Sidebar() {
  const [collapsed, setCollapsed] = useState(false)
  const navigate = useNavigate()
  const user = useAuthStore(s => s.user)
  const logout = useAuthStore(s => s.logout)
  if (!user) return null

  const visible = NAV_ITEMS.filter(i => !i.roles || i.roles.includes(user.role))
  const main = visible.filter(i => !i.section)
  const admin = visible.filter(i => i.section === 'Admin')

  const handleLogout = async () => {
    try { await authApi.logout() } catch {}
    logout(); navigate('/login')
  }

  // ALL colors from CSS vars
  return (
    <aside style={{
      width: collapsed ? 56 : 240,
      minHeight: '100vh',
      background: 'var(--t-surface)',
      borderRight: '1px solid var(--t-border)',
      display: 'flex', flexDirection: 'column',
      transition: 'width 0.3s cubic-bezier(0.16,1,0.3,1)',
      flexShrink: 0, position: 'sticky', top: 0, zIndex: 100, overflow: 'hidden',
    }}>
      {/* Logo */}
      <div style={{
        padding: collapsed ? '16px 0' : '16px',
        display: 'flex', alignItems: 'center',
        justifyContent: collapsed ? 'center' : 'space-between',
        borderBottom: '1px solid var(--t-border)', minHeight: 60, position: 'relative',
      }}>
        {!collapsed ? (
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <img src={logoImg} alt="OMS" style={{ height: 34, width: 'auto', objectFit: 'contain' }} />
            <div style={{ lineHeight: 1.2 }}>
              <span style={{ color: 'var(--t-text1)', fontWeight: 700, fontSize: 13 }}>
                BD Stock <span style={{ color: 'var(--t-accent)' }}>OMS</span>
              </span>
              <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.1em' }}>BSEC · DSE · CSE · CDBL</div>
            </div>
          </div>
        ) : (
          <img src={logoImg} alt="OMS" style={{ height: 28, width: 'auto' }} />
        )}
        <button onClick={() => setCollapsed(v => !v)} style={{
          background: 'none', border: 'none', cursor: 'pointer',
          color: 'var(--t-text3)', padding: 4,
          transform: collapsed ? 'rotate(180deg)' : 'none',
          transition: 'all 0.3s', borderRadius: 6,
        }}><Icon.Collapse /></button>
        <div style={{ position: 'absolute', bottom: 0, left: '10%', right: '10%', height: 1, background: `linear-gradient(90deg, transparent, var(--t-accent), transparent)`, opacity: 0.2 }} />
      </div>

      {/* User */}
      {!collapsed && (
        <div style={{ margin: '10px 12px', padding: 12, background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 10 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{
              width: 34, height: 34, borderRadius: '50%',
              background: 'var(--t-hover)', border: '1px solid var(--t-border)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: 'var(--t-accent)', fontWeight: 700, fontSize: 14, fontFamily: "'JetBrains Mono', monospace",
            }}>{user.fullName.charAt(0)}</div>
            <div style={{ overflow: 'hidden', flex: 1 }}>
              <div style={{ color: 'var(--t-text1)', fontSize: 12, fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{user.fullName}</div>
              <div style={{
                display: 'inline-flex', alignItems: 'center', gap: 4, marginTop: 3,
                background: 'var(--t-hover)', color: 'var(--t-accent)',
                fontSize: 9, fontWeight: 700, padding: '2px 8px', borderRadius: 6,
                fontFamily: "'JetBrains Mono', monospace", border: '1px solid var(--t-border)',
              }}>
                <span style={{ width: 4, height: 4, borderRadius: '50%', background: 'var(--t-accent)' }} />
                {user.role}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Nav */}
      <nav style={{ flex: 1, padding: collapsed ? '12px 6px' : '12px 10px', display: 'flex', flexDirection: 'column', gap: 2, overflowY: 'auto' }}>
        {main.map(item => (
          <NavLink key={item.path} to={item.path} title={collapsed ? item.label : undefined}
            style={({ isActive }) => ({
              display: 'flex', alignItems: 'center', gap: 10,
              padding: collapsed ? '10px 0' : '9px 12px',
              justifyContent: collapsed ? 'center' : 'flex-start',
              borderRadius: 8, textDecoration: 'none', fontSize: 13, fontWeight: isActive ? 600 : 500,
              color: isActive ? 'var(--t-accent)' : 'var(--t-text2)',
              background: isActive ? 'var(--t-hover)' : 'transparent',
              borderLeft: isActive && !collapsed ? '2px solid var(--t-accent)' : '2px solid transparent',
              transition: 'all 0.15s', whiteSpace: 'nowrap',
            })}>
            <item.Icon />
            {!collapsed && <span style={{ flex: 1 }}>{item.label}</span>}
          </NavLink>
        ))}

        {admin.length > 0 && (
          <>
            {!collapsed ? (
              <div style={{ color: 'var(--t-text3)', fontSize: 9, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.12em', padding: '16px 4px 6px', fontWeight: 700 }}>ADMINISTRATION</div>
            ) : (
              <div style={{ height: 1, margin: '10px 8px', background: 'var(--t-border)' }} />
            )}
            {admin.map(item => (
              <NavLink key={item.path} to={item.path} title={collapsed ? item.label : undefined}
                style={({ isActive }) => ({
                  display: 'flex', alignItems: 'center', gap: 10,
                  padding: collapsed ? '10px 0' : '9px 12px',
                  justifyContent: collapsed ? 'center' : 'flex-start',
                  borderRadius: 8, textDecoration: 'none', fontSize: 13, fontWeight: isActive ? 600 : 500,
                  color: isActive ? 'var(--t-accent)' : 'var(--t-text2)',
                  background: isActive ? 'var(--t-hover)' : 'transparent',
                  transition: 'all 0.15s', whiteSpace: 'nowrap',
                })}>
                <item.Icon />
                {!collapsed && <span style={{ flex: 1 }}>{item.label}</span>}
              </NavLink>
            ))}
          </>
        )}
      </nav>

      {/* Bottom */}
      <div style={{ padding: collapsed ? '12px 6px' : '12px 10px', borderTop: '1px solid var(--t-border)', display: 'flex', flexDirection: 'column', gap: 8 }}>
        {!collapsed && user.role === 'SuperAdmin' && (
          <div style={{
            background: 'var(--t-hover)', border: '1px solid var(--t-border)',
            borderRadius: 8, padding: '7px 10px', color: 'var(--t-accent)',
            fontSize: 10, fontFamily: "'JetBrains Mono', monospace", fontWeight: 600,
            display: 'flex', alignItems: 'center', gap: 6,
          }}>
            <span style={{ width: 5, height: 5, borderRadius: '50%', background: 'var(--t-accent)', animation: 'oms-pulse 2s ease-in-out infinite' }} />
            Master DB Owner
          </div>
        )}
        <button onClick={handleLogout} style={{
          display: 'flex', alignItems: 'center', gap: 8,
          justifyContent: collapsed ? 'center' : 'flex-start',
          background: 'var(--t-hover)', border: '1px solid var(--t-border)',
          borderRadius: 8, padding: collapsed ? '10px 0' : '9px 12px',
          color: 'var(--t-text3)', fontSize: 13, cursor: 'pointer', width: '100%',
          transition: 'all 0.2s',
        }}>
          <Icon.Logout />
          {!collapsed && <span>Sign out</span>}
        </button>
      </div>
    </aside>
  )
}
