// @ts-nocheck
// src/components/ui/ThemeMenu.tsx
// Preview on click · Apply/Cancel buttons · Persists after refresh

import { useState, useRef, useEffect } from 'react'
import {
  useThemeStore, THEMES, ACCENTS, DENSITIES,
  type ThemeId, type AccentId,
} from '@/store/themeStore'

const BUY_PRESETS  = ['#00e676', '#00D4AA', '#10B981', '#22C55E', '#4ADE80']
const SELL_PRESETS = ['#ff1744', '#EF4444', '#F43F5E', '#E11D48', '#FF6B6B']

export function ThemeMenu({ variant = 'compact' }: { variant?: 'compact' | 'full' }) {
  const [open, setOpen] = useState(false)
  const [tab, setTab]   = useState<'theme' | 'accent' | 'density' | 'colors'>('theme')
  const ref = useRef<HTMLDivElement>(null)

  const store = useThemeStore()
  const {
    theme, accent, density, buyColor, sellColor,
    pendingTheme, pendingAccent,
    previewTheme, previewAccent,
    confirmTheme, cancelPreview,
    setDensity, setBuyColor, setSellColor,
  } = store

  const hasPending   = pendingTheme !== null || pendingAccent !== null
  const activeTheme  = pendingTheme ?? theme
  const activeAccent = pendingAccent ?? accent
  const tObj = THEMES.find(t => t.id === activeTheme)!
  const aObj = ACCENTS.find(a => a.id === activeAccent)!

  // Close on outside click — cancel preview if pending
  useEffect(() => {
    const h = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) {
        if (hasPending) cancelPreview()
        setOpen(false)
      }
    }
    document.addEventListener('mousedown', h)
    return () => document.removeEventListener('mousedown', h)
  }, [hasPending, cancelPreview])

  const grouped = {
    Dark:    THEMES.filter(t => t.category === 'Dark'),
    Special: THEMES.filter(t => t.category === 'Special'),
    Light:   THEMES.filter(t => t.category === 'Light'),
  }

  const TABS = [
    { id: 'theme',   label: 'Themes',  icon: '🎨' },
    { id: 'accent',  label: 'Accent',  icon: '◉' },
    { id: 'density', label: 'Layout',  icon: '▤' },
    { id: 'colors',  label: 'Colors',  icon: '⬤' },
  ] as const

  return (
    <div ref={ref} style={{ position: 'relative', display: 'inline-flex' }}>
      {/* ── Trigger Button ── */}
      <button onClick={() => setOpen(v => !v)} title="Theme Studio" style={{
        display: 'flex', alignItems: 'center', gap: 5,
        background: open ? 'var(--t-elevated)' : 'var(--t-hover)',
        border: `1px solid ${open ? aObj.color + '40' : 'var(--t-border)'}`,
        borderRadius: 20, padding: '4px 10px', cursor: 'pointer',
        color: 'var(--t-text2)', transition: 'all 0.15s',
      }}>
        <span style={{
          width: 10, height: 10, borderRadius: '50%',
          background: aObj.color, boxShadow: `0 0 5px ${aObj.glow}`, flexShrink: 0,
        }} />
        <svg width="13" height="13" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="1.5"/>
          <circle cx="8" cy="10" r="1.5" fill="currentColor"/>
          <circle cx="12" cy="8" r="1.5" fill="currentColor"/>
          <circle cx="16" cy="10" r="1.5" fill="currentColor"/>
          <circle cx="16" cy="14" r="1.5" fill="currentColor"/>
        </svg>
      </button>

      {/* ── Dropdown Panel ── */}
      {open && (
        <div style={{
          position: 'absolute', top: 'calc(100% + 8px)', right: 0,
          width: 340, maxHeight: 'calc(100vh - 80px)',
          background: 'var(--t-surface)', border: '1px solid var(--t-border)',
          borderRadius: 14, boxShadow: '0 24px 64px rgba(0,0,0,0.5)',
          zIndex: 9999, display: 'flex', flexDirection: 'column', overflow: 'hidden',
        }}>

          {/* Header */}
          <div style={{
            padding: '12px 16px 10px',
            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
            borderBottom: '1px solid var(--t-border)',
          }}>
            <div>
              <div style={{ color: 'var(--t-text1)', fontSize: 13, fontWeight: 700 }}>Theme Studio</div>
              <div style={{
                color: 'var(--t-text3)', fontSize: 9,
                fontFamily: "'JetBrains Mono', monospace", marginTop: 2,
              }}>
                {tObj.label} · {aObj.label} · {density}
                {hasPending && <span style={{ color: aObj.color, marginLeft: 4 }}>● previewing</span>}
              </div>
            </div>
            <button onClick={() => { if (hasPending) cancelPreview(); setOpen(false) }} style={{
              background: 'none', border: 'none', color: 'var(--t-text3)',
              cursor: 'pointer', fontSize: 14, padding: 4,
            }}>✕</button>
          </div>

          {/* Tab Bar */}
          <div style={{ display: 'flex', borderBottom: '1px solid var(--t-border)', padding: '0 4px' }}>
            {TABS.map(t => (
              <button key={t.id} onClick={() => setTab(t.id as any)} style={{
                flex: 1, padding: '8px 0',
                display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 4,
                background: 'none', border: 'none',
                borderBottom: `2px solid ${tab === t.id ? aObj.color : 'transparent'}`,
                color: tab === t.id ? 'var(--t-text1)' : 'var(--t-text3)',
                fontSize: 10, cursor: 'pointer', fontWeight: 600,
                fontFamily: "'JetBrains Mono', monospace", transition: 'all 0.15s',
              }}>
                <span style={{ fontSize: 11 }}>{t.icon}</span>{t.label}
              </button>
            ))}
          </div>

          {/* ── Scrollable Content ── */}
          <div style={{ flex: 1, overflowY: 'auto', padding: 12, minHeight: 0 }}>

            {/* THEMES TAB */}
            {tab === 'theme' && Object.entries(grouped).map(([cat, items]) => (
              <div key={cat} style={{ marginBottom: 10 }}>
                <div style={{
                  color: 'var(--t-text3)', fontSize: 9,
                  fontFamily: "'JetBrains Mono', monospace",
                  letterSpacing: '0.1em', marginBottom: 5, fontWeight: 700,
                }}>{cat.toUpperCase()} ({items.length})</div>

                {items.map(t => {
                  const isActive = activeTheme === t.id
                  return (
                    <button key={t.id} onClick={() => previewTheme(t.id as ThemeId)} style={{
                      display: 'flex', alignItems: 'center', gap: 10,
                      padding: '7px 10px', width: '100%', textAlign: 'left', marginBottom: 2,
                      background: isActive ? 'var(--t-hover)' : 'transparent',
                      border: `1px solid ${isActive ? aObj.color + '30' : 'transparent'}`,
                      borderRadius: 8, cursor: 'pointer', transition: 'all 0.12s',
                    }}>
                      {/* Swatch */}
                      <div style={{
                        width: 34, height: 22, borderRadius: 5, flexShrink: 0,
                        position: 'relative', overflow: 'hidden',
                        background: `linear-gradient(135deg, ${t.bg} 40%, ${t.surface} 60%)`,
                        border: `1px solid ${isActive ? aObj.color + '40' : 'var(--t-border)'}`,
                      }}>
                        <div style={{
                          position: 'absolute', bottom: 2, right: 2,
                          width: 5, height: 5, borderRadius: '50%',
                          background: aObj.color, opacity: 0.5,
                        }} />
                        {!t.dark && (
                          <div style={{
                            position: 'absolute', top: 2, left: 2,
                            width: 4, height: 4, borderRadius: '50%',
                            background: '#FFD700', opacity: 0.6,
                          }} />
                        )}
                      </div>
                      {/* Label */}
                      <div style={{ flex: 1, minWidth: 0 }}>
                        <div style={{
                          color: isActive ? 'var(--t-text1)' : 'var(--t-text2)',
                          fontSize: 12, fontWeight: 600,
                        }}>
                          {t.emoji} {t.label}
                          {!t.dark && (
                            <span style={{
                              fontSize: 8, background: 'var(--t-hover)',
                              padding: '1px 4px', borderRadius: 3,
                              color: 'var(--t-text3)', marginLeft: 4,
                            }}>LIGHT</span>
                          )}
                        </div>
                        <div style={{
                          color: 'var(--t-text3)', fontSize: 9,
                          overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
                        }}>{t.desc}</div>
                      </div>
                      {/* Checkmark */}
                      {isActive && (
                        <svg width="12" height="12" viewBox="0 0 24 24" fill="none" style={{ flexShrink: 0 }}>
                          <path d="M20 6L9 17l-5-5" stroke={aObj.color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
                        </svg>
                      )}
                    </button>
                  )
                })}
              </div>
            ))}

            {/* ACCENT TAB */}
            {tab === 'accent' && (
              <div>
                <div style={{
                  color: 'var(--t-text3)', fontSize: 9,
                  fontFamily: "'JetBrains Mono', monospace",
                  letterSpacing: '0.1em', marginBottom: 8, fontWeight: 700,
                }}>ACCENT COLOR</div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 4 }}>
                  {ACCENTS.map(a => {
                    const isActive = activeAccent === a.id
                    return (
                      <button key={a.id} onClick={() => previewAccent(a.id as AccentId)} style={{
                        display: 'flex', alignItems: 'center', gap: 8,
                        padding: '8px 10px',
                        background: isActive ? 'var(--t-hover)' : 'transparent',
                        border: `1px solid ${isActive ? a.color + '40' : 'var(--t-border)'}`,
                        borderRadius: 7, cursor: 'pointer', transition: 'all 0.12s',
                      }}>
                        <span style={{
                          width: 18, height: 18, borderRadius: '50%',
                          background: a.color, flexShrink: 0,
                          boxShadow: isActive ? `0 0 10px ${a.glow}` : 'none',
                        }} />
                        <span style={{
                          color: isActive ? 'var(--t-text1)' : 'var(--t-text2)',
                          fontSize: 11, fontWeight: isActive ? 600 : 400,
                        }}>{a.label}</span>
                      </button>
                    )
                  })}
                </div>
                {/* Preview strip */}
                <div style={{
                  marginTop: 12, padding: 10, borderRadius: 8,
                  background: 'var(--t-hover)', border: '1px solid var(--t-border)',
                }}>
                  <div style={{ fontSize: 9, color: 'var(--t-text3)', marginBottom: 6, fontFamily: "'JetBrains Mono', monospace", fontWeight: 700 }}>PREVIEW</div>
                  <div style={{ display: 'flex', gap: 4 }}>
                    <div style={{ flex: 1, height: 24, borderRadius: 5, background: aObj.color, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 10, fontWeight: 700, color: '#000' }}>Button</div>
                    <div style={{ flex: 1, height: 24, borderRadius: 5, background: `${aObj.color}15`, border: `1px solid ${aObj.color}30`, display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 10, fontWeight: 600, color: aObj.color }}>Outline</div>
                    <div style={{ flex: 1, height: 24, borderRadius: 5, background: 'var(--t-hover)', border: '1px solid var(--t-border)', display: 'flex', alignItems: 'center', justifyContent: 'center', fontSize: 10, color: 'var(--t-text3)' }}>Ghost</div>
                  </div>
                </div>
              </div>
            )}

            {/* DENSITY TAB */}
            {tab === 'density' && (
              <div>
                <div style={{
                  color: 'var(--t-text3)', fontSize: 9,
                  fontFamily: "'JetBrains Mono', monospace",
                  letterSpacing: '0.1em', marginBottom: 6, fontWeight: 700,
                }}>UI DENSITY</div>
                {DENSITIES.map(d => (
                  <button key={d.id} onClick={() => setDensity(d.id)} style={{
                    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                    padding: '10px 12px', width: '100%', textAlign: 'left', marginBottom: 3,
                    background: density === d.id ? 'var(--t-hover)' : 'transparent',
                    border: `1px solid ${density === d.id ? aObj.color + '25' : 'transparent'}`,
                    borderRadius: 7, cursor: 'pointer', transition: 'all 0.12s',
                  }}>
                    <div>
                      <div style={{ color: 'var(--t-text1)', fontSize: 12, fontWeight: 600 }}>{d.label}</div>
                      <div style={{ color: 'var(--t-text3)', fontSize: 10 }}>{d.desc}</div>
                    </div>
                    {density === d.id && (
                      <svg width="12" height="12" viewBox="0 0 24 24" fill="none">
                        <path d="M20 6L9 17l-5-5" stroke={aObj.color} strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round"/>
                      </svg>
                    )}
                  </button>
                ))}
              </div>
            )}

            {/* COLORS TAB */}
            {tab === 'colors' && (
              <div>
                <div style={{
                  color: 'var(--t-text3)', fontSize: 9,
                  fontFamily: "'JetBrains Mono', monospace",
                  letterSpacing: '0.1em', marginBottom: 10, fontWeight: 700,
                }}>TRADING COLORS</div>

                {/* Buy */}
                <div style={{ marginBottom: 14 }}>
                  <div style={{ color: 'var(--t-text2)', fontSize: 10, marginBottom: 6, fontWeight: 600 }}>Buy / Gain</div>
                  <div style={{ display: 'flex', gap: 6 }}>
                    {BUY_PRESETS.map(c => (
                      <button key={c} onClick={() => setBuyColor(c)} style={{
                        width: 30, height: 30, borderRadius: 7, background: c,
                        border: 'none', cursor: 'pointer',
                        outline: buyColor === c ? '2px solid var(--t-text1)' : 'none',
                        outlineOffset: 2,
                      }} />
                    ))}
                    <label style={{
                      width: 30, height: 30, borderRadius: 7, cursor: 'pointer',
                      background: 'var(--t-hover)', border: '1px dashed var(--t-border)',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontSize: 12, color: 'var(--t-text3)',
                    }}>
                      +
                      <input type="color" value={buyColor} onChange={e => setBuyColor(e.target.value)}
                        style={{ position: 'absolute', opacity: 0, width: 0, height: 0 }} />
                    </label>
                  </div>
                </div>

                {/* Sell */}
                <div style={{ marginBottom: 14 }}>
                  <div style={{ color: 'var(--t-text2)', fontSize: 10, marginBottom: 6, fontWeight: 600 }}>Sell / Loss</div>
                  <div style={{ display: 'flex', gap: 6 }}>
                    {SELL_PRESETS.map(c => (
                      <button key={c} onClick={() => setSellColor(c)} style={{
                        width: 30, height: 30, borderRadius: 7, background: c,
                        border: 'none', cursor: 'pointer',
                        outline: sellColor === c ? '2px solid var(--t-text1)' : 'none',
                        outlineOffset: 2,
                      }} />
                    ))}
                    <label style={{
                      width: 30, height: 30, borderRadius: 7, cursor: 'pointer',
                      background: 'var(--t-hover)', border: '1px dashed var(--t-border)',
                      display: 'flex', alignItems: 'center', justifyContent: 'center',
                      fontSize: 12, color: 'var(--t-text3)',
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
                  background: 'var(--t-hover)', border: '1px solid var(--t-border)',
                }}>
                  <div style={{ fontSize: 9, color: 'var(--t-text3)', marginBottom: 6, fontFamily: "'JetBrains Mono', monospace", fontWeight: 700 }}>PREVIEW</div>
                  <div style={{ display: 'flex', gap: 8, fontFamily: "'JetBrains Mono', monospace" }}>
                    <div style={{ flex: 1, textAlign: 'center' }}>
                      <div style={{ color: buyColor, fontSize: 14, fontWeight: 700 }}>▲ +2.45%</div>
                      <div style={{ color: 'var(--t-text3)', fontSize: 9 }}>Buy</div>
                    </div>
                    <div style={{ width: 1, background: 'var(--t-border)' }} />
                    <div style={{ flex: 1, textAlign: 'center' }}>
                      <div style={{ color: sellColor, fontSize: 14, fontWeight: 700 }}>▼ -1.82%</div>
                      <div style={{ color: 'var(--t-text3)', fontSize: 9 }}>Sell</div>
                    </div>
                  </div>
                </div>
              </div>
            )}
          </div>

          {/* ── Footer: Apply/Cancel (only when previewing) OR status ── */}
          {hasPending ? (
            <div style={{
              borderTop: '1px solid var(--t-border)',
              padding: '10px 12px', display: 'flex', gap: 6,
              background: 'var(--t-panel)',
            }}>
              <button onClick={() => { confirmTheme(); setOpen(false) }} style={{
                flex: 1, padding: '8px 0',
                background: aObj.color, border: 'none', borderRadius: 7,
                color: '#000', fontSize: 11, fontWeight: 700, cursor: 'pointer',
                boxShadow: `0 4px 12px ${aObj.glow}`,
              }}>
                Apply Theme
              </button>
              <button onClick={cancelPreview} style={{
                flex: 1, padding: '8px 0',
                background: 'var(--t-hover)', border: '1px solid var(--t-border)',
                borderRadius: 7, color: 'var(--t-text2)', fontSize: 11, cursor: 'pointer',
              }}>
                Cancel
              </button>
            </div>
          ) : (
            <div style={{
              borderTop: '1px solid var(--t-border)',
              padding: '7px 12px', textAlign: 'center',
            }}>
              <span style={{
                color: 'var(--t-text3)', fontSize: 9,
                fontFamily: "'JetBrains Mono', monospace",
              }}>
                Click to preview · Apply to save
              </span>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
