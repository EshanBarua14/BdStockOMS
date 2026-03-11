// @ts-nocheck
import { ThemeMenu } from '@/components/ui/ThemeMenu'
import { useState, useEffect, useRef, useCallback } from 'react'
import { Link } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'

// ── DSE/CSE real tickers ──────────────────────────────────────────────────
const DSE_TICKERS = [
  { sym: 'DSEX',  price: 6248.30, chg: +0.68 },
  { sym: 'DSES',  price: 1312.45, chg: +0.42 },
  { sym: 'DS30',  price: 2187.60, chg: +0.55 },
  { sym: 'BATBC', price:  716.20, chg: -0.31 },
  { sym: 'BRAC',  price:   54.80, chg: +1.12 },
  { sym: 'GRAMEENPHONE', price: 285.60, chg: -0.18 },
  { sym: 'SQPHARMA',    price: 231.40, chg: +0.94 },
  { sym: 'RENATA',      price: 958.20, chg: +0.27 },
  { sym: 'WALTONHIL',   price: 987.50, chg: +1.43 },
  { sym: 'ISLAMIBANK',  price:  32.60, chg: -0.61 },
  { sym: 'DUTCHBANGL',  price: 112.30, chg: +0.88 },
  { sym: 'MARICO',      price: 180.40, chg: -0.22 },
  { sym: 'BXPHARMA',    price:  22.10, chg: +2.31 },
  { sym: 'CSEALL',      price: 18420.5, chg: +0.39 },
  { sym: 'CSE30',       price:  9841.2, chg: +0.51 },
]

interface Particle {
  x: number; y: number; vx: number; vy: number
  alpha: number; radius: number; color: string; type: 'node' | 'tick'
  label?: string; price?: string; chg?: number; age: number; maxAge: number
}

interface CandleBar {
  x: number; open: number; close: number; high: number; low: number
  width: number; speed: number; color: string
}

