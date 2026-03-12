// @ts-nocheck
import { useState, useRef, useEffect } from 'react'
import {
  useThemeStore, THEMES, ACCENTS, DENSITIES,
  type ThemeId, type AccentId, type DensityOption,
} from '@/store/themeStore'

interface ThemeMenuProps {
  variant?: 'compact' | 'full'
}

const BUY_COLORS = ['#00e676', '#00D4AA', '#10B981', '#22C55E', '#4ADE80']
const SELL_COLORS = ['#ff1744', '#EF4444', '#F43F5E', '#E11D48', '#FF6B6B']

export function ThemeMenu({ variant = 'compact' }: ThemeMenuProps) {
  const [open, setOpen]       = useState(false)
  const [tab, setTab]         = useState<'theme' | 'accent' | 'density' | 'colors'>('theme')
  const ref                   = useRef<HTMLDivElement>(null)

  const {
    theme, accent, density, buyColor, sellColor,
    pendingTheme, pendingAccent,
    previewTheme, previewAccent,
    confirmTheme, cancelPreview,
    setDensity, setBuyColor, setSellColor,
  } = useThemeStore()

  const hasPending = pendingTheme !== null || pendingAccent !== null
  const activeTheme  = pendingTheme  ?? theme
  const activeAccent = pendingAccent ?? accent

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        if (hasPending) cancelPreview()
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', handler)
    return () => document.removeEventListener('mousedown', handler)
  }, [hasPending, cancelPreview])

  const grouped = {
    Dark:    THEMES.filter(t => t.category === 'Dark'),
    Special: THEMES.filter(t => t.category === 'Special'),
    Light:   THEMES.filter(t => t.category === 'Light'),
  }

  const currentThemeObj  = THEMES.find(t => t.id === activeTheme)!
  const currentAccentObj = ACCENTS.find(a => a.id === activeAccent)!

  const TABS = [
    { id: 'theme',   label: 'Themes',  icon: '🎨' },
    { id: 'accent',  label: 'Accent',  icon: '◉' },
    { id: 'density', label: 'Density', icon: '▤' },
    { id: 'colors',  label: 'Colors',  icon: '⬤' },
  ] as const

  return (
    <div ref={ref} style={{ position: 'relative', display: 'inline-flex' }}>
      {/* Trigger */}
      <button onClick={() => setOpen(v => !v)} title="Theme Settings"
        style={{
          display: 'flex', alignItems: 'center', gap: 5,
          background: open ? 'rgba(255,255,255,0.08)' : 'rgba(255,255,255,0.025)',
          border: `1px solid ${open ? 'rgba(255,255,255,0.12)' : 'rgba(255,255,255,0.05)'}`,
          borderRadius: 20, padding: '4px 10px',
          cursor: 'pointer', color: 'rgba(255,255,255,0.6)',
          transition: 'all 0.15s',
        }}>
        <span style={{
          width: 10, height: 10, borderRadius: '50%',
          background: currentAccentObj.color,
          boxShadow: `0 0 5px ${currentAccentObj.glow}`,
          flexShrink: 0,
        }} />
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="1.5"/>
          <circle cx="8" cy="10" r="1.5" fill="currentColor"/>
          <circle cx="12" cy="8" r="1.5" fill="currentColor"/>
          <circle cx="16" cy="10" r="1.5" fill="currentColor"/>
          <circle cx="16" cy="14" r="1.5" fill="currentColor"/>
        </svg>
      </button>

      {/* Drawer */}
      {open && (
        <div style={{
          position: 'absolute', top: 'calc(100% + 8px)', right: 0,
          width: 330, maxHeight: 'calc(100vh - 80px)',
          background: 'rgba(13,19,32,0.95)',
          backdropFilter: 'blur(24px)', WebkitBackdropFilter: 'blur(24px)',
          border: '1px solid rgba(255,255,255,0.08)',
          borderRadius: 14, boxShadow: '0 24px 64px rgba(0,0,0,0.7)',
          zIndex: 9999, display: 'flex', flexDirection: 'column',
          overflow: 'hidden',
        }}>
          {/* Header */}
          <div style={{
            padding: '12px 16px 10px', display: 'flex', alignItems: 'center', justifyContent: 'space-between',
            borderBottom: '1px solid rgba(255,255,255,0.06)',
          }}>
            <div>
              <div style={{ color: '#fff', fontSize: 13, fontWeight: 700, fontFamily: "'Outfit', sans-serif" }}>
                Theme Studio
              </div>
              <div style={{ color: 'rgba(255,255,255,0.25)', fontSize: 9, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.05em', marginTop: 2 }}>
                {currentThemeObj.label} · {currentAccentObj.label} · {density}
              </div>
            </div>
            <button onClick={() => { if (hasPending) cancelPreview(); setOpen(false) }}
              style={{ background: 'none', border: 'none', color: 'rgba(255,255,255,0.3)', cursor: 'pointer', fontSize: 14, padding: 4 }}>✕</button>
          </div>

          {/* Tab bar */}
          <div style={{ display: 'flex', borderBottom: '1px solid rgba(255,255,255,0.05)', padding: '0 4px' }}>
            {TABS.map(t => (
              <button key={t.id} onClick={() => setTab(t.id as any)} style={{
                flex: 1, padding: '8px 0', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 4,
                background: 'none', border: 'none',
                borderBottom: `2px solid ${tab === t.id ? currentAccentObj.color : 'transparent'}`,
                color: tab === t.id ? '#fff' : 'rgba(255,255,255,0.3)',
                fontSize: 10, cursor: 'pointer', fontWeight: 600,
                letterSpacing: '0.04em', fontFamily: "'JetBrains Mono', monospace",
                transition: 'all 0.15s',
              }}>
                <span style={{ fontSize: 11 }}>{t.icon}</span>
                {t.label}
              </button>
            ))}
          </div>

          {/* Content */}
          <div style={{ flex: 1, overflowY: 'auto', padding: 12, minHeight: 0 }}>

            {/* ── THEMES ── */}
            {tab === 'theme' && Object.entries(grouped).map(([cat, items]) => (
              <div key={cat} style={{ marginBottom: 10 }}>
                <div style={{
                  color: 'rgba(255,255,255,0.2)', fontSize: 9,
                  fontFamily: "'JetBrains Mono', monospace",
                  letterSpacing: '0.1em', textTransform: 'uppercase',
                  marginBottom: 5, paddingLeft: 2, fontWeight: 700,
                }}>{cat} ({items.length})</div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
                  {items.map(t => {
                    const isActive = activeTheme === t.id
                    return (
                      <button key={t.id} onClick={() => previewTheme(t.id as ThemeId)}
                        style={{
                          display: 'flex', alignItems: 'center', gap: 10,
                          padding: '7px 10px', width: '100%', textAlign: 'left',
                          background: isActive ? 'rgba(255,255,255,0.06)' : 'transparent',
                          border: `1px solid ${isActive ? currentAccentObj.color + '30' : 'transparent'}`,
                          borderRadius: 8, cursor: 'pointer', transition: 'all 0.12s',
                        }}>
                        {/* Swatch */}
                        <div style={{
                          width: 32, height: 22, borderRadius: 5, flexShrink: 0,
                          background: `linear-gradient(135deg, ${t.bg} 45%, ${t.surface} 55%)`,
                          border: `1px solid ${isActive ? currentAccentObj.color + '40' : 'rgba(255,255,255,0.08)'}`,
                          position: 'relative', overflow: 'hidden',
                        }}>
                          {/* Accent dot inside swatch */}
                          <div style={{
                            position: 'absolute', bottom: 2, right: 2,
                            width: 5, height: 5, borderRadius: '50%',
                            background: currentAccentObj.color, opacity: 0.6,
                          }} />
                        </div>
                        <div style={{ flex: 1, minWidth: 0 }}>
                          <div style={{ color: isActive ? '#fff' : 'rgba(255,255,255,0.7)', fontSize: 12, fontWeight: 600, display: 'flex', alignItems: 'center', gap: 4 }}>
                            {t.emoji} {t.label}
                            {!t.dark && <span style={{ fontSize: 8, background: 'rgba(255,255,255,0.1)', padding: '1px 4px', borderRadius: 3, color: 'rgba(255,255,255,0.4)' }}>LIGHT</span>}
                          </div>
                          <div style={{ color: 'rgba(255,255,255,0.25)', fontSize: 9, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{t.desc}</div>
                        </div>
                        {isActive && (
                          <svg width="12" height="12" viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0 }}>
                            <path d="M20 6L9 17l-5-5" stroke={currentAccentObj.color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
                          </svg>
                        )}
                      </button>
                    )
                  })}
                </div>
              </div>
            ))}

            {/* ── ACCENTS ── */}
            {tab === 'accent' && (
              <div>
                <div style={{ color: 'rgba(255,255,255,0.2)', fontSize: 9, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: 8, fontWeight: 700 }}>ACCENT COLOR</div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 4 }}>
                  {ACCENTS.map(a => {
                    const isActive = activeAccent === a.id
                    return (
                      <button key={a.id} onClick={() => previewAccent(a.id as AccentId)} style={{
                        display: 'flex', alignItems: 'center', gap: 8,
                        padding: '8px 10px',
                        background: isActive ? `${a.color}12` : 'rgba(255,255,255,0.02)',
                        border: `1px solid ${isActive ? a.color + '40' : 'rgba(255,255,255,0.05)'}`,
                        borderRadius: 7, cursor: 'pointer', transition: 'all 0.12s',
                      }}>
                        <span style={{
                          width: 18, height: 18, borderRadius: '50%',
                          background: a.color,
                          boxShadow: isActive ? `0 0 10px ${a.glow}` : 'none',
                          flexShrink: 0, transition: 'box-shadow 0.15s',
                          border: isActive ? `2px solid ${a.color}` : '2px solid transparent',
                        }} />
                        <span style={{
                          color: isActive ? '#fff' : 'rgba(255,255,255,0.45)',
                          fontSize: 11, fontWeight: isActive ? 600 : 400,
                        }}>{a.label}</span>
                      </button>
                    )
                  })}
                </div>

                {/* Preview bar */}
                <div style={{
                  marginTop: 12, padding: 10, borderRadius: 8,
                  background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.04)',
                }}>
                  <div style={{ fontSize: 9, color: 'rgba(255,255,255,0.2)', marginBottom: 6, fontFamily: "'JetBrains Mono', monospace", fontWeight: 700, letterSpacing: '0.06em' }}>PREVIEW</div>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <div style={{ flex: 1, height: 24, borderRadius: 5, background: currentAccentObj.color, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 10, fontWeight: 700, color: '#000' }}>Button</div>
                    <div style={{ flex: 1, height: 24, borderRadius: 5, background: `${currentAccentObj.color}15`, border: `1px solid ${currentAccentObj.color}30`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 10, fontWeight: 600, color: currentAccentObj.color }}>Outline</div>
                    <div style={{ flex: 1, height: 24, borderRadius: 5, background: 'rgba(255,255,255,0.03)', border: '1px solid rgba(255,255,255,0.06)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 10, color: 'rgba(255,255,255,0.4)' }}>Ghost</div>
                  </div>
                </div>
              </div>
            )}

            {/* ── DENSITY ── */}
            {tab === 'density' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                <div style={{ color: 'rgba(255,255,255,0.2)', fontSize: 9, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: 4, fontWeight: 700 }}>UI DENSITY</div>
                {DENSITIES.map((d: DensityOption) => (
                  <button key={d.id} onClick={() => setDensity(d.id)} style={{
                    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                    padding: '10px 12px',
                    background: density === d.id ? 'rgba(255,255,255,0.06)' : 'transparent',
                    border: `1px solid ${density === d.id ? currentAccentObj.color + '25' : 'transparent'}`,
                    borderRadius: 7, cursor: 'pointer', textAlign: 'left', width: '100%',
                    transition: 'all 0.12s',
                  }}>
                    <div>
                      <div style={{ color: '#fff', fontSize: 12, fontWeight: 600 }}>{d.label}</div>
                      <div style={{ color: 'rgba(255,255,255,0.3)', fontSize: 10 }}>{d.desc}</div>
                    </div>
                    {density === d.id && (
                      <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
                        <path d="M20 6L9 17l-5-5" stroke={currentAccentObj.color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
                      </svg>
                    )}
                  </button>
                ))}
              </div>
            )}

            {/* ── COLORS (Buy/Sell) ── */}
            {tab === 'colors' && (
              <div>
                <div style={{ color: 'rgba(255,255,255,0.2)', fontSize: 9, fontFamily: "'JetBrains Mono', monospace", letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: 10, fontWeight: 700 }}>TRADING COLORS</div>

                {/* Buy color */}
                <div style={{ marginBottom: 14 }}>
                  <div style={{ color: 'rgba(255,255,255,0.4)', fontSize: 10, marginBottom: 6, fontWeight: 600 }}>Buy / Gain Color</div>
                  <div style={{ display: 'flex', gap: 6 }}>
                    {BUY_COLORS.map(c => (
                      <button key={c} onClick={() => setBuyColor(c)} style={{
                        width: 32, height: 32, borderRadius: 8, background: c, border: 'none', cursor: 'pointer',
                        outline: buyColor === c ? `2px solid #fff` : 'none',
                        outlineOffset: 2, transition: 'all 0.15s',
                        boxShadow: buyColor === c ? `0 0 12px ${c}60` : 'none',
                      }} />
                    ))}
                    <label style={{
                      width: 32, height: 32, borderRadius: 8, cursor: 'pointer',
                      background: 'rgba(255,255,255,0.04)', border: '1px dashed rgba(255,255,255,0.15)',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontSize: 12, color: 'rgba(255,255,255,0.3)',
                    }}>
                      +
                      <input type="color" value={buyColor} onChange={e => setBuyColor(e.target.value)}
                        style={{ position: 'absolute', opacity: 0, width: 0, height: 0 }} />
                    </label>
                  </div>
                </div>

                {/* Sell color */}
                <div style={{ marginBottom: 14 }}>
                  <div style={{ color: 'rgba(255,255,255,0.4)', fontSize: 10, marginBottom: 6, fontWeight: 600 }}>Sell / Loss Color</div>
                  <div style={{ display: 'flex', gap: 6 }}>
                    {SELL_COLORS.map(c => (
                      <button key={c} onClick={() => setSellColor(c)} style={{
                        width: 32, height: 32, borderRadius: 8, background: c, border: 'none', cursor: 'pointer',
                        outline: sellColor === c ? `2px solid #fff` : 'none',
                        outlineOffset: 2, transition: 'all 0.15s',
                        boxShadow: sellColor === c ? `0 0 12px ${c}60` : 'none',
                      }} />
                    ))}
                    <label style={{
                      width: 32, height: 32, borderRadius: 8, cursor: 'pointer',
                      background: 'rgba(255,255,255,0.04)', border: '1px dashed rgba(255,255,255,0.15)',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontSize: 12, color: 'rgba(255,255,255,0.3)',
                    }}>
                      +
                      <input type="color" value={sellColor} onChange={e => setSellColor(e.target.value)}
                        style={{ position: 'absolute', opacity: 0, width: 0, height: 0 }} />
                    </label>
                  </div>
                </div>

                {/* Preview */}
                <div style={{
                  padding: 10, borderRadius: 8,
                  background: 'rgba(255,255,255,0.02)', border: '1px solid rgba(255,255,255,0.04)',
                }}>
                  <div style={{ fontSize: 9, color: 'rgba(255,255,255,0.2)', marginBottom: 6, fontFamily: "'JetBrains Mono', monospace", fontWeight: 700 }}>PREVIEW</div>
                  <div style={{ display: 'flex', gap: 8, fontFamily: "'JetBrains Mono', monospace" }}>
                    <div style={{ flex: 1, textAlign: 'center' }}>
                      <div style={{ color: buyColor, fontSize: 14, fontWeight: 700 }}>▲ +2.45%</div>
                      <div style={{ color: 'rgba(255,255,255,0.2)', fontSize: 9 }}>Buy / Gain</div>
                    </div>
                    <div style={{ width: 1, background: 'rgba(255,255,255,0.06)' }} />
                    <div style={{ flex: 1, textAlign: 'center' }}>
                      <div style={{ color: sellColor, fontSize: 14, fontWeight: 700 }}>▼ -1.82%</div>
                      <div style={{ color: 'rgba(255,255,255,0.2)', fontSize: 9 }}>Sell / Loss</div>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* Footer bar */}
          {hasPending ? (
            <div style={{
              borderTop: '1px solid rgba(255,255,255,0.06)',
              padding: '10px 12px', display: 'flex', gap: 6,
              background: 'rgba(0,0,0,0.2)',
            }}>
              <button onClick={() => { confirmTheme(); setOpen(false) }} style={{
                flex: 1, padding: '8px 0',
                background: `linear-gradient(135deg, ${currentAccentObj.color}, ${currentAccentObj.color}cc)`,
                border: 'none', borderRadius: 7, color: '#000', fontSize: 11, fontWeight: 700, cursor: 'pointer',
                boxShadow: `0 4px 12px ${currentAccentObj.glow}`,
              }}>Apply Theme</button>
              <button onClick={cancelPreview} style={{
                flex: 1, padding: '8px 0',
                background: 'rgba(255,255,255,0.04)', border: '1px solid rgba(255,255,255,0.08)',
                borderRadius: 7, color: 'rgba(255,255,255,0.5)', fontSize: 11, cursor: 'pointer',
              }}>Cancel</button>
            </div>
          ) : (
            <div style={{
              borderTop: '1px solid rgba(255,255,255,0.04)',
              padding: '7px 12px', display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}>
              <span style={{ color: 'rgba(255,255,255,0.15)', fontSize: 9, fontFamily: "'JetBrains Mono', monospace" }}>
                Click to preview · Apply to save
              </span>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
