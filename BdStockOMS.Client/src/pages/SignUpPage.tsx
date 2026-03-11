import { useState, FormEvent, useEffect } from 'react'
import { Navigate, Link } from 'react-router-dom'
import { useAuthStore } from '@/store/authStore'
import { useThemeStore, THEMES, ACCENTS, type ThemeId, type AccentId } from '@/store/themeStore'
import { Logo } from '@/components/ui/Logo'

type Step = 1 | 2 | 3

function StepIndicator({ current, total }: { current: Step; total: number }) {
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 28 }}>
      {Array.from({ length: total }, (_, i) => {
        const step = (i + 1) as Step
        const done = step < current
        const active = step === current
        return (
          <div key={i} style={{ display: 'flex', alignItems: 'center', gap: 6, flex: step < total ? 1 : undefined }}>
            <div style={{
              width: 26, height: 26, borderRadius: '50%',
              display: 'flex', alignItems: 'center', justifyContent: 'center',
              flexShrink: 0,
              fontSize: 11, fontWeight: 700,
              background: done ? 'var(--accent-600)' : active ? 'var(--accent-glow)' : 'var(--bg-hover)',
              border: `1.5px solid ${done || active ? 'var(--accent-500)' : 'var(--border-default)'}`,
              color: done ? '#fff' : active ? 'var(--accent-300)' : 'var(--text-tertiary)',
              transition: 'all 300ms var(--ease-out-expo)',
            }}>
              {done ? (
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                  <polyline points="20 6 9 17 4 12"/>
                </svg>
              ) : step}
            </div>
            {step < total && (
              <div style={{
                flex: 1, height: 1.5, borderRadius: 99,
                background: done ? 'var(--accent-600)' : 'var(--border-default)',
                transition: 'background 300ms',
              }} />
            )}
          </div>
        )
      })}
    </div>
  )
}

function FormField({
  label, type = 'text', placeholder, value, onChange,
  error, hint, required, autoComplete, children,
}: {
  label: string; type?: string; placeholder?: string
  value: string; onChange: (v: string) => void
  error?: string; hint?: string; required?: boolean; autoComplete?: string
  children?: React.ReactNode
}) {
  const [_focused, setFocused] = useState(false)
  return (
    <div>
      <label style={{ display: 'block', fontSize: 12, fontWeight: 500, color: 'var(--text-secondary)', marginBottom: 6 }}>
        {label}{required && <span style={{ color: 'var(--bear-base)', marginLeft: 3 }}>*</span>}
      </label>
      {children ?? (
        <input
          type={type}
          placeholder={placeholder}
          value={value}
          onChange={e => onChange(e.target.value)}
          onFocus={() => setFocused(true)}
          onBlur={() => setFocused(false)}
          autoComplete={autoComplete}
          className={`input input-lg ${error ? 'input-error' : ''}`}
        />
      )}
      {hint && !error && (
        <p style={{ fontSize: 11, color: 'var(--text-tertiary)', marginTop: 4 }}>{hint}</p>
      )}
      {error && (
        <p className="animate-fade-in" style={{ fontSize: 11, color: 'var(--bear-strong)', marginTop: 4 }}>⚠ {error}</p>
      )}
    </div>
  )
}

