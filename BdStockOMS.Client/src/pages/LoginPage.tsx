import { useState, FormEvent, useEffect, useRef } from 'react'
import { Navigate } from 'react-router-dom'
import { useAuth } from '@/hooks/useAuth'
import { useThemeStore, THEMES, ACCENTS, type ThemeId, type AccentId } from '@/store/themeStore'
import { Logo } from '@/components/ui/Logo'

/* ─── Animated background grid ──────────────────────────────────────────── */
function AnimatedGrid() {
  const canvasRef = useRef<HTMLCanvasElement>(null)

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return
    const ctx = canvas.getContext('2d')
    if (!ctx) return

    let frame = 0
    let raf: number

    function resize() {
      canvas!.width  = window.innerWidth
      canvas!.height = window.innerHeight
    }
    resize()
    window.addEventListener('resize', resize)

    function draw() {
      const w = canvas!.width, h = canvas!.height
      ctx!.clearRect(0, 0, w, h)

      // Subtle grid
      ctx!.strokeStyle = 'rgba(255,255,255,0.028)'
      ctx!.lineWidth = 0.5
      const step = 64
      for (let x = 0; x < w; x += step) {
        ctx!.beginPath(); ctx!.moveTo(x, 0); ctx!.lineTo(x, h); ctx!.stroke()
      }
      for (let y = 0; y < h; y += step) {
        ctx!.beginPath(); ctx!.moveTo(0, y); ctx!.lineTo(w, y); ctx!.stroke()
      }

      // Flowing particles
      const t = frame * 0.003
      for (let i = 0; i < 28; i++) {
        const seed = i * 137.508
        const x = ((Math.sin(seed * 0.1 + t * 0.5) * 0.5 + 0.5) * w * 1.2) - w * 0.1
        const y = ((Math.cos(seed * 0.07 + t * 0.3) * 0.5 + 0.5) * h * 1.2) - h * 0.1
        const size = 1 + Math.sin(seed + t) * 0.5
        const opacity = 0.08 + Math.sin(seed * 2 + t) * 0.05
        ctx!.beginPath()
        ctx!.arc(x, y, size, 0, Math.PI * 2)
        ctx!.fillStyle = `rgba(59,130,246,${opacity})`
        ctx!.fill()
      }

      frame++
      raf = requestAnimationFrame(draw)
    }
    draw()

    return () => {
      cancelAnimationFrame(raf)
      window.removeEventListener('resize', resize)
    }
  }, [])

  return (
    <canvas
      ref={canvasRef}
      aria-hidden
      style={{
        position: 'fixed', inset: 0,
        pointerEvents: 'none',
        zIndex: 0,
      }}
    />
  )
}

/* ─── Floating stat chips (decorative) ──────────────────────────────────── */
function FloatingChip({ style, label, value, positive }: {
  style: React.CSSProperties
  label: string; value: string; positive: boolean
}) {
  return (
    <div style={{
      position: 'absolute',
      padding: '7px 12px',
      borderRadius: 10,
      background: 'var(--glass-bg)',
      border: '1px solid var(--glass-border)',
      backdropFilter: 'blur(16px)',
      display: 'flex', flexDirection: 'column', gap: 1,
      userSelect: 'none', pointerEvents: 'none',
      animation: 'float 4s ease-in-out infinite',
      ...style,
    }}>
      <span style={{ fontSize: 9, color: 'var(--text-tertiary)', fontWeight: 600, letterSpacing: '.06em', textTransform: 'uppercase' }}>
        {label}
      </span>
      <span style={{
        fontSize: 13, fontWeight: 700, fontFamily: 'var(--font-mono)',
        color: positive ? 'var(--bull-strong)' : 'var(--bear-strong)',
        letterSpacing: '.02em',
      }}>
        {value}
      </span>
    </div>
  )
}

/* ─── Security badge ─────────────────────────────────────────────────────── */
function SecurityBadge({ icon, label }: { icon: React.ReactNode; label: string }) {
  return (
    <div style={{
      display: 'flex', alignItems: 'center', gap: 5,
      padding: '4px 10px',
      borderRadius: 99,
      background: 'var(--bg-hover)',
      border: '1px solid var(--border-subtle)',
      fontSize: 10.5, color: 'var(--text-tertiary)',
      fontWeight: 500,
    }}>
      <span style={{ color: 'var(--accent-400)', display: 'flex' }}>{icon}</span>
      {label}
    </div>
  )
}

