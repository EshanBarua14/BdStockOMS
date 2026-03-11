// @ts-nocheck
import { useLocation } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { ThemeMenu }    from '@/components/ui/ThemeMenu'

const ROUTE_LABELS: Record<string, string> = {
  '/dashboard': 'Dashboard',
  '/orders':    'Orders',
  '/portfolio': 'Portfolio',
  '/market':    'Market Data',
  '/admin':     'Admin Panel',
  '/tenants':   'Tenant Manager',
  '/rbac':      'RBAC & Roles',
}

export function Topbar() {
  const location = useLocation()
  const user     = useAuthStore(s => s.user)
  const title    = ROUTE_LABELS[location.pathname] ?? 'BD Stock OMS'

  return (
    <header style={{
      height:         56,
      background:     '#0D1320',
      borderBottom:   '1px solid rgba(255,255,255,0.06)',
      display:        'flex',
      alignItems:     'center',
      padding:        '0 20px',
      gap:            16,
      position:       'sticky',
      top:            0,
      zIndex:         50,
    }}>
      {/* Page title */}
      <div style={{ flex: 1 }}>
        <span style={{
          color:       '#fff',
          fontSize:    15,
          fontWeight:  600,
          fontFamily:  "'Outfit', sans-serif",
          letterSpacing: '-0.01em',
        }}>{title}</span>
      </div>

      {/* Right side controls */}
      <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>

        {/* Market status badge */}
        <div style={{
          display:    'flex',
          alignItems: 'center',
          gap:        6,
          background: 'rgba(0,212,170,0.08)',
          border:     '1px solid rgba(0,212,170,0.2)',
          borderRadius: 20,
          padding:    '4px 10px',
        }}>
          <span style={{
            width: 6, height: 6, borderRadius: '50%',
            background: '#00D4AA',
            boxShadow:  '0 0 6px #00D4AA',
            display:    'inline-block',
          }} />
          <span style={{
            color:      '#00D4AA',
            fontSize:   11,
            fontFamily: "'Space Mono', monospace",
            fontWeight: 600,
          }}>DSE LIVE</span>
        </div>

        {/* Theme menu */}
        <ThemeMenu variant="compact" />

        {/* User avatar */}
        {user && (
          <div style={{
            width:        32,
            height:       32,
            borderRadius: '50%',
            background:   'rgba(0,212,170,0.15)',
            border:       '1px solid rgba(0,212,170,0.3)',
            display:      'flex',
            alignItems:   'center',
            justifyContent: 'center',
            color:        '#00D4AA',
            fontWeight:   700,
            fontSize:     13,
            fontFamily:   "'Outfit', sans-serif",
            cursor:       'default',
            flexShrink:   0,
          }}
            title={`${user.fullName} · ${user.role}`}
          >
            {user.fullName.charAt(0)}
          </div>
        )}
      </div>
    </header>
  )
}
