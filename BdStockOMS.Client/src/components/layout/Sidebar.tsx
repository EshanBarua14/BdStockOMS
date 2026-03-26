// @ts-nocheck
import { useState } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { authApi } from '@/api/auth'
import type { UserRole } from '@/types'
import logoImg from '@/assets/images/logo.png'

// ─── Icons ────────────────────────────────────────────────────────────────────
const Icon = {
  Dashboard:    () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><rect x="3" y="3" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/><rect x="14" y="3" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/><rect x="3" y="14" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/><rect x="14" y="14" width="7" height="7" rx="1.5" stroke="currentColor" strokeWidth="1.5"/></svg>,
  Broker:       () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/><path d="M9 22V12h6v10" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/></svg>,
  BrokerSummary:() => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><rect x="3" y="3" width="18" height="4" rx="1" stroke="currentColor" strokeWidth="1.5"/><rect x="3" y="10" width="11" height="4" rx="1" stroke="currentColor" strokeWidth="1.5"/><rect x="3" y="17" width="7" height="4" rx="1" stroke="currentColor" strokeWidth="1.5"/></svg>,
  TradeMonitor: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M2 12h2l3-7 4 14 3-9 2 5 2-3h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>,
  User:         () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1.5"/><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  Branch:       () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M6 3v12M18 9a3 3 0 100-6 3 3 0 000 6zM6 21a3 3 0 100-6 3 3 0 000 6zM18 9c0 6-12 3-12 9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>,
  BackOffice:   () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/><rect x="9" y="3" width="6" height="4" rx="1" stroke="currentColor" strokeWidth="1.5"/><path d="M9 12h6M9 16h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  BOAccount:    () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M20 7H4a2 2 0 00-2 2v10a2 2 0 002 2h16a2 2 0 002-2V9a2 2 0 00-2-2z" stroke="currentColor" strokeWidth="1.5"/><path d="M16 3H8L4 7h16l-4-4z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/><circle cx="12" cy="13" r="2" stroke="currentColor" strokeWidth="1.5"/></svg>,
  Accounts:     () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><circle cx="8" cy="8" r="4" stroke="currentColor" strokeWidth="1.5"/><circle cx="16" cy="16" r="4" stroke="currentColor" strokeWidth="1.5"/><path d="M12 12l2 2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  Risk:         () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M12 2L3 7v5c0 5.25 3.75 10.15 9 11.35C17.25 22.15 21 17.25 21 12V7l-9-5z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/></svg>,
  FixAdmin:     () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M12 2a10 10 0 100 20A10 10 0 0012 2z" stroke="currentColor" strokeWidth="1.5"/><path d="M12 8v4l3 3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  LogAnalysis:  () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M4 6h16M4 10h10M4 14h12M4 18h8" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  UserActivity: () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M17 21v-2a4 4 0 00-4-4H5a4 4 0 00-4 4v2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/><circle cx="9" cy="7" r="4" stroke="currentColor" strokeWidth="1.5"/><path d="M23 21v-2a4 4 0 00-3-3.87M16 3.13a4 4 0 010 7.75" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  Reports:      () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M3 3h18v4H3zM3 10h18v4H3zM3 17h10v4H3z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/></svg>,
  AppSetting:   () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M12 15a3 3 0 100-6 3 3 0 000 6z" stroke="currentColor" strokeWidth="1.5"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z" stroke="currentColor" strokeWidth="1.5"/></svg>,
  Settings:     () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="3" stroke="currentColor" strokeWidth="1.5"/><path d="M19.4 15a1.65 1.65 0 00.33 1.82l.06.06a2 2 0 010 2.83 2 2 0 01-2.83 0l-.06-.06a1.65 1.65 0 00-1.82-.33 1.65 1.65 0 00-1 1.51V21a2 2 0 01-4 0v-.09A1.65 1.65 0 009 19.4a1.65 1.65 0 00-1.82.33l-.06.06a2 2 0 01-2.83-2.83l.06-.06A1.65 1.65 0 004.68 15a1.65 1.65 0 00-1.51-1H3a2 2 0 010-4h.09A1.65 1.65 0 004.6 9a1.65 1.65 0 00-.33-1.82l-.06-.06a2 2 0 012.83-2.83l.06.06A1.65 1.65 0 009 4.68a1.65 1.65 0 001-1.51V3a2 2 0 014 0v.09a1.65 1.65 0 001 1.51 1.65 1.65 0 001.82-.33l.06-.06a2 2 0 012.83 2.83l-.06.06A1.65 1.65 0 0019.4 9a1.65 1.65 0 001.51 1H21a2 2 0 010 4h-.09a1.65 1.65 0 00-1.51 1z" stroke="currentColor" strokeWidth="1.5"/></svg>,
  AboutUs:      () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="1.5"/><path d="M12 8h.01M11 12h1v4h1" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  Orders:       () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/><rect x="9" y="3" width="6" height="4" rx="1" stroke="currentColor" strokeWidth="1.5"/></svg>,
  Portfolio:    () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M3 17l4-8 4 5 3-3 4 6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/><path d="M3 20h18" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  KYC:          () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M9 12l2 2 4-4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/><path d="M12 2L3 7v5c0 5.25 3.75 10.15 9 11.35C17.25 22.15 21 17.25 21 12V7l-9-5z" stroke="currentColor" strokeWidth="1.5" strokeLinejoin="round"/></svg>,
  Settlements:  () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M12 2v20M17 5H9.5a3.5 3.5 0 000 7h5a3.5 3.5 0 010 7H6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>,
  Logout:       () => <svg width="18" height="18" viewBox="0 0 24 24" fill="none"><path d="M9 21H5a2 2 0 01-2-2V5a2 2 0 012-2h4" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/><path d="M16 17l5-5-5-5M21 12H9" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>,
  Collapse:     () => <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M15 18l-6-6 6-6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>,
  ChevronDown:  () => <svg width="12" height="12" viewBox="0 0 24 24" fill="none"><path d="M6 9l6 6 6-6" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>,
}

