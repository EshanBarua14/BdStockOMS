import { useState, useEffect } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { authApi } from '@/api/auth'
import { useAuthStore } from '@/store/authStore'
import { ThemeMenu } from '@/components/ui/ThemeMenu'
import type { AuthUser } from '@/types'

interface BrokerageItem { id: number; name: string; email: string; phone?: string }

// ── Registration hierarchy ────────────────────────────────────────────────
// SuperAdmin  → creates BrokerageHouse accounts (via /register-brokerage)
// BrokerageHouse/Admin → creates CCD, Admin users  (via admin panel — Day 50)
// CCD/Admin   → creates Traders                     (via admin panel — Day 50)
// Investor    → self-registers here                  (via /register-investor)
// ─────────────────────────────────────────────────────────────────────────

type Mode = 'select' | 'investor' | 'brokerage'

function Input({ label, type = 'text', value, onChange, placeholder, required = true }: {
  label: string; type?: string; value: string
  onChange: (v: string) => void; placeholder?: string; required?: boolean
}) {
  const [focused, setFocused] = useState(false)
  return (
    <div style={{ marginBottom: 14 }}>
      <label style={{ display: 'block', color: 'rgba(255,255,255,0.45)', fontSize: 11, marginBottom: 5, letterSpacing: '0.07em', textTransform: 'uppercase' }}>
        {label}{required && <span style={{ color: '#FF6B6B', marginLeft: 2 }}>*</span>}
      </label>
      <input
        type={type} value={value} required={required}
        onChange={e => onChange(e.target.value)}
        onFocus={() => setFocused(true)}
        onBlur={() => setFocused(false)}
        placeholder={placeholder}
        style={{
          width: '100%', boxSizing: 'border-box',
          background: 'rgba(255,255,255,0.04)',
          border: `1px solid ${focused ? 'rgba(0,212,170,0.5)' : 'rgba(255,255,255,0.08)'}`,
          borderRadius: 7, padding: '10px 12px',
          color: '#fff', fontSize: 13, outline: 'none',
          transition: 'border-color 0.2s',
          fontFamily: "'Outfit', sans-serif",
        }}
      />
    </div>
  )
}

function PasswordStrength({ password }: { password: string }) {
  const checks = [
    { label: '8+ chars',    ok: password.length >= 8 },
    { label: 'Uppercase',   ok: /[A-Z]/.test(password) },
    { label: 'Number',      ok: /[0-9]/.test(password) },
    { label: 'Special',     ok: /[^A-Za-z0-9]/.test(password) },
  ]
  const score = checks.filter(c => c.ok).length
  const colors = ['#FF6B6B', '#FF6B6B', '#F59E0B', '#F59E0B', '#00D4AA']
  return (
    <div style={{ marginBottom: 14 }}>
      <div style={{ display: 'flex', gap: 3, marginBottom: 5 }}>
        {[0,1,2,3].map(i => (
          <div key={i} style={{
            flex: 1, height: 3, borderRadius: 2,
            background: i < score ? colors[score] : 'rgba(255,255,255,0.08)',
            transition: 'background 0.2s',
          }} />
        ))}
      </div>
      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
        {checks.map(c => (
          <span key={c.label} style={{
            fontSize: 10, color: c.ok ? '#00D4AA' : 'rgba(255,255,255,0.25)',
            transition: 'color 0.2s',
          }}>
            {c.ok ? '✓' : '○'} {c.label}
          </span>
        ))}
      </div>
    </div>
  )
}