// ── Canvas: scrolling candlesticks + floating ticker nodes ────────────────
function useDseCanvas(canvasRef: React.RefObject<HTMLCanvasElement | null>) {
  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')!
    let raf: number

    const resize = () => {
      canvas.width  = window.innerWidth
      canvas.height = window.innerHeight
    }
    resize()
    window.addEventListener('resize', resize)

    // Candlestick chart rows (3 rows scrolling left)
    const rows: CandleBar[][] = [[], [], []]
    const rowY = [0.18, 0.50, 0.78]
    const rowAmp = [60, 45, 55]

    function makeCandle(rowIdx: number): CandleBar {
      const bullish = Math.random() > 0.45
      const base    = 200 + Math.random() * 100
      const body    = 8 + Math.random() * 28
      const wick    = 4 + Math.random() * 16
      return {
        x:     canvas.width + 40,
        open:  rowY[rowIdx] * canvas.height + (Math.random() - 0.5) * rowAmp[rowIdx],
        close: rowY[rowIdx] * canvas.height + (Math.random() - 0.5) * rowAmp[rowIdx],
        high:  rowY[rowIdx] * canvas.height - wick - Math.random() * 10,
        low:   rowY[rowIdx] * canvas.height + wick + Math.random() * 10,
        width: 6 + Math.random() * 8,
        speed: 0.6 + Math.random() * 0.5,
        color: bullish ? 'rgba(0,212,170,' : 'rgba(255,107,107,',
      }
    }

    rows.forEach((row, i) => {
      for (let x = 0; x < canvas.width; x += 22) {
        const c = makeCandle(i)
        c.x = x
        row.push(c)
      }
    })

    // Floating ticker particles
    const particles: Particle[] = []

    function spawnTicker() {
      const t = DSE_TICKERS[Math.floor(Math.random() * DSE_TICKERS.length)]
      particles.push({
        x:      Math.random() * canvas.width,
        y:      canvas.height + 20,
        vx:     (Math.random() - 0.5) * 0.4,
        vy:     -(0.3 + Math.random() * 0.5),
        alpha:  0,
        radius: 28 + Math.random() * 12,
        color:  t.chg >= 0 ? '#00D4AA' : '#FF6B6B',
        type:   'tick',
        label:  t.sym,
        price:  t.price.toFixed(2),
        chg:    t.chg,
        age:    0,
        maxAge: 280 + Math.random() * 120,
      })
    }

    // Institution labels
    const LABELS = ['DSE', 'CSE', 'BSEC', 'CDBL', 'BB', 'SEC']
    function spawnLabel() {
      const lbl = LABELS[Math.floor(Math.random() * LABELS.length)]
      particles.push({
        x:      Math.random() * canvas.width,
        y:      Math.random() * canvas.height,
        vx:     (Math.random() - 0.5) * 0.15,
        vy:     (Math.random() - 0.5) * 0.15,
        alpha:  0,
        radius: 0,
        color:  'rgba(255,255,255,0.06)',
        type:   'node',
        label:  lbl,
        age:    0,
        maxAge: 400 + Math.random() * 200,
      })
    }

    // Seed initial
    for (let i = 0; i < 6; i++) spawnTicker()
    for (let i = 0; i < 4; i++) spawnLabel()

    let frame = 0
    const tick = () => {
      frame++
      const W = canvas.width
      const H = canvas.height

      // Background
      ctx.fillStyle = 'rgba(8,12,20,0.92)'
      ctx.fillRect(0, 0, W, H)

      // Grid lines (subtle)
      ctx.strokeStyle = 'rgba(255,255,255,0.03)'
      ctx.lineWidth = 1
      for (let y = 0; y < H; y += 60) {
        ctx.beginPath(); ctx.moveTo(0, y); ctx.lineTo(W, y); ctx.stroke()
      }
      for (let x = 0; x < W; x += 80) {
        ctx.beginPath(); ctx.moveTo(x, 0); ctx.lineTo(x, H); ctx.stroke()
      }

      // Draw candlestick rows
      rows.forEach((row, ri) => {
        row.forEach((c, ci) => {
          c.x -= c.speed
          if (c.x < -20) {
            const nc = makeCandle(ri)
            row[ci] = nc
            return
          }
          const alpha = Math.min(1, Math.min(c.x / 60, (W - c.x) / 60)) * 0.55

          // Wick
          ctx.strokeStyle = c.color + alpha * 0.7 + ')'
          ctx.lineWidth = 1
          ctx.beginPath()
          ctx.moveTo(c.x, c.high)
          ctx.lineTo(c.x, c.low)
          ctx.stroke()

          // Body
          const top    = Math.min(c.open, c.close)
          const height = Math.max(Math.abs(c.close - c.open), 3)
          ctx.fillStyle = c.color + alpha + ')'
          ctx.fillRect(c.x - c.width / 2, top, c.width, height)
        })
      })

      // Draw ticker particles
      particles.forEach((p, i) => {
        p.x  += p.vx
        p.y  += p.vy
        p.age++

        const fadeIn  = Math.min(p.age / 40, 1)
        const fadeOut = Math.min((p.maxAge - p.age) / 40, 1)
        p.alpha = fadeIn * fadeOut

        if (p.type === 'tick') {
          // Glow ring
          const grad = ctx.createRadialGradient(p.x, p.y, 0, p.x, p.y, p.radius)
          grad.addColorStop(0, p.color + '18')
          grad.addColorStop(1, 'transparent')
          ctx.fillStyle = grad
          ctx.beginPath()
          ctx.arc(p.x, p.y, p.radius, 0, Math.PI * 2)
          ctx.fill()

          // Border circle
          ctx.strokeStyle = p.color + Math.floor(p.alpha * 120).toString(16).padStart(2,'0')
          ctx.lineWidth = 1
          ctx.beginPath()
          ctx.arc(p.x, p.y, p.radius - 2, 0, Math.PI * 2)
          ctx.stroke()

          // Symbol
          ctx.font = 'bold 8px "Space Mono", monospace'
          ctx.fillStyle = p.color + Math.floor(p.alpha * 220).toString(16).padStart(2,'0')
          ctx.textAlign = 'center'
          ctx.fillText(p.label ?? '', p.x, p.y - 4)

          // Price
          ctx.font = '7px "Space Mono", monospace'
          ctx.fillStyle = 'rgba(255,255,255,' + p.alpha * 0.7 + ')'
          ctx.fillText('৳' + (p.price ?? ''), p.x, p.y + 5)

          // Change
          const chg = p.chg ?? 0
          ctx.font = '7px "Space Mono", monospace'
          ctx.fillStyle = p.color + Math.floor(p.alpha * 180).toString(16).padStart(2,'0')
          ctx.fillText((chg >= 0 ? '+' : '') + chg.toFixed(2) + '%', p.x, p.y + 14)
        } else {
          // Institution label ghost
          ctx.font = 'bold 48px "Outfit", sans-serif'
          ctx.fillStyle = 'rgba(255,255,255,' + p.alpha * 0.04 + ')'
          ctx.textAlign = 'center'
          ctx.fillText(p.label ?? '', p.x, p.y)
        }

        if (p.age >= p.maxAge) {
          if (p.type === 'tick') spawnTicker()
          else spawnLabel()
          particles.splice(i, 1)
        }
      })

      // Connecting lines between nearby tickers
      const ticks = particles.filter(p => p.type === 'tick')
      for (let a = 0; a < ticks.length; a++) {
        for (let b = a + 1; b < ticks.length; b++) {
          const dx   = ticks[a].x - ticks[b].x
          const dy   = ticks[a].y - ticks[b].y
          const dist = Math.sqrt(dx * dx + dy * dy)
          if (dist < 180) {
            const alpha = (1 - dist / 180) * 0.08
            ctx.strokeStyle = `rgba(0,212,170,${alpha})`
            ctx.lineWidth = 0.5
            ctx.beginPath()
            ctx.moveTo(ticks[a].x, ticks[a].y)
            ctx.lineTo(ticks[b].x, ticks[b].y)
            ctx.stroke()
          }
        }
      }

      // Spawn more particles over time
      if (frame % 90 === 0 && particles.filter(p => p.type === 'tick').length < 12) spawnTicker()
      if (frame % 150 === 0 && particles.filter(p => p.type === 'node').length < 5) spawnLabel()

      raf = requestAnimationFrame(tick)
    }
    tick()
    return () => {
      cancelAnimationFrame(raf)
      window.removeEventListener('resize', resize)
    }
  }, [canvasRef])
}

