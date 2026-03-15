import { Link } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'

const card: React.CSSProperties = {
  minHeight: '100vh', display: 'flex', flexDirection: 'column',
  alignItems: 'center', justifyContent: 'center',
  background: '#080C14', color: '#fff', fontFamily: "'Outfit', sans-serif", gap: 16,
}
const tag: React.CSSProperties = {
  background: 'rgba(0,212,170,0.1)', border: '1px solid rgba(0,212,170,0.3)',
  color: '#00D4AA', padding: '4px 12px', borderRadius: 20, fontSize: 12,
  fontFamily: "'Space Mono', monospace",
}

function ComingSoon({ title, scope }: { title: string; scope: string }) {
  const user = useAuthStore(s => s.user)
  return (
    <div style={card}>
      <span style={tag}>{scope}</span>
      <h1 style={{ fontSize: 28, fontWeight: 700, margin: 0 }}>{title}</h1>
      <p style={{ color: 'rgba(255,255,255,0.4)', fontSize: 14, margin: 0 }}>
        Day 50 — coming next sprint
      </p>
      <p style={{ color: 'rgba(255,255,255,0.25)', fontSize: 12, margin: 0 }}>
        Logged in as <strong style={{ color: '#00D4AA' }}>{user?.fullName}</strong>
        {' '}· Role: <strong style={{ color: '#00D4AA' }}>{user?.role}</strong>
        {' '}· Brokerage: <strong style={{ color: '#00D4AA' }}>{user?.brokerageHouseName}</strong>
      </p>
      <Link to="/dashboard" style={{ color: '#00D4AA', fontSize: 13, marginTop: 8 }}>← Back to dashboard</Link>
    </div>
  )
}

export const OrdersPage     = () => <ComingSoon title="Orders"          scope="All Roles" />
export const PortfolioPage  = () => <ComingSoon title="Portfolio"       scope="Investor · Trader" />
export const MarketPage     = () => <ComingSoon title="Market Data"     scope="All Roles" />
export const SuperAdminPage = () => <ComingSoon title="Admin Dashboard" scope="SuperAdmin · Admin" />
export const TenantPage     = () => <ComingSoon title="Tenant Manager"  scope="SuperAdmin only" />
export const RbacPage       = () => <ComingSoon title="RBAC Dashboard"  scope="SuperAdmin only" />

export function ForbiddenPage() {
  return (
    <div style={card}>
      <span style={{ ...tag, background: 'rgba(255,107,107,0.1)', borderColor: 'rgba(255,107,107,0.3)', color: '#FF6B6B' }}>403 Forbidden</span>
      <h1 style={{ fontSize: 28, fontWeight: 700, margin: 0 }}>Access Denied</h1>
      <p style={{ color: 'rgba(255,255,255,0.4)', fontSize: 14 }}>You do not have permission to view this page.</p>
      <Link to="/dashboard" style={{ color: '#00D4AA', fontSize: 13 }}>← Back to dashboard</Link>
    </div>
  )
}

export function NotFoundPage() {
  return (
    <div style={card}>
      <span style={tag}>404</span>
      <h1 style={{ fontSize: 28, fontWeight: 700, margin: 0 }}>Page Not Found</h1>
      <Link to="/dashboard" style={{ color: '#00D4AA', fontSize: 13 }}>← Back to dashboard</Link>
    </div>
  )
}

export function RMSPage() {
  return (
    <div style={{ padding: 32, color: 'var(--t-text1)' }}>
      <div style={{ fontSize: 20, fontWeight: 700, marginBottom: 8 }}>Risk Management System</div>
      <div style={{ fontSize: 13, color: 'var(--t-text3)', marginBottom: 24 }}>Configure credit limits, board limits, trading code limits and sector limits per entity.</div>
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 12 }}>
        {['Client','User','BO Group','Basket','Branch','Broker'].map(entity => (
          <div key={entity} style={{ background: 'var(--t-surface)', border: '1px solid var(--t-border)', borderRadius: 10, padding: '16px 20px', cursor: 'pointer' }}
            onMouseEnter={e => e.currentTarget.style.borderColor = 'var(--t-accent)'}
            onMouseLeave={e => e.currentTarget.style.borderColor = 'var(--t-border)'}>
            <div style={{ fontSize: 13, fontWeight: 700, marginBottom: 4 }}>{entity}</div>
            <div style={{ fontSize: 11, color: 'var(--t-text3)' }}>Credit · Board · Trading Code · Sector · Category</div>
          </div>
        ))}
      </div>
    </div>
  )
}