const mono = "'JetBrains Mono', monospace"
const ADMIN_ROLES: UserRole[] = ['SuperAdmin', 'Admin']
const BROKER_ROLES: UserRole[] = ['SuperAdmin', 'Admin', 'BrokerageAdmin', 'BrokerageHouse']

// ─── Nav structure ────────────────────────────────────────────────────────────
// Each item: { label, path, Icon, roles?, children? }
// children → renders as collapsible sub-menu
const NAV_STRUCTURE = [
  // ── Trader / Investor items ──────────────────────────────────────────────
  { label: 'Dashboard',    path: '/dashboard',     Icon: Icon.Dashboard,    roles: null },
  { label: 'Orders',       path: '/orders',         Icon: Icon.Orders,       roles: null },
  { label: 'Portfolio',    path: '/portfolio',      Icon: Icon.Portfolio,    roles: ['Investor','Trader','Admin','SuperAdmin','BrokerageHouse','BrokerageAdmin'] },
  { label: 'Trade Monitor',path: '/trade-monitor',  Icon: Icon.TradeMonitor, roles: ['SuperAdmin','Admin','BrokerageAdmin','CCD','Trader','Investor'] },

  // ── Admin / SuperAdmin block ─────────────────────────────────────────────
  { _section: 'ADMINISTRATION', roles: ADMIN_ROLES },

  { label: 'Broker',         path: '/admin/brokers',         Icon: Icon.Broker,       roles: ADMIN_ROLES },
  { label: 'Broker Summary', path: '/admin/broker-summary',  Icon: Icon.BrokerSummary,roles: ADMIN_ROLES },
  { label: 'Trade Monitor',  path: '/trade-monitor',         Icon: Icon.TradeMonitor, roles: ADMIN_ROLES },
  { label: 'User',           path: '/rbac',                  Icon: Icon.User,         roles: ADMIN_ROLES },
  { label: 'Branch',         path: '/admin/branches',        Icon: Icon.Branch,       roles: ADMIN_ROLES },
  { label: 'Back Office',    path: '/admin',                  Icon: Icon.BackOffice,   roles: ADMIN_ROLES },
  { label: 'BO Account',     path: '/admin/bo-accounts',     Icon: Icon.BOAccount,    roles: ADMIN_ROLES },

  {
    label: 'Accounts', Icon: Icon.Accounts, roles: BROKER_ROLES,
    children: [
      { label: 'Portfolio',    path: '/portfolio',    Icon: Icon.Portfolio   },
      { label: 'Settlements',  path: '/settlements',  Icon: Icon.Settlements },
    ]
  },

  { label: 'Risk Management', path: '/rms',             Icon: Icon.Risk,        roles: ADMIN_ROLES },
  { label: 'Fix Admin',       path: '/admin/fix',        Icon: Icon.FixAdmin,    roles: ['SuperAdmin'] },
  { label: 'Log Analysis',    path: '/audit',            Icon: Icon.LogAnalysis, roles: ADMIN_ROLES },
  { label: 'User Activities', path: '/admin/activities', Icon: Icon.UserActivity,roles: ADMIN_ROLES },

  {
    label: 'Reports', Icon: Icon.Reports, roles: ADMIN_ROLES,
    children: [
      { label: 'Reports',   path: '/reports', Icon: Icon.Reports      },
      { label: 'Audit Log', path: '/audit',   Icon: Icon.LogAnalysis  },
    ]
  },

  { label: 'App Setting', path: '/admin/app-settings', Icon: Icon.AppSetting, roles: ['SuperAdmin'] },

  {
    label: 'Settings', Icon: Icon.Settings, roles: ADMIN_ROLES,
    children: [
      { label: 'System Settings', path: '/settings', Icon: Icon.Settings },
      { label: 'KYC',             path: '/kyc',       Icon: Icon.KYC      },
    ]
  },

  { label: 'About Us', path: '/about', Icon: Icon.AboutUs, roles: null },
]