// ── LoginPage ─────────────────────────────────────────────────────────────
export function LoginPage() {
  const { login, isLoading, error, clearError } = useAuth()
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const [email, setEmail]       = useState('')
  const [password, setPassword] = useState('')
  const [showPass, setShowPass] = useState(false)
  const [focused, setFocused]   = useState<string | null>(null)

  useDseCanvas(canvasRef)

  const handleSubmit = useCallback(async (e: React.FormEvent) => {
    e.preventDefault()
    clearError()
    await login({ email, password })
  }, [login, email, password, clearError])

  return (
    <div style={{ position: 'relative', minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', fontFamily: "'Outfit', sans-serif", overflow: 'hidden', background: '#080C14' }}>

      {/* Canvas background */}
      <canvas ref={canvasRef} style={{ position: 'absolute', inset: 0, zIndex: 0 }} />

      {/* Top bar — institution logos */}
      <div style={{ position: 'absolute', top: 0, left: 0, right: 0, zIndex: 2, display: 'flex', alignItems: 'center', justifyContent: 'space-between', padding: '16px 32px', borderBottom: '1px solid rgba(255,255,255,0.05)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          {/* Hex logo */}
          <svg width="28" height="28" viewBox="0 0 28 28">
            <polygon points="14,2 24,8 24,20 14,26 4,20 4,8" fill="none" stroke="#00D4AA" strokeWidth="1.5" />
            <polygon points="14,6 21,10 21,18 14,22 7,18 7,10" fill="rgba(0,212,170,0.1)" stroke="#00D4AA" strokeWidth="0.5" />
            <text x="14" y="17" textAnchor="middle" fill="#00D4AA" fontSize="7" fontFamily="Space Mono" fontWeight="bold">OMS</text>
          </svg>
          <span style={{ color: '#fff', fontWeight: 700, fontSize: 15, letterSpacing: '0.04em' }}>BD Stock OMS</span>
        </div>
        <div style={{ display: 'flex', gap: 20 }}>
          <ThemeMenu variant="compact" />
          {['DSE', 'CSE', 'BSEC', 'CDBL'].map(inst => (
            <span key={inst} style={{ color: 'rgba(255,255,255,0.3)', fontSize: 11, fontFamily: "'Space Mono', monospace", letterSpacing: '0.1em' }}>{inst}</span>
          ))}
        </div>
      </div>

      {/* Login card */}
      <div style={{ position: 'relative', zIndex: 2, width: '100%', maxWidth: 420, margin: '0 16px' }}>
        <div style={{
          background: 'rgba(13,19,32,0.85)',
          backdropFilter: 'blur(24px)',
          border: '1px solid rgba(0,212,170,0.18)',
          borderRadius: 16,
          padding: '40px 36px',
          boxShadow: '0 0 60px rgba(0,212,170,0.06), 0 24px 48px rgba(0,0,0,0.5)',
        }}>
          {/* Header */}
          <div style={{ textAlign: 'center', marginBottom: 32 }}>
            <div style={{ display: 'inline-flex', alignItems: 'center', justifyContent: 'center', width: 52, height: 52, borderRadius: '50%', background: 'rgba(0,212,170,0.1)', border: '1px solid rgba(0,212,170,0.3)', marginBottom: 16 }}>
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
                <path d="M3 17l4-8 4 5 3-3 4 6" stroke="#00D4AA" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="19" cy="5" r="2" fill="#00D4AA" opacity="0.6"/>
              </svg>
            </div>
            <h1 style={{ color: '#fff', fontSize: 22, fontWeight: 700, margin: '0 0 6px', letterSpacing: '-0.02em' }}>Welcome back</h1>
            <p style={{ color: 'rgba(255,255,255,0.4)', fontSize: 13, margin: 0 }}>
              Bangladesh Securities & Exchange Commission certified platform
            </p>
          </div>

          {/* Error */}
          {error && (
            <div style={{ background: 'rgba(255,107,107,0.1)', border: '1px solid rgba(255,107,107,0.3)', borderRadius: 8, padding: '10px 14px', marginBottom: 20, display: 'flex', alignItems: 'center', gap: 8 }}>
              <svg width="14" height="14" viewBox="0 0 14 14" fill="none"><circle cx="7" cy="7" r="6" stroke="#FF6B6B" strokeWidth="1.2"/><path d="M7 4v3M7 9.5v.5" stroke="#FF6B6B" strokeWidth="1.2" strokeLinecap="round"/></svg>
              <span style={{ color: '#FF6B6B', fontSize: 13 }}>{error}</span>
            </div>
          )}

          {/* Form */}
          <form onSubmit={handleSubmit} autoComplete="off">
            {/* Email */}
            <div style={{ marginBottom: 16 }}>
              <label style={{ display: 'block', color: 'rgba(255,255,255,0.5)', fontSize: 12, marginBottom: 6, letterSpacing: '0.06em', textTransform: 'uppercase' }}>Email</label>
              <div style={{ position: 'relative' }}>
                <input
                  type="email"
                  value={email}
                  onChange={e => setEmail(e.target.value)}
                  onFocus={() => setFocused('email')}
                  onBlur={() => setFocused(null)}
                  placeholder="your@email.com"
                  required
                  style={{
                    width: '100%', boxSizing: 'border-box',
                    background: 'rgba(255,255,255,0.04)',
                    border: `1px solid ${focused === 'email' ? 'rgba(0,212,170,0.6)' : 'rgba(255,255,255,0.1)'}`,
                    borderRadius: 8, padding: '11px 14px',
                    color: '#fff', fontSize: 14, outline: 'none',
                    transition: 'border-color 0.2s',
                  }}
                />
              </div>
            </div>

            {/* Password */}
            <div style={{ marginBottom: 24 }}>
              <label style={{ display: 'block', color: 'rgba(255,255,255,0.5)', fontSize: 12, marginBottom: 6, letterSpacing: '0.06em', textTransform: 'uppercase' }}>Password</label>
              <div style={{ position: 'relative' }}>
                <input
                  type={showPass ? 'text' : 'password'}
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  onFocus={() => setFocused('password')}
                  onBlur={() => setFocused(null)}
                  placeholder="••••••••"
                  required
                  style={{
                    width: '100%', boxSizing: 'border-box',
                    background: 'rgba(255,255,255,0.04)',
                    border: `1px solid ${focused === 'password' ? 'rgba(0,212,170,0.6)' : 'rgba(255,255,255,0.1)'}`,
                    borderRadius: 8, padding: '11px 40px 11px 14px',
                    color: '#fff', fontSize: 14, outline: 'none',
                    transition: 'border-color 0.2s',
                  }}
                />
                <button type="button" onClick={() => setShowPass(v => !v)}
                  style={{ position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)', background: 'none', border: 'none', cursor: 'pointer', color: 'rgba(255,255,255,0.3)', padding: 0 }}>
                  {showPass
                    ? <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/><path d="M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/><line x1="1" y1="1" x2="23" y2="23" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>
                    : <svg width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z" stroke="currentColor" strokeWidth="1.5"/><circle cx="12" cy="12" r="3" stroke="currentColor" strokeWidth="1.5"/></svg>
                  }
                </button>
              </div>
            </div>

            {/* Submit */}
            <button
              type="submit"
              disabled={isLoading}
              style={{
                width: '100%', padding: '12px',
                background: isLoading ? 'rgba(0,212,170,0.3)' : 'linear-gradient(135deg, #00D4AA 0%, #00B894 100%)',
                border: 'none', borderRadius: 8, color: '#0A1628',
                fontSize: 14, fontWeight: 700, cursor: isLoading ? 'not-allowed' : 'pointer',
                letterSpacing: '0.04em', transition: 'opacity 0.2s',
                boxShadow: isLoading ? 'none' : '0 4px 20px rgba(0,212,170,0.3)',
              }}
            >
              {isLoading ? 'Signing in…' : 'Sign in to OMS'}
            </button>
          </form>

          {/* Footer links */}
          <div style={{ marginTop: 24, textAlign: 'center' }}>
            <p style={{ color: 'rgba(255,255,255,0.3)', fontSize: 13, margin: '0 0 12px' }}>
              New brokerage?{' '}
              <Link to="/signup" style={{ color: '#00D4AA', textDecoration: 'none', fontWeight: 600 }}>Register your firm</Link>
            </p>
            <p style={{ color: 'rgba(255,255,255,0.18)', fontSize: 11, margin: 0, lineHeight: 1.6 }}>
              {'Built by '}
              <a href="https://www.linkedin.com/in/eshanbarua" target="_blank" rel="noopener noreferrer"
                style={{ color: 'rgba(0,212,170,0.6)', textDecoration: 'none', fontWeight: 600 }}>
                Eshan Barua
              </a>
              {' · BD Stock OMS v2 · BSEC Regulated'}
            </p>
          </div>
        </div>

        {/* Market status strip */}
        <div style={{ marginTop: 12, display: 'flex', justifyContent: 'center', gap: 24 }}>
          {[
            { label: 'DSEX', value: '6,248.30', up: true },
            { label: 'CSE30', value: '9,841.20', up: true },
            { label: 'CDBL', value: 'Active', up: true },
          ].map(item => (
            <div key={item.label} style={{ textAlign: 'center' }}>
              <div style={{ color: 'rgba(255,255,255,0.3)', fontSize: 10, fontFamily: "'Space Mono', monospace", letterSpacing: '0.08em' }}>{item.label}</div>
              <div style={{ color: item.up ? '#00D4AA' : '#FF6B6B', fontSize: 12, fontFamily: "'Space Mono', monospace", fontWeight: 700 }}>{item.value}</div>
            </div>
          ))}
        </div>
      </div>
    </div>
  )
}

export default LoginPage