/* ─── Eye icons ──────────────────────────────────────────────────────────── */
function EyeIcon({ open }: { open: boolean }) {
  return open ? (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/>
      <line x1="1" y1="1" x2="23" y2="23"/>
    </svg>
  ) : (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
      <circle cx="12" cy="12" r="3"/>
    </svg>
  )
}

/* ─── LOGIN PAGE ─────────────────────────────────────────────────────────── */
export function LoginPage() {
  const { login, isAuthenticated, isLoading, error, clearError } = useAuth()
  const { theme, accent, setTheme, setAccent } = useThemeStore()

  const [email,    setEmail]    = useState('')
  const [password, setPassword] = useState('')
  const [showPw,   setShowPw]   = useState(false)
  const [mounted,  setMounted]  = useState(false)
  const [focused,  setFocused]  = useState<'email'|'password'|null>(null)

  useEffect(() => { const t = setTimeout(() => setMounted(true), 60); return () => clearTimeout(t) }, [])

  if (isAuthenticated) return <Navigate to="/dashboard" replace />

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    await login({ email, password })
  }

  return (
    <div style={{
      minHeight: '100vh', height: '100vh',
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'var(--bg-base)',
      padding: 24,
      position: 'relative',
      overflow: 'hidden',
    }}>
      <AnimatedGrid />

      {/* Radial glows */}
      <div aria-hidden style={{
        position: 'fixed', inset: 0, pointerEvents: 'none', zIndex: 0,
        background: `
          radial-gradient(ellipse 60% 50% at 20% 30%, var(--accent-glow), transparent),
          radial-gradient(ellipse 40% 40% at 80% 70%, color-mix(in srgb, var(--accent-700) 12%, transparent), transparent)
        `,
      }} />

      {/* Decorative floating chips */}
      <FloatingChip style={{ top: '18%', left: '8%', animationDelay: '0s' }}   label="DSEX" value="+0.68%" positive={true} />
      <FloatingChip style={{ top: '30%', left: '4%', animationDelay: '1.2s' }} label="BATBC" value="৳716.00" positive={true} />
      <FloatingChip style={{ top: '60%', left: '6%', animationDelay: '2.1s' }} label="DS30" value="-0.81%" positive={false} />
      <FloatingChip style={{ top: '20%', right: '7%', animationDelay: '0.7s' }} label="Vol" value="82.4M" positive={true} />
      <FloatingChip style={{ top: '42%', right: '4%', animationDelay: '1.8s' }} label="RENATA" value="৳1,420" positive={false} />
      <FloatingChip style={{ top: '65%', right: '8%', animationDelay: '0.4s' }} label="Fill Rate" value="98.2%" positive={true} />

      {/* Theme strip — top center */}
      <div style={{
        position: 'fixed', top: 16, left: '50%', transform: 'translateX(-50%)',
        display: 'flex', alignItems: 'center', gap: 4,
        padding: '5px 10px',
        borderRadius: 99,
        background: 'var(--glass-bg)',
        border: '1px solid var(--glass-border)',
        backdropFilter: 'blur(20px)',
        zIndex: 10,
      }}>
        {/* Themes */}
        {THEMES.map(t => (
          <button key={t.id} onClick={() => setTheme(t.id as ThemeId)} title={t.label}
            style={{
              width: 22, height: 22, borderRadius: '50%',
              fontSize: 11,
              border: `1.5px solid ${theme === t.id ? 'var(--accent-400)' : 'transparent'}`,
              background: theme === t.id ? 'var(--accent-glow)' : 'transparent',
              cursor: 'pointer',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              transition: 'all 120ms',
            }}>
            {t.emoji}
          </button>
        ))}
        <div style={{ width: 1, height: 12, background: 'var(--border-default)', margin: '0 2px' }} />
        {/* Accents */}
        {ACCENTS.map(a => (
          <button key={a.id} onClick={() => setAccent(a.id as AccentId)} title={a.label}
            style={{
              width: 14, height: 14, borderRadius: '50%',
              background: a.color,
              border: `2px solid ${accent === a.id ? '#fff' : 'transparent'}`,
              outline: accent === a.id ? `2px solid ${a.color}` : 'none',
              outlineOffset: 1,
              cursor: 'pointer',
              transition: 'all 120ms',
              boxShadow: accent === a.id ? `0 0 8px ${a.color}90` : 'none',
            }}
          />
        ))}
      </div>

      {/* Card */}
      <div style={{
        position: 'relative', zIndex: 1,
        width: '100%', maxWidth: 420,
        opacity: mounted ? 1 : 0,
        transform: mounted ? 'translateY(0) scale(1)' : 'translateY(24px) scale(.97)',
        transition: 'opacity 600ms var(--ease-out-expo), transform 600ms var(--ease-out-expo)',
      }}>
        <div style={{
          background: 'var(--glass-bg)',
          border: '1px solid var(--glass-border)',
          backdropFilter: 'blur(32px) saturate(180%)',
          borderRadius: 'var(--r-2xl)',
          overflow: 'hidden',
          boxShadow: 'var(--shadow-xl)',
        }}>
          {/* Top accent line */}
          <div style={{
            height: 3,
            background: `linear-gradient(90deg, transparent, var(--accent-500), var(--accent-300), var(--accent-500), transparent)`,
          }}/>

          <div style={{ padding: '32px 36px 36px' }}>
            {/* Logo + brand */}
            <div style={{ display: 'flex', alignItems: 'center', gap: 14, marginBottom: 32 }}>
              <Logo size={44} animated />
              <div>
                <div style={{
                  fontFamily: 'var(--font-display)',
                  fontWeight: 800, fontSize: 20,
                  color: 'var(--text-primary)',
                  letterSpacing: '-0.04em',
                  lineHeight: 1,
                }}>
                  BD<span style={{ color: 'var(--accent-400)' }}>OMS</span>
                </div>
                <div style={{
                  fontSize: 10, color: 'var(--text-tertiary)',
                  letterSpacing: '.08em', textTransform: 'uppercase',
                  fontWeight: 500, marginTop: 3,
                }}>
                  Order Management System
                </div>
              </div>
            </div>

            <h1 style={{
              fontFamily: 'var(--font-display)',
              fontWeight: 700, fontSize: 24,
              color: 'var(--text-primary)',
              letterSpacing: '-0.03em',
              marginBottom: 6, lineHeight: 1,
            }}>
              Welcome back
            </h1>
            <p style={{ fontSize: 13, color: 'var(--text-secondary)', marginBottom: 28 }}>
              Sign in to your trading account
            </p>

            {/* Error */}
            {error && (
              <div className="animate-slide-down" style={{
                display: 'flex', alignItems: 'flex-start', gap: 10,
                padding: '10px 14px', borderRadius: 'var(--r-md)', marginBottom: 20,
                background: 'var(--bear-muted)',
                border: '1px solid var(--bear-border)',
              }}>
                <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="var(--bear-strong)" strokeWidth="2.2" strokeLinecap="round" strokeLinejoin="round" style={{ flexShrink: 0, marginTop: 1 }}>
                  <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
                </svg>
                <span style={{ flex: 1, fontSize: 12.5, color: 'var(--bear-strong)', lineHeight: 1.5 }}>{error}</span>
                <button onClick={clearError} style={{
                  background: 'none', border: 'none', cursor: 'pointer',
                  color: 'var(--bear-strong)', opacity: .7,
                  fontSize: 13, padding: 0, lineHeight: 1,
                }}>✕</button>
              </div>
            )}

            <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
              {/* Email */}
              <div>
                <label style={{
                  display: 'block', fontSize: 12, fontWeight: 500,
                  color: 'var(--text-secondary)', marginBottom: 7, letterSpacing: '.01em',
                }}>
                  Email address
                </label>
                <div className="input-group">
                  <span className="input-group-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                      <path d="M4 4h16c1.1 0 2 .9 2 2v12c0 1.1-.9 2-2 2H4c-1.1 0-2-.9-2-2V6c0-1.1.9-2 2-2z"/>
                      <polyline points="22,6 12,13 2,6"/>
                    </svg>
                  </span>
                  <input
                    type="email" autoComplete="email" required
                    placeholder="you@brokerage.com.bd"
                    value={email}
                    onChange={e => { setEmail(e.target.value); clearError() }}
                    onFocus={() => setFocused('email')}
                    onBlur={() => setFocused(null)}
                    className="input input-lg"
                    disabled={isLoading}
                    style={{
                      boxShadow: focused === 'email' ? `0 0 0 3px var(--accent-glow)` : undefined,
                    }}
                  />
                </div>
              </div>

              {/* Password */}
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 7 }}>
                  <label style={{ fontSize: 12, fontWeight: 500, color: 'var(--text-secondary)', letterSpacing: '.01em' }}>
                    Password
                  </label>
                  <button type="button" onClick={e => e.preventDefault()}
                    style={{ background: 'none', border: 'none', cursor: 'pointer', fontSize: 12, color: 'var(--accent-400)', padding: 0 }}>
                    Forgot password?
                  </button>
                </div>
                <div className="input-group" style={{ position: 'relative' }}>
                  <span className="input-group-icon">
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                      <rect x="3" y="11" width="18" height="11" rx="2" ry="2"/>
                      <path d="M7 11V7a5 5 0 0110 0v4"/>
                    </svg>
                  </span>
                  <input
                    type={showPw ? 'text' : 'password'}
                    autoComplete="current-password" required
                    placeholder="••••••••••••"
                    value={password}
                    onChange={e => { setPassword(e.target.value); clearError() }}
                    onFocus={() => setFocused('password')}
                    onBlur={() => setFocused(null)}
                    className="input input-lg"
                    disabled={isLoading}
                    style={{ paddingRight: 46 }}
                  />
                  <button type="button" onClick={() => setShowPw(s => !s)}
                    aria-label={showPw ? 'Hide password' : 'Show password'}
                    style={{
                      position: 'absolute', right: 12,
                      background: 'none', border: 'none', cursor: 'pointer',
                      color: 'var(--text-tertiary)', display: 'flex', alignItems: 'center',
                      padding: 4, borderRadius: 6,
                      transition: 'color 120ms',
                    }}
                    onMouseEnter={e => { (e.currentTarget as HTMLButtonElement).style.color = 'var(--text-primary)' }}
                    onMouseLeave={e => { (e.currentTarget as HTMLButtonElement).style.color = 'var(--text-tertiary)' }}
                  >
                    <EyeIcon open={!showPw} />
                  </button>
                </div>
              </div>

              {/* Submit */}
              <button
                type="submit"
                disabled={isLoading || !email || !password}
                className="btn btn-primary btn-xl"
                style={{ marginTop: 4, width: '100%', fontFamily: 'var(--font-display)', fontWeight: 700 }}
              >
                {isLoading ? (
                  <>
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round"
                      style={{ animation: 'spinSlow 0.8s linear infinite' }}>
                      <path d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" opacity=".2"/>
                      <path d="M21 12a9 9 0 00-9-9"/>
                    </svg>
                    Authenticating…
                  </>
                ) : (
                  <>
                    Sign in to OMS
                    <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round">
                      <line x1="5" y1="12" x2="19" y2="12"/><polyline points="12 5 19 12 12 19"/>
                    </svg>
                  </>
                )}
              </button>
            </form>

            {/* Security section */}
            <div style={{ margin: '24px 0 0' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: 10, marginBottom: 12 }}>
                <div style={{ flex: 1, height: 1, background: 'var(--border-subtle)' }} />
                <span style={{ fontSize: 10, color: 'var(--text-tertiary)', letterSpacing: '.08em', fontWeight: 600, textTransform: 'uppercase' }}>
                  Secured
                </span>
                <div style={{ flex: 1, height: 1, background: 'var(--border-subtle)' }} />
              </div>
              <div style={{ display: 'flex', gap: 6, flexWrap: 'wrap', justifyContent: 'center' }}>
                <SecurityBadge label="MFA Protected" icon={<svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round"><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z"/></svg>} />
                <SecurityBadge label="TLS 1.3" icon={<svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round"><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0110 0v4"/></svg>} />
                <SecurityBadge label="JWT + Refresh" icon={<svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round"><polyline points="23 4 23 10 17 10"/><polyline points="1 20 1 14 7 14"/><path d="M3.51 9a9 9 0 0114.85-3.36L23 10M1 14l4.64 4.36A9 9 0 0020.49 15"/></svg>} />
                <SecurityBadge label="Session Monitor" icon={<svg width="9" height="9" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round"><circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/></svg>} />
              </div>
            </div>
          </div>
        </div>

        {/* Footer credit */}
        <div style={{ textAlign: 'center', marginTop: 16, display: 'flex', flexDirection: 'column', gap: 4 }}>
          <p style={{ fontSize: 11, color: 'var(--text-tertiary)' }}>
            © {new Date().getFullYear()} BD Stock OMS · Bangladesh Securities & Exchange Commission
          </p>
          <p style={{ fontSize: 10.5, color: 'var(--text-tertiary)' }}>
            Design & Developed by{' '}
            <span style={{ color: 'var(--accent-400)', fontWeight: 600 }}>Eshan Barua</span>
          </p>
        </div>
      </div>
    </div>
  )
}