// ─── Sub-menu item ────────────────────────────────────────────────────────────
function SubItem({ item, collapsed }: any) {
  return (
    <NavLink to={item.path} title={collapsed ? item.label : undefined}
      style={({ isActive }) => ({
        display: 'flex', alignItems: 'center', gap: 8,
        padding: collapsed ? '8px 0' : '7px 10px 7px 36px',
        justifyContent: collapsed ? 'center' : 'flex-start',
        borderRadius: 6, textDecoration: 'none', fontSize: 11, fontWeight: isActive ? 600 : 400,
        color: isActive ? 'var(--t-accent)' : 'var(--t-text3)',
        background: isActive ? 'var(--t-hover)' : 'transparent',
        transition: 'all 0.12s', whiteSpace: 'nowrap',
      })}
      onMouseEnter={e => { if (!e.currentTarget.classList.contains('active')) e.currentTarget.style.color = 'var(--t-text2)' }}
      onMouseLeave={e => { if (!e.currentTarget.classList.contains('active')) e.currentTarget.style.color = 'var(--t-text3)' }}
    >
      <item.Icon />
      {!collapsed && <span>{item.label}</span>}
    </NavLink>
  )
}

// ─── Nav item (with optional sub-menu) ───────────────────────────────────────
function NavItem({ item, collapsed }: any) {
  const [open, setOpen] = useState(false)

  if (item.children) {
    return (
      <div>
        <button onClick={() => setOpen(v => !v)}
          title={collapsed ? item.label : undefined}
          style={{
            display: 'flex', alignItems: 'center', gap: 10, width: '100%',
            padding: collapsed ? '10px 0' : '9px 12px',
            justifyContent: collapsed ? 'center' : 'flex-start',
            borderRadius: 8, border: 'none', cursor: 'pointer', fontSize: 13, fontWeight: 500,
            color: 'var(--t-text2)', background: open ? 'var(--t-hover)' : 'transparent',
            transition: 'all 0.15s', whiteSpace: 'nowrap',
          }}
          onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
          onMouseLeave={e => { if (!open) e.currentTarget.style.background = 'transparent' }}
        >
          <item.Icon />
          {!collapsed && (
            <>
              <span style={{ flex: 1, textAlign: 'left' }}>{item.label}</span>
              <span style={{ transform: open ? 'rotate(180deg)' : 'none', transition: 'transform 0.2s', color: 'var(--t-text3)' }}>
                <Icon.ChevronDown />
              </span>
            </>
          )}
        </button>

        {open && !collapsed && (
          <div style={{ marginTop: 1, marginBottom: 1 }}>
            {item.children.map((c: any) => <SubItem key={c.path} item={c} collapsed={collapsed} />)}
          </div>
        )}
        {open && collapsed && (
          <div style={{ marginTop: 1 }}>
            {item.children.map((c: any) => <SubItem key={c.path} item={c} collapsed={collapsed} />)}
          </div>
        )}
      </div>
    )
  }

  return (
    <NavLink to={item.path} title={collapsed ? item.label : undefined}
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
  )
}

