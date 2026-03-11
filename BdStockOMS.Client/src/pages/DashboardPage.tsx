import { useState, useEffect, useRef } from 'react'
import { useAuthStore } from '@/store/authStore'

/* ─── Micro sparkline (SVG) ─────────────────────────────────────────────── */
function Sparkline({ data, color, height = 36 }: { data: number[]; color: string; height?: number }) {
  const w = 96, h = height
  const min = Math.min(...data), max = Math.max(...data)
  const range = max - min || 1
  const pts = data.map((v, i) => {
    const x = (i / (data.length - 1)) * w
    const y = h - ((v - min) / range) * h * 0.85 - h * 0.075
    return `${x},${y}`
  }).join(' ')
  const areaClose = `${w},${h} 0,${h}`
  return (
    <svg width={w} height={h} style={{ overflow:'visible' }}>
      <defs>
        <linearGradient id={`sg-${color.replace('#','')}`} x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity=".25"/>
          <stop offset="100%" stopColor={color} stopOpacity="0"/>
        </linearGradient>
      </defs>
      <polygon points={`${pts} ${areaClose}`} fill={`url(#sg-${color.replace('#','')})`}/>
      <polyline points={pts} fill="none" stroke={color} strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
    </svg>
  )
}

function generateSpark(base: number, n = 20): number[] {
  const pts = [base]
  for (let i = 1; i < n; i++) {
    pts.push(Math.max(0, pts[i-1] + (Math.random() - 0.48) * base * 0.03))
  }
  return pts
}

/* ─── KPI Card ──────────────────────────────────────────────────────────── */
interface KpiProps {
  label: string; value: string; subValue?: string
  change?: number; spark?: number[]; sparkColor?: string
  prefix?: string; delay?: number
}
function KpiCard({ label, value, subValue, change, spark, sparkColor = '#3B82F6', prefix, delay = 0 }: KpiProps) {
  const up = (change ?? 0) >= 0
  return (
    <div className="animate-slide-up" style={{
      background:'var(--bg-surface)', border:'1px solid var(--border-subtle)',
      borderRadius:14, padding:'18px 20px',
      display:'flex', flexDirection:'column', gap:0,
      animationDelay:`${delay}ms`,
      transition:'border-color 200ms, box-shadow 200ms',
      cursor:'default', overflow:'hidden', position:'relative',
    }}
    onMouseEnter={e => {
      const el = e.currentTarget as HTMLDivElement
      el.style.borderColor = 'var(--border-strong)'
      el.style.boxShadow = '0 4px 20px rgba(0,0,0,.3)'
    }}
    onMouseLeave={e => {
      const el = e.currentTarget as HTMLDivElement
      el.style.borderColor = 'var(--border-subtle)'
      el.style.boxShadow = 'none'
    }}>
      {/* Label */}
      <div style={{ fontSize:11, fontWeight:600, letterSpacing:'.06em', textTransform:'uppercase', color:'var(--text-tertiary)', marginBottom:10 }}>
        {label}
      </div>
      {/* Main row */}
      <div style={{ display:'flex', alignItems:'flex-end', justifyContent:'space-between', gap:8 }}>
        <div>
          <div style={{
            fontFamily:'var(--font-display)', fontWeight:700, fontSize:26,
            color:'var(--text-primary)', letterSpacing:'-0.03em', lineHeight:1,
            fontVariantNumeric:'tabular-nums',
          }}>
            {prefix && <span style={{ fontSize:14, color:'var(--text-secondary)', marginRight:2 }}>{prefix}</span>}
            {value}
          </div>
          <div style={{ display:'flex', alignItems:'center', gap:6, marginTop:6 }}>
            {change !== undefined && (
              <span style={{ fontSize:12, fontWeight:600, color: up ? 'var(--bull-strong)' : 'var(--bear-strong)' }}>
                {up ? '▲' : '▼'} {Math.abs(change).toFixed(2)}%
              </span>
            )}
            {subValue && <span style={{ fontSize:12, color:'var(--text-tertiary)' }}>{subValue}</span>}
          </div>
        </div>
        {spark && (
          <div style={{ opacity:.85 }}>
            <Sparkline data={spark} color={sparkColor} />
          </div>
        )}
      </div>
    </div>
  )
}

