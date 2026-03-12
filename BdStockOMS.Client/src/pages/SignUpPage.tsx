// @ts-nocheck
// src/pages/SignUpPage.tsx
// Compact Investor Registration — 2-step, field validation, fits 100% zoom

import { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import { ThemeMenu } from '@/components/ui/ThemeMenu'
import logoImg from '@/assets/images/logo.png'
import type { AuthUser } from '@/types'

interface BrokerageItem { id: number; name: string; email: string; phone?: string }

// ── Compact Input ────────────────────────────────────────────
function Input({ label, type = 'text', value, onChange, placeholder, required = true, error: fieldError, icon }: {
  label: string; type?: string; value: string; error?: string
  onChange: (v: string) => void; placeholder?: string; required?: boolean; icon?: React.ReactNode
}) {
  const [focused, setFocused] = useState(false)
  const hasError = !!fieldError
  const borderColor = hasError ? 'rgba(255,107,107,0.5)' : focused ? 'rgba(0,212,170,0.5)' : 'rgba(255,255,255,0.07)'

  return (
    <div style={{ marginBottom: 12 }}>
      <label style={{
        display: 'flex', alignItems: 'center', gap: 4,
        color: hasError ? '#FF6B6B' : 'rgba(255,255,255,0.4)', fontSize: 10,
        marginBottom: 4, letterSpacing: '0.06em', textTransform: 'uppercase',
        fontFamily: "'JetBrains Mono', monospace", fontWeight: 600,
      }}>
        {label}{required && <span style={{ color: '#FF6B6B' }}>*</span>}
      </label>
      <div style={{ position: 'relative' }}>
        {icon && (
          <div style={{
            position: 'absolute', left: 10, top: '50%', transform: 'translateY(-50%)',
            color: focused ? 'rgba(0,212,170,0.5)' : 'rgba(255,255,255,0.12)',
            transition: 'color 0.2s', display: 'flex',
          }}>{icon}</div>
        )}
        <input type={type} value={value} required={required}
          onChange={e => onChange(e.target.value)}
          onFocus={() => setFocused(true)} onBlur={() => setFocused(false)}
          placeholder={placeholder}
          style={{
            width: '100%', boxSizing: 'border-box',
            background: 'var(--t-hover)',
            border: `1px solid ${borderColor}`,
            borderRadius: 7, padding: icon ? '8px 12px 8px 34px' : '8px 12px',
            color: 'var(--t-text1)', fontSize: 13, outline: 'none',
            fontFamily: "'Outfit', sans-serif",
            transition: 'all 0.2s',
            boxShadow: focused ? '0 0 12px rgba(0,212,170,0.04)' : 'none',
          }}
        />
      </div>
      {fieldError && (
        <div style={{ color: '#FF6B6B', fontSize: 10, marginTop: 3, fontFamily: "'JetBrains Mono', monospace" }}>
          {fieldError}
        </div>
      )}
    </div>
  )
}

// ── Password Strength (compact) ──────────────────────────────
function PasswordStrength({ password }: { password: string }) {
  const checks = [
    { label: '8+', ok: password.length >= 8 },
    { label: 'A-Z', ok: /[A-Z]/.test(password) },
    { label: '0-9', ok: /[0-9]/.test(password) },
    { label: '!@#', ok: /[^A-Za-z0-9]/.test(password) },
  ]
  const score = checks.filter(c => c.ok).length
  const colors = ['#FF6B6B', '#FF6B6B', '#F59E0B', '#F59E0B', '#00D4AA']
  return (
    <div style={{ marginBottom: 10, marginTop: -4 }}>
      <div style={{ display: 'flex', gap: 2, marginBottom: 4 }}>
        {[0, 1, 2, 3].map(i => (
          <div key={i} style={{
            flex: 1, height: 2, borderRadius: 2,
            background: i < score ? colors[score] : 'rgba(255,255,255,0.06)',
            transition: 'background 0.3s',
          }} />
        ))}
      </div>
      <div style={{ display: 'flex', gap: 8 }}>
        {checks.map(c => (
          <span key={c.label} style={{
            fontSize: 9, color: c.ok ? '#00D4AA' : 'rgba(255,255,255,0.2)',
            fontFamily: "'JetBrains Mono', monospace", fontWeight: 600,
          }}>{c.ok ? '✓' : '○'} {c.label}</span>
        ))}
      </div>
    </div>
  )
}

// ── Step dots ────────────────────────────────────────────────
function Steps({ current }: { current: number }) {
  return (
    <div style={{ display: 'flex', gap: 6, justifyContent: 'center', marginBottom: 16 }}>
      {[0, 1].map(i => (
        <div key={i} style={{
          width: i === current ? 20 : 8, height: 3, borderRadius: 3,
          background: i <= current ? '#00D4AA' : 'rgba(255,255,255,0.08)',
          transition: 'all 0.3s cubic-bezier(0.16,1,0.3,1)',
        }} />
      ))}
    </div>
  )
}

// ── Validation helpers ───────────────────────────────────────
const isValidEmail = (e: string) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(e)
const isValidPhone = (p: string) => /^01[3-9]\d{8}$/.test(p.replace(/[\s-]/g, ''))

// ═══════════════════════════════════════════════════════════
export function SignUpPage() {
  const navigate = useNavigate()
  const setUser = useAuthStore(s => s.setUser)
  const [brokerages, setBrokerages] = useState<BrokerageItem[]>([])
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [step, setStep] = useState(0)
  const [success, setSuccess] = useState(false)
  const [touched, setTouched] = useState<Record<string, boolean>>({})

  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phone, setPhone] = useState('')
  const [boNumber, setBoNumber] = useState('')
  const [brokerage, setBrokerage] = useState<number>(0)
  const [password, setPassword] = useState('')
  const [confirmPass, setConfirmPass] = useState('')
  const [agreed, setAgreed] = useState(false)

  useEffect(() => { authApi.getBrokerages().then(setBrokerages).catch(() => {}) }, [])

  const touch = (f: string) => setTouched(p => ({ ...p, [f]: true }))

  // Field errors
  const errors = {
    fullName: touched.fullName && !fullName ? 'Required' : '',
    email: touched.email && !email ? 'Required' : touched.email && !isValidEmail(email) ? 'Invalid email format' : '',
    phone: touched.phone && !phone ? 'Required' : touched.phone && !isValidPhone(phone) ? 'Invalid BD phone (01XXXXXXXXX)' : '',
    brokerage: touched.brokerage && !brokerage ? 'Required' : '',
    password: touched.password && password.length < 8 ? 'Min 8 characters' : '',
    confirmPass: touched.confirmPass && password !== confirmPass ? 'Passwords don\'t match' : '',
  }

  const canStep0 = fullName && isValidEmail(email) && isValidPhone(phone)
  const canSubmit = brokerage && password.length >= 8 && password === confirmPass && agreed

  const goStep1 = () => {
    setTouched({ fullName: true, email: true, phone: true })
    if (canStep0) { setError(null); setStep(1) }
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setTouched({ ...touched, brokerage: true, password: true, confirmPass: true })
    if (!canSubmit) return
    setLoading(true); setError(null)
    try {
      const data = await authApi.registerInvestor({
        fullName, email, phone, password,
        brokerageHouseId: brokerage,
        boNumber: boNumber || undefined,
      })
      setSuccess(true); setStep(2)
    } catch (err: unknown) {
      setError((err as any)?.response?.data?.message ?? 'Registration failed')
    } finally { setLoading(false) }
  }

  // ── Success ────────────────────────────────────────────
  if (success) {
    return (
      <div style={{
        minHeight: '100vh', background: 'var(--t-bg)',
        display: 'flex', alignItems: 'center', justifyContent: 'center',
        fontFamily: "'Outfit', sans-serif",
      }}>
        <div style={{
          width: 400, textAlign: 'center',
          background: 'var(--t-surface)', backdropFilter: 'blur(24px)',
          border: '1px solid rgba(0,212,170,0.15)', borderRadius: 14,
          padding: '36px 28px', position: 'relative', overflow: 'hidden',
          boxShadow: '0 0 60px rgba(0,212,170,0.04), 0 24px 48px rgba(0,0,0,0.5)',
        }}>
          <div style={{ position: 'absolute', top: 0, left: '15%', right: '15%', height: 1, background: 'linear-gradient(90deg, transparent, rgba(0,212,170,0.4), transparent)' }} />
          <div style={{
            width: 52, height: 52, borderRadius: '50%', margin: '0 auto 16px',
            background: 'rgba(0,212,170,0.08)', border: '2px solid rgba(0,212,170,0.2)',
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none">
              <path d="M20 6L9 17l-5-5" stroke="#00D4AA" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </div>
          <h2 style={{ color: 'var(--t-text1)', fontSize: 18, fontWeight: 700, margin: '0 0 6px' }}>Registration Submitted!</h2>
          <p style={{ color: 'var(--t-text3)', fontSize: 12, margin: '0 0 16px', lineHeight: 1.5 }}>
            Your brokerage house admin will review and approve your registration.
          </p>
          <div style={{
            background: 'rgba(255,215,64,0.06)', border: '1px solid rgba(255,215,64,0.12)',
            borderRadius: 8, padding: '10px 12px', marginBottom: 20, textAlign: 'left',
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 6 }}>
              <span style={{ width: 5, height: 5, borderRadius: '50%', background: '#ffd740', animation: 'oms-pulse 2s ease-in-out infinite' }} />
              <span style={{ color: '#ffd740', fontSize: 10, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.06em' }}>PENDING APPROVAL</span>
            </div>
            <div style={{ color: 'var(--t-text3)', fontSize: 10, lineHeight: 1.5 }}>
              1. Brokerage admin reviews your application<br/>
              2. You'll receive email confirmation<br/>
              3. Then sign in and start trading
            </div>
          </div>
          <Link to="/login" style={{
            display: 'inline-flex', padding: '9px 24px', borderRadius: 7,
            background: 'linear-gradient(135deg, #00D4AA, #00B894)',
            color: '#0A1628', fontSize: 13, fontWeight: 700, textDecoration: 'none',
            boxShadow: '0 4px 16px rgba(0,212,170,0.25)',
          }}>Go to Sign In</Link>
        </div>
      </div>
    )
  }

  // ── Form ───────────────────────────────────────────────
  return (
    <div style={{
      minHeight: '100vh', background: 'var(--t-bg)',
      display: 'flex', flexDirection: 'column',
      fontFamily: "'Outfit', sans-serif",
    }}>
      {/* Ambient */}
      <div style={{ position: 'fixed', top: -200, right: -200, width: 500, height: 500, borderRadius: '50%', background: 'radial-gradient(circle, rgba(0,212,170,0.03) 0%, transparent 70%)', pointerEvents: 'none' }} />

      {/* Top bar */}
      <div style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '10px 24px', flexShrink: 0,
        borderBottom: '1px solid var(--t-border)',
      }}>
        <Link to="/login" style={{ display: 'flex', alignItems: 'center', gap: 8, textDecoration: 'none' }}>
          <img src={logoImg} alt="OMS" style={{ height: 28, filter: 'drop-shadow(0 0 6px rgba(0,212,170,0.12))' }} />
          <span style={{ color: 'var(--t-text1)', fontWeight: 700, fontSize: 13 }}>BD Stock <span style={{ color: '#00D4AA' }}>OMS</span></span>
        </Link>
        <ThemeMenu variant="compact" />
      </div>

      {/* Content */}
      <div style={{
        flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center',
        padding: '16px',
      }}>
        <div style={{ width: '100%', maxWidth: 420 }}>

          {step > 0 && (
            <button onClick={() => { setStep(0); setError(null) }} style={{
              background: 'none', border: 'none', color: 'var(--t-text3)',
              cursor: 'pointer', fontSize: 12, marginBottom: 8, padding: 0,
              display: 'flex', alignItems: 'center', gap: 5, fontFamily: "'Outfit', sans-serif",
            }}>
              <svg width="12" height="12" viewBox="0 0 24 24" fill="none"><path d="M19 12H5M12 19l-7-7 7-7" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>
              Back
            </button>
          )}

          <div style={{
            background: 'var(--t-surface)', backdropFilter: 'blur(24px)',
            border: '1px solid var(--t-border)', borderRadius: 14,
            padding: '24px 24px 20px', position: 'relative', overflow: 'hidden',
            boxShadow: '0 0 60px rgba(0,212,170,0.03), 0 24px 48px rgba(0,0,0,0.4)',
          }}>
            <div style={{ position: 'absolute', top: 0, left: '15%', right: '15%', height: 1, background: 'linear-gradient(90deg, transparent, rgba(0,212,170,0.3), transparent)' }} />

            {/* Header */}
            <div style={{ textAlign: 'center', marginBottom: 4 }}>
              <div style={{
                display: 'inline-flex', alignItems: 'center', gap: 5, marginBottom: 8,
                padding: '3px 10px', borderRadius: 16,
                background: 'rgba(0,212,170,0.06)', border: '1px solid var(--t-border)',
              }}>
                <svg width="10" height="10" viewBox="0 0 24 24" fill="none"><path d="M3 17l4-8 4 5 3-3 4 6" stroke="#00D4AA" strokeWidth="1.5" strokeLinecap="round"/></svg>
                <span style={{ color: '#00D4AA', fontSize: 9, fontWeight: 700, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.06em' }}>INVESTOR REGISTRATION</span>
              </div>
              <h2 style={{ color: 'var(--t-text1)', fontSize: 18, fontWeight: 700, margin: '0 0 3px' }}>Create your account</h2>
              <p style={{ color: 'var(--t-text3)', fontSize: 11, margin: 0 }}>Register under a BSEC-licensed brokerage</p>
            </div>

            <Steps current={step} />

            {error && (
              <div style={{
                background: 'rgba(255,107,107,0.08)', border: '1px solid rgba(255,107,107,0.15)',
                borderRadius: 7, padding: '8px 12px', marginBottom: 12,
                display: 'flex', alignItems: 'center', gap: 6, fontSize: 11, color: '#FF6B6B',
              }}>
                <svg width="12" height="12" viewBox="0 0 14 14" fill="none"><circle cx="7" cy="7" r="6" stroke="#FF6B6B" strokeWidth="1.2"/><path d="M7 4v3M7 9.5v.5" stroke="#FF6B6B" strokeWidth="1.2" strokeLinecap="round"/></svg>
                {error}
              </div>
            )}

            <form onSubmit={handleSubmit}>
              {/* ── Step 0 ── */}
              {step === 0 && (
                <div>
                  <div style={{ color: 'var(--t-text3)', fontSize: 9, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.08em', marginBottom: 10, fontWeight: 700 }}>PERSONAL DETAILS</div>

                  <Input label="Full Name" value={fullName} error={errors.fullName}
                    onChange={v => { setFullName(v); touch('fullName') }} placeholder="e.g. Rahim Uddin"
                    icon={<svg width="13" height="13" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="8" r="4" stroke="currentColor" strokeWidth="1.5"/><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7" stroke="currentColor" strokeWidth="1.5"/></svg>}
                  />
                  <Input label="Email" value={email} type="email" error={errors.email}
                    onChange={v => { setEmail(v); touch('email') }} placeholder="you@example.com"
                    icon={<svg width="13" height="13" viewBox="0 0 24 24" fill="none"><path d="M4 4h16c1.1 0 2 .9 2 2v12a2 2 0 01-2 2H4a2 2 0 01-2-2V6c0-1.1.9-2 2-2z" stroke="currentColor" strokeWidth="1.5"/><path d="M22 6l-10 7L2 6" stroke="currentColor" strokeWidth="1.5"/></svg>}
                  />
                  <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 10px' }}>
                    <Input label="Phone" value={phone} error={errors.phone}
                      onChange={v => { setPhone(v); touch('phone') }} placeholder="01XXXXXXXXX"
                      icon={<svg width="13" height="13" viewBox="0 0 24 24" fill="none"><path d="M22 16.92v3a2 2 0 01-2.18 2 19.79 19.79 0 01-8.63-3.07 19.5 19.5 0 01-6-6 19.79 19.79 0 01-3.07-8.67A2 2 0 014.11 2h3a2 2 0 012 1.72c.12.96.36 1.9.7 2.81a2 2 0 01-.45 2.11L8.09 9.91a16 16 0 006 6l1.27-1.27a2 2 0 012.11-.45c.91.34 1.85.58 2.81.7A2 2 0 0122 16.92z" stroke="currentColor" strokeWidth="1.5"/></svg>}
                    />
                    <Input label="BO Number" value={boNumber}
                      onChange={setBoNumber} placeholder="Optional" required={false}
                    />
                  </div>

                  <button type="button" onClick={goStep1}
                    style={{
                      width: '100%', padding: '10px', marginTop: 4,
                      background: canStep0 ? 'linear-gradient(135deg, #00D4AA, #00B894)' : 'rgba(255,255,255,0.04)',
                      border: 'none', borderRadius: 7,
                      color: canStep0 ? '#0A1628' : 'rgba(255,255,255,0.2)',
                      fontSize: 13, fontWeight: 700, cursor: canStep0 ? 'pointer' : 'not-allowed',
                      fontFamily: "'Outfit', sans-serif",
                      boxShadow: canStep0 ? '0 4px 16px rgba(0,212,170,0.2)' : 'none',
                      transition: 'all 0.2s',
                    }}>Continue →</button>
                </div>
              )}

              {/* ── Step 1 ── */}
              {step === 1 && (
                <div>
                  <div style={{ color: 'var(--t-text3)', fontSize: 9, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.08em', marginBottom: 10, fontWeight: 700 }}>BROKERAGE & SECURITY</div>

                  <div style={{ marginBottom: 12 }}>
                    <label style={{ display: 'block', color: 'var(--t-text3)', fontSize: 10, marginBottom: 4, letterSpacing: '0.06em', textTransform: 'uppercase', fontFamily: "'JetBrains Mono', monospace", fontWeight: 600 }}>
                      Brokerage House <span style={{ color: '#FF6B6B' }}>*</span>
                    </label>
                    <select value={brokerage} onChange={e => { setBrokerage(Number(e.target.value)); touch('brokerage') }}
                      required style={{
                        width: '100%', boxSizing: 'border-box',
                        background: 'var(--t-hover)',
                        border: `1px solid ${errors.brokerage ? 'rgba(255,107,107,0.5)' : 'rgba(255,255,255,0.07)'}`,
                        borderRadius: 7, padding: '8px 12px',
                        color: brokerage ? 'var(--t-text1)' : 'var(--t-text3)',
                        fontSize: 13, outline: 'none', cursor: 'pointer',
                        fontFamily: "'Outfit', sans-serif",
                      }}>
                      <option value={0} disabled>Select brokerage house…</option>
                      {brokerages.map(b => (
                        <option key={b.id} value={b.id} style={{ background: 'var(--t-panel)', color: 'var(--t-text1)' }}>{b.name}</option>
                      ))}
                    </select>
                    {errors.brokerage && <div style={{ color: '#FF6B6B', fontSize: 10, marginTop: 3, fontFamily: "'JetBrains Mono', monospace" }}>{errors.brokerage}</div>}
                  </div>

                  <Input label="Password" value={password} type="password" error={errors.password}
                    onChange={v => { setPassword(v); touch('password') }} placeholder="Min 8 characters" />
                  <PasswordStrength password={password} />

                  <Input label="Confirm Password" value={confirmPass} type="password" error={errors.confirmPass}
                    onChange={v => { setConfirmPass(v); touch('confirmPass') }} placeholder="Re-enter password" />

                  <label style={{
                    display: 'flex', alignItems: 'flex-start', gap: 8,
                    marginBottom: 12, padding: '10px 12px', borderRadius: 7,
                    background: 'var(--t-hover)', border: '1px solid var(--t-border)',
                    cursor: 'pointer',
                  }}>
                    <input type="checkbox" checked={agreed} onChange={e => setAgreed(e.target.checked)}
                      style={{ marginTop: 1, accentColor: '#00D4AA', cursor: 'pointer' }} />
                    <span style={{ color: 'var(--t-text3)', fontSize: 10, lineHeight: 1.5 }}>
                      I agree to the <span style={{ color: '#00D4AA' }}>Terms</span> &{' '}
                      <span style={{ color: '#00D4AA' }}>Privacy Policy</span>.
                      Account requires brokerage approval.
                    </span>
                  </label>

                  <div style={{
                    background: 'rgba(68,138,255,0.04)', border: '1px solid rgba(68,138,255,0.10)',
                    borderRadius: 7, padding: '8px 10px', marginBottom: 12,
                    display: 'flex', alignItems: 'center', gap: 6,
                  }}>
                    <svg width="12" height="12" viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0 }}>
                      <circle cx="12" cy="12" r="10" stroke="#448aff" strokeWidth="1.5"/>
                      <path d="M12 8v4M12 16h.01" stroke="#448aff" strokeWidth="1.5" strokeLinecap="round"/>
                    </svg>
                    <span style={{ color: 'var(--t-text3)', fontSize: 10, lineHeight: 1.4 }}>
                      Registration reviewed by brokerage admin per RBAC approval.
                    </span>
                  </div>

                  <button type="submit" disabled={loading || !canSubmit} style={{
                    width: '100%', padding: '10px',
                    background: (!loading && canSubmit) ? 'linear-gradient(135deg, #00D4AA, #00B894)' : 'rgba(255,255,255,0.04)',
                    border: 'none', borderRadius: 7,
                    color: (!loading && canSubmit) ? '#0A1628' : 'rgba(255,255,255,0.2)',
                    fontSize: 13, fontWeight: 700,
                    cursor: (!loading && canSubmit) ? 'pointer' : 'not-allowed',
                    fontFamily: "'Outfit', sans-serif",
                    boxShadow: (!loading && canSubmit) ? '0 4px 16px rgba(0,212,170,0.2)' : 'none',
                    transition: 'all 0.2s',
                  }}>{loading ? 'Submitting…' : 'Submit Registration'}</button>
                </div>
              )}
            </form>

            <div style={{ textAlign: 'center', marginTop: 14 }}>
              <span style={{ color: 'var(--t-text3)', fontSize: 11 }}>
                Already registered?{' '}
                <Link to="/login" style={{ color: '#00D4AA', textDecoration: 'none', fontWeight: 600 }}>Sign in</Link>
              </span>
            </div>
          </div>

          {/* Info + credit */}
          <div style={{
            marginTop: 10, padding: '8px 14px', borderRadius: 8,
            background: 'var(--t-hover)', border: '1px solid var(--t-border)',
            textAlign: 'center', color: 'var(--t-text3)', fontSize: 10,
          }}>
            <strong style={{ color: 'var(--t-text3)' }}>Trader / CCD / Admin?</strong> — Created by brokerage admin via Admin Panel.
          </div>
          <div style={{ textAlign: 'center', marginTop: 10 }}>
            <span style={{ color: 'var(--t-text3)', fontSize: 9 }}>
              Developed by{' '}
              <a href="https://www.linkedin.com/in/eshan01/" target="_blank" rel="noopener noreferrer"
                style={{ color: 'rgba(0,212,170,0.4)', textDecoration: 'none', fontWeight: 600 }}>Eshan Barua</a>
              {' · '}
              <span style={{ fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.04em' }}>BSEC · DSE · CSE · CDBL</span>
            </span>
          </div>
        </div>
      </div>
    </div>
  )
}

export default SignUpPage