// ─── Sidebar ─────────────────────────────────────────────────────────────────
export function Sidebar() {
  const [collapsed, setCollapsed] = useState(false)
  const navigate = useNavigate()
  const user = useAuthStore(s => s.user)
  const logout = useAuthStore(s => s.logout)
  if (!user) return null

  const isAdmin = ADMIN_ROLES.includes(user.role)

  const handleLogout = async () => {
    try { await authApi.logout() } catch {}
    logout(); navigate('/login')
  }

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

      {/* ── Logo ── */}
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
              <div style={{ fontSize: 8, color: 'var(--t-text3)', fontFamily: mono, letterSpacing: '0.1em' }}>BSEC · DSE · CSE · CDBL</div>
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

      {/* ── User card ── */}
      {!collapsed && (
        <div style={{ margin: '10px 12px', padding: 12, background: 'var(--t-hover)', border: '1px solid var(--t-border)', borderRadius: 10 }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <div style={{
              width: 34, height: 34, borderRadius: '50%',
              background: 'var(--t-hover)', border: '1px solid var(--t-border)',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              color: 'var(--t-accent)', fontWeight: 700, fontSize: 14, fontFamily: mono,
            }}>{user.fullName?.charAt(0)}</div>
            <div style={{ overflow: 'hidden', flex: 1 }}>
              <div style={{ color: 'var(--t-text1)', fontSize: 12, fontWeight: 600, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{user.fullName}</div>
              <div style={{
                display: 'inline-flex', alignItems: 'center', gap: 4, marginTop: 3,
                background: 'var(--t-hover)', color: 'var(--t-accent)',
                fontSize: 9, fontWeight: 700, padding: '2px 8px', borderRadius: 6,
                fontFamily: mono, border: '1px solid var(--t-border)',
              }}>
                <span style={{ width: 4, height: 4, borderRadius: '50%', background: 'var(--t-accent)' }} />
                {user.role}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* ── Nav ── */}
      <nav style={{ flex: 1, padding: collapsed ? '12px 6px' : '12px 10px', display: 'flex', flexDirection: 'column', gap: 1, overflowY: 'auto' }}>
        {NAV_STRUCTURE.map((item, idx) => {
          // Section header
          if (item._section) {
            if (!isAdmin) return null
            return collapsed ? (
              <div key={idx} style={{ height: 1, margin: '10px 4px', background: 'var(--t-border)' }} />
            ) : (
              <div key={idx} style={{ color: 'var(--t-text3)', fontSize: 9, fontFamily: mono, letterSpacing: '0.12em', padding: '14px 4px 5px', fontWeight: 700 }}>
                {item._section}
              </div>
            )
          }

          // Role check
          if (item.roles && !item.roles.includes(user.role)) return null

          return <NavItem key={item.label + idx} item={item} collapsed={collapsed} />
        })}
      </nav>

      {/* ── Bottom ── */}
      <div style={{ padding: collapsed ? '12px 6px' : '12px 10px', borderTop: '1px solid var(--t-border)', display: 'flex', flexDirection: 'column', gap: 8 }}>
        {!collapsed && user.role === 'SuperAdmin' && (
          <div style={{
            background: 'var(--t-hover)', border: '1px solid var(--t-border)',
            borderRadius: 8, padding: '7px 10px', color: 'var(--t-accent)',
            fontSize: 10, fontFamily: mono, fontWeight: 600,
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
