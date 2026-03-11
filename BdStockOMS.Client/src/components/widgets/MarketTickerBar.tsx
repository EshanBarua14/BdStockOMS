import { useState, useEffect, useRef } from 'react'

interface TickerItem {
  symbol: string
  price: number
  change: number
  pct: number
}

const INITIAL_TICKERS: TickerItem[] = [
  { symbol: 'DSEX',       price: 6248.32, change:  42.18, pct:  0.68 },
  { symbol: 'DSES',       price: 1358.94, change:  12.44, pct:  0.92 },
  { symbol: 'DS30',       price: 2214.56, change: -18.22, pct: -0.81 },
  { symbol: 'SQURPHARMA', price: 312.50,  change:   8.90, pct:  2.93 },
  { symbol: 'BATBC',      price: 716.00,  change: -12.40, pct: -1.70 },
  { symbol: 'BRAC BANK',  price: 51.20,   change:   0.80, pct:  1.59 },
  { symbol: 'GPHOUSE',    price: 48.80,   change:  -0.60, pct: -1.21 },
  { symbol: 'MARICO',     price: 280.00,  change:   3.50, pct:  1.27 },
  { symbol: 'ISLAMIBANK', price: 37.90,   change:   0.40, pct:  1.07 },
  { symbol: 'DUTCHBANGL', price: 78.60,   change:  -1.10, pct: -1.38 },
  { symbol: 'LHBL',       price: 42.30,   change:   1.20, pct:  2.92 },
  { symbol: 'RENATA',     price: 1420.00, change: -22.00, pct: -1.52 },
]

function TickerItem({ item }: { item: TickerItem }) {
  const isUp = item.change >= 0
  return (
    <span style={{
      display: 'inline-flex', alignItems: 'center', gap: 6,
      paddingRight: 24, flexShrink: 0,
      fontSize: 11.5, fontFamily: 'var(--font-mono)',
    }}>
      <span style={{ color: 'var(--text-primary)', fontWeight: 600, fontFamily: 'var(--font-ui)', fontSize: 11 }}>
        {item.symbol}
      </span>
      <span style={{ color: 'var(--text-secondary)' }}>
        ৳{item.price.toLocaleString('en', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
      </span>
      <span style={{ color: isUp ? 'var(--bull-strong)' : 'var(--bear-strong)', fontWeight: 600 }}>
        {isUp ? '+' : ''}{item.pct.toFixed(2)}%
      </span>
      <span style={{ color: 'var(--border-strong)', userSelect: 'none' }}>·</span>
    </span>
  )
}

export function MarketTickerBar() {
  const [tickers, setTickers] = useState(INITIAL_TICKERS)
  const trackRef = useRef<HTMLDivElement>(null)
  const [paused, setPaused] = useState(false)

  // Simulate live price updates
  useEffect(() => {
    const id = setInterval(() => {
      setTickers(prev => prev.map(t => {
        const delta  = (Math.random() - 0.48) * t.price * 0.002
        const price  = Math.max(0.01, +(t.price + delta).toFixed(2))
        const change = +(t.change + delta).toFixed(2)
        const pct    = +((change / (price - change)) * 100).toFixed(2)
        return { ...t, price, change, pct }
      }))
    }, 2500)
    return () => clearInterval(id)
  }, [])

  const doubled = [...tickers, ...tickers]

  return (
    <div
      style={{
        height: 28,
        background: 'var(--bg-elevated)',
        borderBottom: '1px solid var(--border-subtle)',
        display: 'flex', alignItems: 'center',
        overflow: 'hidden',
        position: 'relative',
      }}
      onMouseEnter={() => setPaused(true)}
      onMouseLeave={() => setPaused(false)}
    >
      {/* Left fade */}
      <div style={{
        position: 'absolute', left: 0, top: 0, width: 40, height: '100%', zIndex: 2,
        background: 'linear-gradient(to right, var(--bg-elevated), transparent)',
        pointerEvents: 'none',
      }} />
      {/* Right fade */}
      <div style={{
        position: 'absolute', right: 0, top: 0, width: 40, height: '100%', zIndex: 2,
        background: 'linear-gradient(to left, var(--bg-elevated), transparent)',
        pointerEvents: 'none',
      }} />

      {/* Scrolling track */}
      <div
        ref={trackRef}
        style={{
          display: 'flex', alignItems: 'center',
          animation: `tickerScroll 60s linear infinite`,
          animationPlayState: paused ? 'paused' : 'running',
          paddingLeft: 16,
          willChange: 'transform',
        }}
      >
        {doubled.map((item, i) => (
          <TickerItem key={`${item.symbol}-${i}`} item={item} />
        ))}
      </div>
    </div>
  )
}
