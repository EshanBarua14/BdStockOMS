import { Link } from 'react-router-dom'

function ComingSoon({ title, day }: { title: string; day: number }) {
  return (
    <div style={{ maxWidth: 900, margin: '0 auto' }}>
      <h1 style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 22, color: 'var(--text-primary)', letterSpacing: '-0.02em', marginBottom: 6 }}>{title}</h1>
      <div style={{ background: 'var(--bg-surface)', border: '1px solid var(--border-subtle)', borderRadius: 'var(--r-xl)', padding: '48px 32px', textAlign: 'center', marginTop: 24 }}>
        <div style={{ width: 64, height: 64, borderRadius: '50%', background: 'var(--accent-glow)', border: '1px solid color-mix(in srgb,var(--accent-500) 30%,transparent)', display: 'flex', alignItems: 'center', justifyContent: 'center', margin: '0 auto 16px' }}>
          <svg width="28" height="28" viewBox="0 0 24 24" fill="none" stroke="var(--accent-400)" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>
        </div>
        <p style={{ fontSize: 15, color: 'var(--text-secondary)', marginBottom: 8 }}><strong style={{ color: 'var(--text-primary)' }}>{title}</strong> is coming in Day {day}</p>
        <p style={{ fontSize: 12, color: 'var(--text-tertiary)' }}>Full implementation with real-time data, charts, and advanced filtering.</p>
      </div>
    </div>
  )
}

export const OrdersPage    = () => <ComingSoon title="Orders"       day={50} />
export const PortfolioPage = () => <ComingSoon title="Portfolio"    day={50} />
export const MarketPage    = () => <ComingSoon title="Market Watch" day={50} />
export const WatchlistPage = () => <ComingSoon title="Watchlist"    day={51} />
export const ReportsPage   = () => <ComingSoon title="Reports"      day={51} />

export function ForbiddenPage() {
  return (
    <div style={{ minHeight:'100vh', background:'var(--bg-base)', display:'flex', alignItems:'center', justifyContent:'center', padding:24 }}>
      <div style={{ textAlign:'center' }}>
        <div style={{ fontFamily:'var(--font-mono)', fontSize:72, fontWeight:700, color:'var(--bear-muted)', lineHeight:1, marginBottom:8 }}>403</div>
        <h1 style={{ fontFamily:'var(--font-display)', fontWeight:700, fontSize:22, color:'var(--text-primary)', marginBottom:8 }}>Access Forbidden</h1>
        <p style={{ color:'var(--text-secondary)', fontSize:14, marginBottom:24 }}>You don't have permission to view this page.</p>
        <Link to="/dashboard" className="btn btn-primary">← Back to Dashboard</Link>
      </div>
    </div>
  )
}

export function NotFoundPage() {
  return (
    <div style={{ minHeight:'100vh', background:'var(--bg-base)', display:'flex', alignItems:'center', justifyContent:'center', padding:24 }}>
      <div style={{ textAlign:'center' }}>
        <div style={{ fontFamily:'var(--font-mono)', fontSize:72, fontWeight:700, lineHeight:1, marginBottom:8, background:'linear-gradient(135deg,var(--accent-400),var(--accent-600))', WebkitBackgroundClip:'text', WebkitTextFillColor:'transparent' }}>404</div>
        <h1 style={{ fontFamily:'var(--font-display)', fontWeight:700, fontSize:22, color:'var(--text-primary)', marginBottom:8 }}>Page Not Found</h1>
        <p style={{ color:'var(--text-secondary)', fontSize:14, marginBottom:24 }}>The page you're looking for doesn't exist.</p>
        <Link to="/dashboard" className="btn btn-primary">← Back to Dashboard</Link>
      </div>
    </div>
  )
}