/* ─── Order status badge ────────────────────────────────────────────────── */
type OStatus = 'Filled'|'Open'|'PartiallyFilled'|'Cancelled'|'Pending'|'Rejected'
const STATUS_MAP: Record<OStatus, { cls: string; label: string }> = {
  Filled:          { cls:'badge-bull',    label:'Filled'    },
  Open:            { cls:'badge-neutral', label:'Open'      },
  PartiallyFilled: { cls:'badge-info',    label:'Partial'   },
  Cancelled:       { cls:'badge-bear',    label:'Cancelled' },
  Pending:         { cls:'badge-warn',    label:'Pending'   },
  Rejected:        { cls:'badge-bear',    label:'Rejected'  },
}

/* ─── Mock order feed ───────────────────────────────────────────────────── */
const BASE_ORDERS = [
  { id:'ORD-9281', symbol:'SQURPHARMA', side:'Buy'  as const, type:'Limit',  qty:500,  price:312.50, status:'Filled'    as OStatus, time:'10:32:44' },
  { id:'ORD-9280', symbol:'BATBC',      side:'Sell' as const, type:'Market', qty:50,   price:716.00, status:'Filled'    as OStatus, time:'10:28:11' },
  { id:'ORD-9279', symbol:'BRAC BANK',  side:'Buy'  as const, type:'Limit',  qty:1000, price:51.20,  status:'Open'      as OStatus, time:'10:15:03' },
  { id:'ORD-9278', symbol:'GPHOUSE',    side:'Sell' as const, type:'Limit',  qty:200,  price:48.80,  status:'PartiallyFilled' as OStatus, time:'09:58:22' },
  { id:'ORD-9277', symbol:'MARICO',     side:'Buy'  as const, type:'Market', qty:100,  price:280.00, status:'Cancelled' as OStatus, time:'09:41:07' },
  { id:'ORD-9276', symbol:'RENATA',     side:'Sell' as const, type:'Limit',  qty:30,   price:1420.00,status:'Pending'   as OStatus, time:'09:30:00' },
]

/* ─── Mini chart bar ────────────────────────────────────────────────────── */
function BarChart({ data, color }: { data: number[]; color: string }) {
  const max = Math.max(...data)
  return (
    <div style={{ display:'flex', alignItems:'flex-end', gap:2, height:32 }}>
      {data.map((v, i) => (
        <div key={i} style={{
          flex:1, borderRadius:'2px 2px 0 0',
          height:`${(v/max)*100}%`,
          background: color,
          opacity: i === data.length-1 ? 1 : 0.4 + (i/data.length)*0.4,
          transition:'height 400ms cubic-bezier(0.16,1,0.3,1)',
        }} />
      ))}
    </div>
  )
}

/* ─── Section Header ────────────────────────────────────────────────────── */
function SectionHeader({ title, action }: { title: string; action?: React.ReactNode }) {
  return (
    <div style={{ display:'flex', alignItems:'center', justifyContent:'space-between', marginBottom:14 }}>
      <h2 style={{ fontFamily:'var(--font-display)', fontWeight:700, fontSize:14, color:'var(--text-primary)', letterSpacing:'-0.01em' }}>
        {title}
      </h2>
      {action}
    </div>
  )
}

/* ─── INDEX TICKER ──────────────────────────────────────────────────────── */
function IndexCard({ label, value, change, pct }: { label:string; value:number; change:number; pct:number }) {
  const up = pct >= 0
  const spark = useRef(generateSpark(value, 24))
  return (
    <div style={{
      background:'var(--bg-surface)', border:'1px solid var(--border-subtle)',
      borderRadius:12, padding:'14px 16px', flex:1, minWidth:0,
    }}>
      <div style={{ fontSize:10.5, fontWeight:700, letterSpacing:'.06em', textTransform:'uppercase', color:'var(--text-tertiary)', marginBottom:8 }}>{label}</div>
      <div style={{ display:'flex', alignItems:'flex-end', gap:10, justifyContent:'space-between' }}>
        <div>
          <div style={{ fontFamily:'var(--font-mono)', fontSize:18, fontWeight:600, color:'var(--text-primary)', fontVariantNumeric:'tabular-nums' }}>
            {value.toLocaleString('en', { minimumFractionDigits:2 })}
          </div>
          <div style={{ fontSize:12, fontWeight:600, marginTop:3, color: up ? 'var(--bull-strong)' : 'var(--bear-strong)' }}>
            {up ? '+' : ''}{change.toFixed(2)} ({up ? '+' : ''}{pct.toFixed(2)}%)
          </div>
        </div>
        <Sparkline data={spark.current} color={up ? 'var(--bull-strong)' : 'var(--bear-strong)'} height={30} />
      </div>
    </div>
  )
}