export function SignUpPage() {
  const isAuthenticated = useAuthStore(s => s.isAuthenticated)
  const { theme, accent, setTheme, setAccent } = useThemeStore()
  const [mounted, setMounted] = useState(false)
  const [step, setStep] = useState<Step>(1)

  // Step 1 fields
  const [firstName, setFirstName] = useState('')
  const [lastName, setLastName]   = useState('')
  const [email, setEmail]         = useState('')

  // Step 2 fields
  const [role, setRole]       = useState('')
  const [brokerId, setBroker] = useState('')
  const [phone, setPhone]     = useState('')

  // Step 3 fields
  const [password, setPassword]   = useState('')
  const [confirm, setConfirm]     = useState('')
  const [showPw, setShowPw]       = useState(false)
  const [agreed, setAgreed]       = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [errors, setErrors]       = useState<Record<string, string>>({})

  useEffect(() => { const t = setTimeout(() => setMounted(true), 60); return () => clearTimeout(t) }, [])
  if (isAuthenticated) return <Navigate to="/dashboard" replace />

  function validateStep(s: Step): boolean {
    const e: Record<string, string> = {}
    if (s === 1) {
      if (!firstName.trim()) e.firstName = 'First name is required'
      if (!lastName.trim())  e.lastName  = 'Last name is required'
      if (!email.includes('@')) e.email  = 'Enter a valid email address'
    } else if (s === 2) {
      if (!role)  e.role  = 'Select your role'
      if (!phone.match(/^[0-9+\s-]{10,}$/)) e.phone = 'Enter a valid phone number'
    } else if (s === 3) {
      if (password.length < 8) e.password = 'Minimum 8 characters'
      if (password !== confirm) e.confirm  = 'Passwords do not match'
      if (!agreed) e.agreed = 'You must accept the terms'
    }
    setErrors(e)
    return Object.keys(e).length === 0
  }

  function handleNext(e: FormEvent) {
    e.preventDefault()
    if (!validateStep(step)) return
    if (step < 3) setStep(s => (s + 1) as Step)
    else handleRegister()
  }

  async function handleRegister() {
    setIsLoading(true)
    // TODO: connect to /api/auth/register
    await new Promise(r => setTimeout(r, 1500))
    setIsLoading(false)
  }

  const stepTitles = ['Your Identity', 'Role & Access', 'Secure Password']
  const stepSubtitles = [
    'Tell us who you are',
    'Set up your access level',
    'Protect your account',
  ]

  const ROLES = [
    { id: 'Investor',       label: 'Investor',         desc: 'Buy and sell securities' },
    { id: 'Broker',         label: 'Broker',            desc: 'Execute client orders' },
    { id: 'BrokerageAdmin', label: 'Brokerage Admin',   desc: 'Manage brokerage operations' },
  ]

  return (
    <div style={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'var(--bg-base)', padding: '24px 24px 40px', position: 'relative', overflow: 'auto',
    }}>
      {/* Background */}
      <div aria-hidden style={{
        position: 'fixed', inset: 0, pointerEvents: 'none',
        background: `
          radial-gradient(ellipse 50% 50% at 80% 20%, var(--accent-glow), transparent),
          radial-gradient(ellipse 40% 40% at 10% 80%, color-mix(in srgb, var(--accent-700) 10%, transparent), transparent)
        `,
      }} />
      <div aria-hidden style={{
        position: 'fixed', inset: 0, pointerEvents: 'none', opacity: .025,
        backgroundImage: 'linear-gradient(var(--text-primary) 1px, transparent 1px), linear-gradient(90deg, var(--text-primary) 1px, transparent 1px)',
        backgroundSize: '56px 56px',
      }} />

      {/* Theme strip */}
      <div style={{
        position: 'fixed', top: 16, left: '50%', transform: 'translateX(-50%)',
        display: 'flex', alignItems: 'center', gap: 4,
        padding: '5px 10px', borderRadius: 99,
        background: 'var(--glass-bg)', border: '1px solid var(--glass-border)',
        backdropFilter: 'blur(20px)', zIndex: 10,
      }}>
        {THEMES.map(t => (
          <button key={t.id} onClick={() => setTheme(t.id as ThemeId)} title={t.label}
            style={{
              width: 22, height: 22, borderRadius: '50%', fontSize: 11,
              border: `1.5px solid ${theme === t.id ? 'var(--accent-400)' : 'transparent'}`,
              background: theme === t.id ? 'var(--accent-glow)' : 'transparent',
              cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center',
              transition: 'all 120ms',
            }}>
            {t.emoji}
          </button>
        ))}
        <div style={{ width: 1, height: 12, background: 'var(--border-default)', margin: '0 2px' }} />
        {ACCENTS.map(a => (
          <button key={a.id} onClick={() => setAccent(a.id as AccentId)} title={a.label}
            style={{
              width: 14, height: 14, borderRadius: '50%', background: a.color,
              border: `2px solid ${accent === a.id ? '#fff' : 'transparent'}`,
              outline: accent === a.id ? `2px solid ${a.color}` : 'none', outlineOffset: 1,
              cursor: 'pointer', transition: 'all 120ms',
              boxShadow: accent === a.id ? `0 0 8px ${a.color}90` : 'none',
            }}
          />
        ))}
      </div>

      {/* Main card */}
      <div style={{
        position: 'relative', zIndex: 1,
        width: '100%', maxWidth: 460,
        opacity: mounted ? 1 : 0,
        transform: mounted ? 'translateY(0)' : 'translateY(24px)',
        transition: 'opacity 600ms var(--ease-out-expo), transform 600ms var(--ease-out-expo)',
        marginTop: 56,
      }}>
        <div style={{
          background: 'var(--glass-bg)',
          border: '1px solid var(--glass-border)',
          backdropFilter: 'blur(32px) saturate(180%)',
          borderRadius: 'var(--r-2xl)',
          overflow: 'hidden',
          boxShadow: 'var(--shadow-xl)',
        }}>
          <div style={{ height: 3, background: `linear-gradient(90deg, transparent, var(--accent-500), var(--accent-300), var(--accent-500), transparent)` }} />

          <div style={{ padding: '30px 36px 36px' }}>
            {/* Logo */}
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, marginBottom: 28 }}>
              <Logo size={36} />
              <div>
                <div style={{ fontFamily: 'var(--font-display)', fontWeight: 800, fontSize: 16, color: 'var(--text-primary)', letterSpacing: '-0.03em' }}>
                  BD<span style={{ color: 'var(--accent-400)' }}>OMS</span>
                </div>
                <div style={{ fontSize: 9.5, color: 'var(--text-tertiary)', letterSpacing: '.06em', textTransform: 'uppercase', marginTop: 1 }}>
                  Create Account
                </div>
              </div>
            </div>

            <StepIndicator current={step} total={3} />

            <h2 style={{ fontFamily: 'var(--font-display)', fontWeight: 700, fontSize: 20, color: 'var(--text-primary)', letterSpacing: '-0.02em', marginBottom: 4, lineHeight: 1 }}>
              {stepTitles[step - 1]}
            </h2>
            <p style={{ fontSize: 13, color: 'var(--text-secondary)', marginBottom: 24 }}>
              {stepSubtitles[step - 1]}
            </p>

            <form onSubmit={handleNext} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>

              {/* STEP 1 */}
              {step === 1 && (
                <>
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 12 }}>
                    <FormField label="First Name" placeholder="Eshan" value={firstName} onChange={setFirstName} error={errors.firstName} required />
                    <FormField label="Last Name" placeholder="Barua" value={lastName} onChange={setLastName} error={errors.lastName} required />
                  </div>
                  <FormField label="Email Address" type="email" placeholder="you@brokerage.com.bd" value={email} onChange={setEmail} error={errors.email} required autoComplete="email" />
                </>
              )}

              {/* STEP 2 */}
              {step === 2 && (
                <>
                  <div>
                    <label style={{ display: 'block', fontSize: 12, fontWeight: 500, color: 'var(--text-secondary)', marginBottom: 8 }}>
                      Account Role <span style={{ color: 'var(--bear-base)' }}>*</span>
                    </label>
                    <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
                      {ROLES.map(r => (
                        <button key={r.id} type="button" onClick={() => setRole(r.id)}
                          style={{
                            display: 'flex', alignItems: 'center', gap: 12,
                            padding: '10px 14px', borderRadius: 'var(--r-md)', textAlign: 'left',
                            border: `1.5px solid ${role === r.id ? 'var(--accent-500)' : 'var(--border-default)'}`,
                            background: role === r.id ? 'var(--accent-glow)' : 'var(--bg-elevated)',
                            cursor: 'pointer', transition: 'all 150ms',
                          }}>
                          <div style={{
                            width: 10, height: 10, borderRadius: '50%', flexShrink: 0,
                            border: `2px solid ${role === r.id ? 'var(--accent-500)' : 'var(--border-strong)'}`,
                            background: role === r.id ? 'var(--accent-500)' : 'transparent',
                            transition: 'all 150ms',
                          }} />
                          <div>
                            <div style={{ fontSize: 13, fontWeight: 600, color: role === r.id ? 'var(--accent-300)' : 'var(--text-primary)' }}>{r.label}</div>
                            <div style={{ fontSize: 11, color: 'var(--text-tertiary)', marginTop: 1 }}>{r.desc}</div>
                          </div>
                        </button>
                      ))}
                    </div>
                    {errors.role && <p style={{ fontSize: 11, color: 'var(--bear-strong)', marginTop: 6 }}>⚠ {errors.role}</p>}
                  </div>
                  <FormField label="Brokerage ID (optional)" placeholder="e.g. DSE-2024-001" value={brokerId} onChange={setBroker} hint="Leave blank if you're an independent investor" />
                  <FormField label="Phone Number" type="tel" placeholder="+880 17XX-XXXXXX" value={phone} onChange={setPhone} error={errors.phone} required autoComplete="tel" />
                </>
              )}

              {/* STEP 3 */}
              {step === 3 && (
                <>
                  <div>
                    <label style={{ display: 'block', fontSize: 12, fontWeight: 500, color: 'var(--text-secondary)', marginBottom: 6 }}>
                      Password <span style={{ color: 'var(--bear-base)' }}>*</span>
                    </label>
                    <div style={{ position: 'relative' }}>
                      <input
                        type={showPw ? 'text' : 'password'}
                        placeholder="••••••••••••"
                        value={password}
                        onChange={e => setPassword(e.target.value)}
                        className={`input input-lg ${errors.password ? 'input-error' : ''}`}
                        style={{ paddingRight: 46 }}
                        autoComplete="new-password"
                      />
                      <button type="button" onClick={() => setShowPw(s => !s)}
                        style={{
                          position: 'absolute', right: 12, top: '50%', transform: 'translateY(-50%)',
                          background: 'none', border: 'none', cursor: 'pointer',
                          color: 'var(--text-tertiary)', display: 'flex', alignItems: 'center', padding: 4,
                        }}>
                        <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
                          {showPw
                            ? <><path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19m-6.72-1.07a3 3 0 11-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/></>
                            : <><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></>
                          }
                        </svg>
                      </button>
                    </div>
                    {/* Strength bar */}
                    {password && (
                      <div style={{ marginTop: 6 }}>
                        <div style={{ display: 'flex', gap: 3, marginBottom: 3 }}>
                          {[1,2,3,4].map(n => {
                            const strength = [password.length >= 8, /[A-Z]/.test(password), /[0-9]/.test(password), /[^a-zA-Z0-9]/.test(password)].filter(Boolean).length
                            return (
                              <div key={n} style={{
                                flex: 1, height: 3, borderRadius: 99,
                                background: n <= strength
                                  ? strength <= 1 ? 'var(--bear-base)' : strength <= 2 ? 'var(--warn-base)' : strength <= 3 ? 'var(--accent-500)' : 'var(--bull-base)'
                                  : 'var(--border-default)',
                                transition: 'background 200ms',
                              }} />
                            )
                          })}
                        </div>
                        {errors.password && <p style={{ fontSize: 11, color: 'var(--bear-strong)' }}>⚠ {errors.password}</p>}
                      </div>
                    )}
                  </div>
                  <FormField label="Confirm Password" type={showPw ? 'text' : 'password'} placeholder="••••••••••••" value={confirm} onChange={setConfirm} error={errors.confirm} required autoComplete="new-password" />

                  <label style={{ display: 'flex', alignItems: 'flex-start', gap: 10, cursor: 'pointer', marginTop: 4 }}>
                    <div
                      onClick={() => setAgreed(a => !a)}
                      style={{
                        width: 18, height: 18, borderRadius: 5, flexShrink: 0, marginTop: 1,
                        border: `2px solid ${agreed ? 'var(--accent-500)' : errors.agreed ? 'var(--bear-base)' : 'var(--border-strong)'}`,
                        background: agreed ? 'var(--accent-600)' : 'transparent',
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                        transition: 'all 150ms',
                        cursor: 'pointer',
                      }}>
                      {agreed && (
                        <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="3" strokeLinecap="round" strokeLinejoin="round">
                          <polyline points="20 6 9 17 4 12"/>
                        </svg>
                      )}
                    </div>
                    <span style={{ fontSize: 12, color: 'var(--text-secondary)', lineHeight: 1.5 }}>
                      I agree to the{' '}
                      <span style={{ color: 'var(--accent-400)', cursor: 'pointer' }}>Terms of Service</span>
                      {' '}and{' '}
                      <span style={{ color: 'var(--accent-400)', cursor: 'pointer' }}>Privacy Policy</span>
                      {' '}of BD Stock OMS
                    </span>
                  </label>
                  {errors.agreed && <p style={{ fontSize: 11, color: 'var(--bear-strong)', marginTop: -8 }}>⚠ {errors.agreed}</p>}
                </>
              )}

              {/* Nav buttons */}
              <div style={{ display: 'flex', gap: 10, marginTop: 8 }}>
                {step > 1 && (
                  <button type="button" onClick={() => setStep(s => (s - 1) as Step)}
                    className="btn btn-secondary btn-lg"
                    style={{ flex: 1 }}>
                    ← Back
                  </button>
                )}
                <button type="submit" className="btn btn-primary btn-lg"
                  disabled={isLoading}
                  style={{ flex: 2, fontFamily: 'var(--font-display)', fontWeight: 700 }}>
                  {isLoading ? (
                    <><svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" style={{ animation: 'spinSlow 0.8s linear infinite' }}><path d="M21 12a9 9 0 11-18 0 9 9 0 0118 0z" opacity=".2"/><path d="M21 12a9 9 0 00-9-9"/></svg> Creating…</>
                  ) : step < 3 ? 'Continue →' : 'Create Account'}
                </button>
              </div>
            </form>

            <p style={{ textAlign: 'center', fontSize: 12, color: 'var(--text-tertiary)', marginTop: 20 }}>
              Already have an account?{' '}
              <Link to="/login" style={{ color: 'var(--accent-400)', fontWeight: 500 }}>Sign in</Link>
            </p>
          </div>
        </div>

        {/* Footer */}
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