export function SignUpPage() {
  const navigate   = useNavigate()
  const setUser    = useAuthStore(s => s.setUser)
  const [mode, setMode]           = useState<Mode>('select')
  const [brokerages, setBrokerages] = useState<BrokerageItem[]>([])
  const [loading, setLoading]     = useState(false)
  const [error, setError]         = useState<string | null>(null)

  // Investor form
  const [invFull, setInvFull]     = useState('')
  const [invEmail, setInvEmail]   = useState('')
  const [invPhone, setInvPhone]   = useState('')
  const [invPass, setInvPass]     = useState('')
  const [invBH, setInvBH]         = useState<number>(0)
  const [invBO, setInvBO]         = useState('')

  // Brokerage form
  const [bhFirm, setBhFirm]       = useState('')
  const [bhLic, setBhLic]         = useState('')
  const [bhFirmEmail, setBhFirmEmail] = useState('')
  const [bhPhone, setBhPhone]     = useState('')
  const [bhAddr, setBhAddr]       = useState('')
  const [bhFull, setBhFull]       = useState('')
  const [bhEmail, setBhEmail]     = useState('')
  const [bhPass, setBhPass]       = useState('')

  useEffect(() => {
    authApi.getBrokerages().then(setBrokerages).catch(() => {})
  }, [])

  const card: React.CSSProperties = {
    background: 'rgba(13,19,32,0.88)',
    backdropFilter: 'blur(20px)',
    border: '1px solid rgba(0,212,170,0.15)',
    borderRadius: 14,
    padding: '32px 30px',
    boxShadow: '0 0 60px rgba(0,0,0,0.5)',
  }

  // ── Mode selector ───────────────────────────────────────────────────────
  if (mode === 'select') {
    return (
      <div style={{ minHeight: '100vh', background: '#080C14', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', fontFamily: "'Outfit', sans-serif", padding: '20px 16px' }}>
        <div style={{ position: 'absolute', top: 16, right: 20 }}><ThemeMenu /></div>
        <div style={{ width: '100%', maxWidth: 480 }}>
          <div style={{ textAlign: 'center', marginBottom: 32 }}>
            <svg width="40" height="40" viewBox="0 0 28 28" style={{ marginBottom: 12 }}>
              <polygon points="14,2 24,8 24,20 14,26 4,20 4,8" fill="none" stroke="#00D4AA" strokeWidth="1.5"/>
              <text x="14" y="17" textAnchor="middle" fill="#00D4AA" fontSize="6" fontFamily="Space Mono" fontWeight="bold">OMS</text>
            </svg>
            <h1 style={{ color: '#fff', fontSize: 22, fontWeight: 700, margin: '0 0 6px' }}>Create an account</h1>
            <p style={{ color: 'rgba(255,255,255,0.35)', fontSize: 13, margin: 0 }}>Who are you registering as?</p>
          </div>

          <div style={{ display: 'flex', flexDirection: 'column', gap: 10 }}>
            {/* Investor */}
            <button onClick={() => setMode('investor')} style={{
              display: 'flex', alignItems: 'center', gap: 16,
              background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(0,212,170,0.2)',
              borderRadius: 10, padding: '18px 20px', cursor: 'pointer', textAlign: 'left',
              transition: 'all 0.15s',
            }}>
              <div style={{ width: 42, height: 42, borderRadius: '50%', background: 'rgba(0,212,170,0.12)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M3 17l4-8 4 5 3-3 4 6" stroke="#00D4AA" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/></svg>
              </div>
              <div>
                <div style={{ color: '#fff', fontWeight: 600, fontSize: 14 }}>Investor</div>
                <div style={{ color: 'rgba(255,255,255,0.35)', fontSize: 12, marginTop: 2 }}>Self-register under a licensed brokerage house</div>
              </div>
              <svg style={{ marginLeft: 'auto', flexShrink: 0 }} width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M9 18l6-6-6-6" stroke="rgba(255,255,255,0.3)" strokeWidth="1.5" strokeLinecap="round"/></svg>
            </button>

            {/* Brokerage */}
            <button onClick={() => setMode('brokerage')} style={{
              display: 'flex', alignItems: 'center', gap: 16,
              background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(167,139,250,0.2)',
              borderRadius: 10, padding: '18px 20px', cursor: 'pointer', textAlign: 'left',
              transition: 'all 0.15s',
            }}>
              <div style={{ width: 42, height: 42, borderRadius: '50%', background: 'rgba(167,139,250,0.1)', display: 'flex', alignItems: 'center', justifyContent: 'center', flexShrink: 0 }}>
                <svg width="20" height="20" viewBox="0 0 24 24" fill="none"><path d="M3 9l9-7 9 7v11a2 2 0 01-2 2H5a2 2 0 01-2-2V9z" stroke="#A78BFA" strokeWidth="1.5"/><path d="M9 22V12h6v10" stroke="#A78BFA" strokeWidth="1.5"/></svg>
              </div>
              <div>
                <div style={{ color: '#fff', fontWeight: 600, fontSize: 14 }}>Brokerage House</div>
                <div style={{ color: 'rgba(255,255,255,0.35)', fontSize: 12, marginTop: 2 }}>Register your firm — BSEC licensed brokerages only</div>
              </div>
              <svg style={{ marginLeft: 'auto', flexShrink: 0 }} width="16" height="16" viewBox="0 0 24 24" fill="none"><path d="M9 18l6-6-6-6" stroke="rgba(255,255,255,0.3)" strokeWidth="1.5" strokeLinecap="round"/></svg>
            </button>

            {/* Trader/CCD/Admin info box */}
            <div style={{ background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.06)', borderRadius: 10, padding: '14px 16px' }}>
              <div style={{ color: 'rgba(255,255,255,0.4)', fontSize: 12, lineHeight: 1.6 }}>
                <strong style={{ color: 'rgba(255,255,255,0.6)' }}>Trader / CCD / Admin?</strong><br/>
                Accounts for Traders are created by your CCD or Admin.<br/>
                CCD and Admin accounts are created by the Brokerage House.
              </div>
            </div>
          </div>

          <p style={{ textAlign: 'center', color: 'rgba(255,255,255,0.3)', fontSize: 13, marginTop: 20 }}>
            Already have an account?{' '}
            <Link to="/login" style={{ color: '#00D4AA', textDecoration: 'none', fontWeight: 600 }}>Sign in</Link>
          </p>
        </div>
      </div>
    )
  }

  // ── Investor registration ───────────────────────────────────────────────
  if (mode === 'investor') {
    const handleInvestorSubmit = async (e: React.FormEvent) => {
      e.preventDefault()
      if (!invBH) { setError('Please select a brokerage house'); return }
      setLoading(true); setError(null)
      try {
        const data = await authApi.registerInvestor({
          fullName: invFull, email: invEmail, phone: invPhone,
          password: invPass, brokerageHouseId: invBH, boNumber: invBO || undefined,
        })
        const authUser: AuthUser = {
          userId: data.userId, fullName: data.fullName, email: data.email,
          role: data.role, brokerageHouseId: data.brokerageHouseId,
          brokerageHouseName: data.brokerageHouseName,
          token: data.token, expiresAt: new Date(data.expiresAt).getTime(),
        }
        setUser(authUser)
        navigate('/dashboard')
      } catch (err: unknown) {
        setError((err as any)?.response?.data?.message ?? 'Registration failed')
      } finally { setLoading(false) }
    }

    return (
      <div style={{ minHeight: '100vh', background: '#080C14', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', fontFamily: "'Outfit', sans-serif", padding: '20px 16px' }}>
        <div style={{ position: 'absolute', top: 16, right: 20 }}><ThemeMenu /></div>
        <div style={{ width: '100%', maxWidth: 460 }}>
          <button onClick={() => { setMode('select'); setError(null) }} style={{ background: 'none', border: 'none', color: 'rgba(255,255,255,0.4)', cursor: 'pointer', fontSize: 13, marginBottom: 16, padding: 0, display: 'flex', alignItems: 'center', gap: 6 }}>
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none"><path d="M19 12H5M12 19l-7-7 7-7" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>
            Back
          </button>
          <div style={card}>
            <div style={{ marginBottom: 24 }}>
              <h2 style={{ color: '#fff', fontSize: 18, fontWeight: 700, margin: '0 0 4px' }}>Investor Registration</h2>
              <p style={{ color: 'rgba(255,255,255,0.35)', fontSize: 12, margin: 0 }}>Self-register under a BSEC-licensed brokerage</p>
            </div>

            {error && (
              <div style={{ background: 'rgba(255,107,107,0.1)', border: '1px solid rgba(255,107,107,0.3)', borderRadius: 7, padding: '9px 12px', marginBottom: 16, color: '#FF6B6B', fontSize: 13 }}>{error}</div>
            )}

            <form onSubmit={handleInvestorSubmit}>
              <Input label="Full Name"    value={invFull}  onChange={setInvFull}  placeholder="e.g. Rahim Uddin" />
              <Input label="Email"        value={invEmail} onChange={setInvEmail} placeholder="you@example.com" type="email" />
              <Input label="Phone"        value={invPhone} onChange={setInvPhone} placeholder="01XXXXXXXXX" />
              <Input label="BO Number"    value={invBO}    onChange={setInvBO}    placeholder="Beneficiary Owner Number (optional)" required={false} />

              {/* Brokerage selector */}
              <div style={{ marginBottom: 14 }}>
                <label style={{ display: 'block', color: 'rgba(255,255,255,0.45)', fontSize: 11, marginBottom: 5, letterSpacing: '0.07em', textTransform: 'uppercase' }}>
                  Brokerage House <span style={{ color: '#FF6B6B' }}>*</span>
                </label>
                <select value={invBH} onChange={e => setInvBH(Number(e.target.value))} required style={{
                  width: '100%', boxSizing: 'border-box',
                  background: '#0D1320', border: '1px solid rgba(255,255,255,0.08)',
                  borderRadius: 7, padding: '10px 12px',
                  color: invBH ? '#fff' : 'rgba(255,255,255,0.3)',
                  fontSize: 13, outline: 'none', cursor: 'pointer',
                }}>
                  <option value={0} disabled>Select your brokerage house…</option>
                  {brokerages.map(b => (
                    <option key={b.id} value={b.id}>{b.name}</option>
                  ))}
                </select>
              </div>

              <div style={{ marginBottom: 14 }}>
                <Input label="Password" value={invPass} onChange={setInvPass} type="password" placeholder="Min 8 chars" />
                <PasswordStrength password={invPass} />
              </div>

              <button type="submit" disabled={loading} style={{
                width: '100%', padding: '11px', marginTop: 4,
                background: loading ? 'rgba(0,212,170,0.3)' : '#00D4AA',
                border: 'none', borderRadius: 7, color: '#0A1628',
                fontSize: 13, fontWeight: 700, cursor: loading ? 'not-allowed' : 'pointer',
              }}>
                {loading ? 'Creating account…' : 'Create Investor Account'}
              </button>
            </form>
          </div>
        </div>
      </div>
    )
  }

  // ── Brokerage registration ─────────────────────────────────────────────
  const handleBrokerageSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setLoading(true); setError(null)
    try {
      const data = await authApi.registerBrokerage({
        firmName: bhFirm, licenseNumber: bhLic, firmEmail: bhFirmEmail,
        firmPhone: bhPhone, firmAddress: bhAddr,
        fullName: bhFull, email: bhEmail, password: bhPass,
      })
      const authUser: AuthUser = {
        userId: data.userId, fullName: data.fullName, email: data.email,
        role: data.role, brokerageHouseId: data.brokerageHouseId,
        brokerageHouseName: data.brokerageHouseName,
        token: data.token, expiresAt: new Date(data.expiresAt).getTime(),
      }
      setUser(authUser)
      navigate('/dashboard')
    } catch (err: unknown) {
      setError((err as any)?.response?.data?.message ?? 'Registration failed')
    } finally { setLoading(false) }
  }

  return (
    <div style={{ minHeight: '100vh', background: '#080C14', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', fontFamily: "'Outfit', sans-serif", padding: '20px 16px' }}>
      <div style={{ position: 'absolute', top: 16, right: 20 }}><ThemeMenu /></div>
      <div style={{ width: '100%', maxWidth: 520 }}>
        <button onClick={() => { setMode('select'); setError(null) }} style={{ background: 'none', border: 'none', color: 'rgba(255,255,255,0.4)', cursor: 'pointer', fontSize: 13, marginBottom: 16, padding: 0, display: 'flex', alignItems: 'center', gap: 6 }}>
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none"><path d="M19 12H5M12 19l-7-7 7-7" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round"/></svg>
          Back
        </button>
        <div style={card}>
          <div style={{ marginBottom: 24 }}>
            <div style={{ display: 'inline-flex', alignItems: 'center', gap: 6, background: 'rgba(167,139,250,0.1)', border: '1px solid rgba(167,139,250,0.2)', borderRadius: 20, padding: '3px 10px', marginBottom: 10 }}>
              <span style={{ color: '#A78BFA', fontSize: 11, fontWeight: 600, letterSpacing: '0.06em' }}>BSEC LICENSED</span>
            </div>
            <h2 style={{ color: '#fff', fontSize: 18, fontWeight: 700, margin: '0 0 4px' }}>Register Brokerage House</h2>
            <p style={{ color: 'rgba(255,255,255,0.35)', fontSize: 12, margin: 0 }}>Creates a new brokerage account + admin user. Tenant DB provisioned by SuperAdmin.</p>
          </div>

          {error && (
            <div style={{ background: 'rgba(255,107,107,0.1)', border: '1px solid rgba(255,107,107,0.3)', borderRadius: 7, padding: '9px 12px', marginBottom: 16, color: '#FF6B6B', fontSize: 13 }}>{error}</div>
          )}

          <form onSubmit={handleBrokerageSubmit}>
            <div style={{ color: 'rgba(255,255,255,0.25)', fontSize: 10, fontFamily: "'Space Mono', monospace", letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: 10 }}>Firm Details</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 12px' }}>
              <Input label="Firm Name"        value={bhFirm}      onChange={setBhFirm}      placeholder="Pioneer Securities Ltd" />
              <Input label="License Number"   value={bhLic}       onChange={setBhLic}       placeholder="BSEC/MB/2024/XXX" />
              <Input label="Firm Email"       value={bhFirmEmail} onChange={setBhFirmEmail} placeholder="info@firm.com.bd" type="email" />
              <Input label="Firm Phone"       value={bhPhone}     onChange={setBhPhone}     placeholder="01XXXXXXXXX" required={false} />
            </div>
            <Input label="Firm Address"     value={bhAddr}  onChange={setBhAddr}  placeholder="Motijheel, Dhaka" />

            <div style={{ color: 'rgba(255,255,255,0.25)', fontSize: 10, fontFamily: "'Space Mono', monospace", letterSpacing: '0.1em', textTransform: 'uppercase', margin: '14px 0 10px' }}>Admin Account</div>
            <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '0 12px' }}>
              <Input label="Admin Full Name"  value={bhFull}  onChange={setBhFull}  placeholder="Full Name" />
              <Input label="Admin Email"      value={bhEmail} onChange={setBhEmail} placeholder="admin@firm.com" type="email" />
            </div>
            <Input label="Password" value={bhPass} onChange={setBhPass} type="password" placeholder="Min 8 chars" />
            <PasswordStrength password={bhPass} />

            <button type="submit" disabled={loading} style={{
              width: '100%', padding: '11px', marginTop: 8,
              background: loading ? 'rgba(167,139,250,0.3)' : '#A78BFA',
              border: 'none', borderRadius: 7, color: '#fff',
              fontSize: 13, fontWeight: 700, cursor: loading ? 'not-allowed' : 'pointer',
            }}>
              {loading ? 'Registering…' : 'Register Brokerage House'}
            </button>
          </form>
        </div>
      </div>
    </div>
  )
}

export default SignUpPage