/* ─── Dashboard Page ────────────────────────────────────────────────────── */
export function DashboardPage() {
  const user = useAuthStore(s => s.user)
  const [_tick, setTick] = useState(0)
  const sparks = useRef({
    portfolio: generateSpark(2481500, 20),
    pnl:       generateSpark(56820, 20),
    volume:    generateSpark(18425000, 20),
  })

  // Simulate live updates
  useEffect(() => {
    const id = setInterval(() => setTick(t => t + 1), 3000)
    return () => clearInterval(id)
  }, [])

  const greeting = (() => {
    const h = new Date().getHours()
    return h < 12 ? 'Good morning' : h < 17 ? 'Good afternoon' : 'Good evening'
  })()

  return (
    <div style={{ maxWidth:1400, margin:'0 auto', display:'flex', flexDirection:'column', gap:24 }}>

      {/* Welcome row */}
      <div className="animate-fade-in" style={{ display:'flex', alignItems:'center', justifyContent:'space-between', gap:16 }}>
        <div>
          <h1 style={{
            fontFamily:'var(--font-display)', fontWeight:800, fontSize:22,
            color:'var(--text-primary)', letterSpacing:'-0.02em', lineHeight:1.1,
          }}>
            {greeting},{' '}
            <span style={{ background:'linear-gradient(90deg, var(--accent-300), var(--accent-500))', WebkitBackgroundClip:'text', WebkitTextFillColor:'transparent' }}>
              {user?.email?.split('@')[0] ?? 'trader'}
            </span>
          </h1>
          <p style={{ fontSize:13, color:'var(--text-secondary)', marginTop:4 }}>
            {new Date().toLocaleDateString('en-GB', { weekday:'long', year:'numeric', month:'long', day:'numeric' })} · Session active
          </p>
        </div>
        <div style={{ display:'flex', gap:8 }}>
          <button className="btn btn-primary btn-lg" style={{ gap:7 }}>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            New Order
          </button>
          <button className="btn btn-secondary btn-lg">Quick Trade</button>
        </div>
      </div>

      {/* Market indices */}
      <div className="animate-slide-up delay-50" style={{ display:'flex', gap:12, overflow:'auto' }} >
        <IndexCard label="DSEX"  value={6248.32} change={42.18}  pct={0.68}  />
        <IndexCard label="DSES"  value={1358.94} change={12.44}  pct={0.92}  />
        <IndexCard label="DS30"  value={2214.56} change={-18.22} pct={-0.81} />
        <IndexCard label="CSCX"  value={11480.20}change={84.15}  pct={0.74}  />
      </div>

      {/* KPI Grid */}
      <div style={{ display:'grid', gridTemplateColumns:'repeat(auto-fit, minmax(220px, 1fr))', gap:14 }}>
        <KpiCard label="Portfolio Value"  value="24,81,500" prefix="৳" change={2.34}  spark={sparks.current.portfolio} sparkColor="var(--accent-500)"   delay={0}   />
        <KpiCard label="Today's P&L"      value="56,820"    prefix="৳" change={2.34}  spark={sparks.current.pnl}       sparkColor="var(--bull-strong)"  delay={60}  />
        <KpiCard label="Open Orders"      value="3"         subValue="2 Buy · 1 Sell"                                                                    delay={120} />
        <KpiCard label="Cash Available"   value="3,18,200"  prefix="৳" subValue="12.8% of portfolio"                                                     delay={180} />
      </div>

      {/* Main content grid */}
      <div style={{ display:'grid', gridTemplateColumns:'1fr 320px', gap:20 }}>

        {/* Orders table */}
        <div className="animate-slide-up delay-200" style={{ background:'var(--bg-surface)', border:'1px solid var(--border-subtle)', borderRadius:14, overflow:'hidden' }}>
          <div style={{ padding:'16px 20px', borderBottom:'1px solid var(--border-subtle)', display:'flex', alignItems:'center', justifyContent:'space-between' }}>
            <SectionHeader title="Recent Orders" />
            <div style={{ display:'flex', gap:6 }}>
              {(['All','Open','Filled','Cancelled'] as const).map((f, i) => (
                <button key={f} className="btn btn-ghost btn-sm"
                  style={{
                    height:26, fontSize:11, padding:'0 8px',
                    background: i===0 ? 'var(--accent-glow)' : undefined,
                    color: i===0 ? 'var(--accent-300)' : undefined,
                  }}>
                  {f}
                </button>
              ))}
            </div>
          </div>
          <div style={{ overflowX:'auto' }}>
            <table className="data-table">
              <thead>
                <tr>
                  {['Order ID','Symbol','Side','Type','Qty','Price (৳)','Status','Time'].map(h => (
                    <th key={h}>{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {BASE_ORDERS.map((o, i) => {
                  const s = STATUS_MAP[o.status]
                  return (
                    <tr key={o.id} className={`animate-fade-in delay-${Math.min(i*50,300)}`}>
                      <td><span className="mono" style={{ color:'var(--accent-400)', fontSize:12 }}>{o.id}</span></td>
                      <td><span style={{ fontWeight:600, fontSize:12.5, letterSpacing:'.01em' }}>{o.symbol}</span></td>
                      <td>
                        <span style={{ fontWeight:700, fontSize:12, color: o.side==='Buy' ? 'var(--bull-strong)' : 'var(--bear-strong)' }}>
                          {o.side}
                        </span>
                      </td>
                      <td><span style={{ color:'var(--text-secondary)', fontSize:12 }}>{o.type}</span></td>
                      <td><span className="mono">{o.qty.toLocaleString()}</span></td>
                      <td><span className="mono" style={{ fontWeight:500 }}>৳{o.price.toLocaleString('en',{minimumFractionDigits:2})}</span></td>
                      <td><span className={`badge ${s.cls}`}>{s.label}</span></td>
                      <td><span className="mono" style={{ color:'var(--text-tertiary)', fontSize:11.5 }}>{o.time}</span></td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
          <div style={{ padding:'10px 20px', borderTop:'1px solid var(--border-subtle)', display:'flex', justifyContent:'space-between', alignItems:'center' }}>
            <span style={{ fontSize:12, color:'var(--text-tertiary)' }}>Showing 6 of 142 orders</span>
            <button className="btn btn-ghost btn-sm">View all orders →</button>
          </div>
        </div>

        {/* Right sidebar */}
        <div style={{ display:'flex', flexDirection:'column', gap:14 }}>

          {/* Top movers */}
          <div className="animate-slide-up delay-250" style={{ background:'var(--bg-surface)', border:'1px solid var(--border-subtle)', borderRadius:14, overflow:'hidden' }}>
            <div style={{ padding:'14px 16px', borderBottom:'1px solid var(--border-subtle)' }}>
              <SectionHeader title="Top Movers" />
            </div>
            <div style={{ padding:'8px 0' }}>
              {[
                { symbol:'SQURPHARMA', price:312.50,  pct:4.82  },
                { symbol:'BATBC',      price:716.00,  pct:3.21  },
                { symbol:'LHBL',       price:42.30,   pct:2.92  },
                { symbol:'RENATA',     price:1420.00, pct:-1.52 },
                { symbol:'GPHOUSE',    price:48.80,   pct:-2.87 },
                { symbol:'DUTCHBANGL', price:78.60,   pct:-3.21 },
              ].map(m => (
                <div key={m.symbol} style={{
                  display:'flex', alignItems:'center', padding:'7px 16px', gap:10,
                  transition:'background 80ms',
                }}
                onMouseEnter={e => { (e.currentTarget as HTMLDivElement).style.background = 'var(--bg-hover)' }}
                onMouseLeave={e => { (e.currentTarget as HTMLDivElement).style.background = 'transparent' }}>
                  <div style={{ flex:1, minWidth:0 }}>
                    <div style={{ fontSize:12.5, fontWeight:600, color:'var(--text-primary)' }}>{m.symbol}</div>
                    <div className="mono" style={{ fontSize:11.5, color:'var(--text-secondary)', marginTop:1 }}>৳{m.price.toLocaleString('en',{minimumFractionDigits:2})}</div>
                  </div>
                  <div style={{ textAlign:'right' }}>
                    <div style={{ fontSize:13, fontWeight:700, color: m.pct>=0 ? 'var(--bull-strong)' : 'var(--bear-strong)' }}>
                      {m.pct>=0?'+':''}{m.pct.toFixed(2)}%
                    </div>
                    <div style={{ marginTop:3 }}>
                      <BarChart
                        data={Array.from({length:7},() => Math.random())}
                        color={m.pct>=0 ? 'var(--bull-base)' : 'var(--bear-base)'}
                      />
                    </div>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Quick order pad */}
          <div className="animate-slide-up delay-300" style={{ background:'var(--bg-surface)', border:'1px solid var(--border-subtle)', borderRadius:14, overflow:'hidden' }}>
            <div style={{ padding:'14px 16px', borderBottom:'1px solid var(--border-subtle)' }}>
              <SectionHeader title="Quick Order" />
            </div>
            <div style={{ padding:'14px 16px', display:'flex', flexDirection:'column', gap:12 }}>
              {/* Symbol */}
              <div>
                <label style={{ fontSize:11, fontWeight:600, letterSpacing:'.05em', textTransform:'uppercase', color:'var(--text-tertiary)', display:'block', marginBottom:5 }}>Symbol</label>
                <input className="input" placeholder="e.g. BATBC" defaultValue="SQURPHARMA" />
              </div>
              {/* Side */}
              <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:6 }}>
                <button className="btn btn-success btn-lg" style={{ width:'100%', fontFamily:'var(--font-display)', fontWeight:700, fontSize:15 }}>
                  ▲ Buy
                </button>
                <button className="btn btn-danger btn-lg" style={{ width:'100%', fontFamily:'var(--font-display)', fontWeight:700, fontSize:15 }}>
                  ▼ Sell
                </button>
              </div>
              {/* Qty + Price */}
              <div style={{ display:'grid', gridTemplateColumns:'1fr 1fr', gap:8 }}>
                <div>
                  <label style={{ fontSize:11, fontWeight:600, letterSpacing:'.05em', textTransform:'uppercase', color:'var(--text-tertiary)', display:'block', marginBottom:5 }}>Quantity</label>
                  <input className="input" type="number" placeholder="100" />
                </div>
                <div>
                  <label style={{ fontSize:11, fontWeight:600, letterSpacing:'.05em', textTransform:'uppercase', color:'var(--text-tertiary)', display:'block', marginBottom:5 }}>Price (৳)</label>
                  <input className="input" type="number" placeholder="312.50" />
                </div>
              </div>
              <button className="btn btn-primary" style={{ width:'100%' }}>Place Order</button>
            </div>
          </div>
        </div>
      </div>

      {/* Bottom stats row */}
      <div className="animate-slide-up delay-400" style={{ display:'grid', gridTemplateColumns:'repeat(auto-fit, minmax(200px, 1fr))', gap:14, paddingBottom:8 }}>
        {[
          { label:'Total Trades Today', value:'24', sub:'↑ 8 vs yesterday', color:'var(--accent-400)' },
          { label:'Avg Fill Rate',      value:'98.2%', sub:'Last 30 days',     color:'var(--bull-strong)' },
          { label:'Rejected Orders',    value:'0',     sub:'Zero today',       color:'var(--bull-strong)' },
          { label:'Pending Settlement', value:'৳4,82,000', sub:'T+2 due',      color:'var(--warn-base)' },
        ].map((s,i) => (
          <div key={i} style={{ background:'var(--bg-surface)', border:'1px solid var(--border-subtle)', borderRadius:12, padding:'14px 16px' }}>
            <div style={{ fontSize:10.5, fontWeight:700, letterSpacing:'.06em', textTransform:'uppercase', color:'var(--text-tertiary)', marginBottom:8 }}>{s.label}</div>
            <div style={{ fontFamily:'var(--font-display)', fontWeight:700, fontSize:22, color:s.color, letterSpacing:'-0.02em', fontVariantNumeric:'tabular-nums' }}>{s.value}</div>
            <div style={{ fontSize:11.5, color:'var(--text-tertiary)', marginTop:4 }}>{s.sub}</div>
          </div>
        ))}
      </div>
    </div>
  )
}
