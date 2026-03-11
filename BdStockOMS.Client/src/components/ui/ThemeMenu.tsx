import { useState, useRef, useEffect } from 'react'
import {
  useThemeStore, THEMES, ACCENTS, DENSITIES,
  type ThemeId, type AccentId, type DensityOption,
} from '@/store/themeStore'

interface ThemeMenuProps {
  /** compact = icon-only trigger (for topbar), full = labeled (for sidebar) */
  variant?: 'compact' | 'full'
}

export function ThemeMenu({ variant = 'compact' }: ThemeMenuProps) {
  const [open, setOpen]       = useState(false)
  const [tab, setTab]         = useState<'theme' | 'accent' | 'density'>('theme')
  const ref                   = useRef<HTMLDivElement>(null)

  const {
    theme, accent, density,
    pendingTheme, pendingAccent,
    previewTheme, previewAccent,
    confirmTheme, cancelPreview,
    setDensity,
  } = useThemeStore()

  const hasPending = pendingTheme !== null || pendingAccent !== null
  const activeTheme  = pendingTheme  ?? theme
  const activeAccent = pendingAccent ?? accent

  // Close on outside click
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

  return (
    <div ref={ref} style={{ position: 'relative', display: 'inline-flex' }}>
      {/* Trigger button */}
      <button
        onClick={() => setOpen(v => !v)}
        title="Change theme"
        style={{
          display:     'flex',
          alignItems:  'center',
          gap:         6,
          background:  open ? 'rgba(255,255,255,0.08)' : 'rgba(255,255,255,0.04)',
          border:      `1px solid ${open ? 'rgba(255,255,255,0.15)' : 'rgba(255,255,255,0.08)'}`,
          borderRadius: 8,
          padding:     variant === 'full' ? '7px 12px' : '7px 10px',
          cursor:      'pointer',
          color:       'rgba(255,255,255,0.7)',
          fontSize:    13,
          transition:  'all 0.15s',
        }}
      >
        {/* Color swatch */}
        <span style={{
          width: 12, height: 12, borderRadius: '50%',
          background: currentAccentObj.color,
          boxShadow:  `0 0 6px ${currentAccentObj.glow}`,
          display:    'inline-block', flexShrink: 0,
        }} />
        {variant === 'full' && (
          <span style={{ fontSize: 12 }}>{currentThemeObj.label}</span>
        )}
        {/* Palette icon */}
        <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
          <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="1.5"/>
          <circle cx="8"  cy="10" r="1.5" fill="currentColor"/>
          <circle cx="12" cy="8"  r="1.5" fill="currentColor"/>
          <circle cx="16" cy="10" r="1.5" fill="currentColor"/>
          <circle cx="16" cy="14" r="1.5" fill="currentColor"/>
          <circle cx="8"  cy="14" r="1.5" fill="currentColor"/>
        </svg>
      </button>

      {/* Dropdown panel */}
      {open && (
        <div style={{
          position:   'absolute',
          top:        'calc(100% + 8px)',
          right:      0,
          width:      300,
          background: '#0D1320',
          border:     '1px solid rgba(255,255,255,0.1)',
          borderRadius: 12,
          boxShadow:  '0 24px 48px rgba(0,0,0,0.6)',
          zIndex:     1000,
          overflow:   'hidden',
        }}>
          {/* Tabs */}
          <div style={{ display: 'flex', borderBottom: '1px solid rgba(255,255,255,0.06)' }}>
            {(['theme','accent','density'] as const).map(t => (
              <button key={t} onClick={() => setTab(t)} style={{
                flex: 1, padding: '10px 0',
                background: tab === t ? 'rgba(255,255,255,0.05)' : 'none',
                border: 'none',
                borderBottom: tab === t ? `2px solid ${currentAccentObj.color}` : '2px solid transparent',
                color: tab === t ? '#fff' : 'rgba(255,255,255,0.35)',
                fontSize: 11, cursor: 'pointer', fontWeight: 600,
                letterSpacing: '0.06em', textTransform: 'uppercase',
                transition: 'all 0.15s',
                fontFamily: "'Space Mono', monospace",
              }}>
                {t}
              </button>
            ))}
          </div>

          <div style={{ padding: 14, maxHeight: 340, overflowY: 'auto' }}>
            {/* ── Theme tab ── */}
            {tab === 'theme' && Object.entries(grouped).map(([cat, items]) => (
              <div key={cat} style={{ marginBottom: 12 }}>
                <div style={{
                  color: 'rgba(255,255,255,0.25)', fontSize: 10,
                  fontFamily: "'Space Mono', monospace",
                  letterSpacing: '0.1em', textTransform: 'uppercase',
                  marginBottom: 6, paddingLeft: 2,
                }}>{cat}</div>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 3 }}>
                  {items.map(t => (
                    <button
                      key={t.id}
                      onClick={() => previewTheme(t.id as ThemeId)}
                      style={{
                        display:     'flex',
                        alignItems:  'center',
                        gap:         10,
                        padding:     '8px 10px',
                        background:  activeTheme === t.id ? 'rgba(255,255,255,0.08)' : 'transparent',
                        border:      `1px solid ${activeTheme === t.id ? 'rgba(255,255,255,0.15)' : 'transparent'}`,
                        borderRadius: 7,
                        cursor:      'pointer',
                        textAlign:   'left',
                        width:       '100%',
                        transition:  'all 0.12s',
                      }}
                    >
                      {/* Swatch */}
                      <div style={{
                        width: 28, height: 20, borderRadius: 4,
                        background: `linear-gradient(135deg, ${t.bg} 50%, ${t.surface})`,
                        border: '1px solid rgba(255,255,255,0.1)',
                        flexShrink: 0,
                      }} />
                      <div style={{ flex: 1 }}>
                        <div style={{ color: '#fff', fontSize: 12, fontWeight: 600 }}>{t.emoji} {t.label}</div>
                        <div style={{ color: 'rgba(255,255,255,0.3)', fontSize: 10 }}>{t.desc}</div>
                      </div>
                      {activeTheme === t.id && (
                        <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                          <path d="M20 6L9 17l-5-5" stroke={currentAccentObj.color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                        </svg>
                      )}
                    </button>
                  ))}
                </div>
              </div>
            ))}

            {/* ── Accent tab ── */}
            {tab === 'accent' && (
              <div>
                <div style={{ color: 'rgba(255,255,255,0.25)', fontSize: 10, fontFamily: "'Space Mono', monospace", letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: 10 }}>
                  Accent Color
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 6 }}>
                  {ACCENTS.map(a => (
                    <button key={a.id} onClick={() => previewAccent(a.id as AccentId)} style={{
                      display: 'flex', alignItems: 'center', gap: 8,
                      padding: '9px 10px',
                      background:   activeAccent === a.id ? 'rgba(255,255,255,0.08)' : 'transparent',
                      border:       `1px solid ${activeAccent === a.id ? a.color + '55' : 'rgba(255,255,255,0.06)'}`,
                      borderRadius: 7,
                      cursor:       'pointer',
                      transition:   'all 0.12s',
                    }}>
                      <span style={{
                        width: 16, height: 16, borderRadius: '50%',
                        background: a.color,
                        boxShadow:  activeAccent === a.id ? `0 0 8px ${a.glow}` : 'none',
                        flexShrink: 0,
                        transition: 'box-shadow 0.15s',
                      }} />
                      <span style={{
                        color:      activeAccent === a.id ? '#fff' : 'rgba(255,255,255,0.5)',
                        fontSize:   12,
                        fontWeight: activeAccent === a.id ? 600 : 400,
                      }}>{a.label}</span>
                    </button>
                  ))}
                </div>
              </div>
            )}

            {/* ── Density tab ── */}
            {tab === 'density' && (
              <div style={{ display: 'flex', flexDirection: 'column', gap: 6 }}>
                <div style={{ color: 'rgba(255,255,255,0.25)', fontSize: 10, fontFamily: "'Space Mono', monospace", letterSpacing: '0.1em', textTransform: 'uppercase', marginBottom: 4 }}>
                  UI Density
                </div>
                {DENSITIES.map((d: DensityOption) => (
                  <button key={d.id} onClick={() => setDensity(d.id)} style={{
                    display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                    padding: '10px 12px',
                    background:   density === d.id ? 'rgba(255,255,255,0.07)' : 'transparent',
                    border:       `1px solid ${density === d.id ? 'rgba(255,255,255,0.12)' : 'transparent'}`,
                    borderRadius: 7,
                    cursor:       'pointer',
                    textAlign:    'left',
                    width:        '100%',
                    transition:   'all 0.12s',
                  }}>
                    <div>
                      <div style={{ color: '#fff', fontSize: 12, fontWeight: 600 }}>{d.label}</div>
                      <div style={{ color: 'rgba(255,255,255,0.35)', fontSize: 11 }}>{d.desc}</div>
                    </div>
                    {density === d.id && (
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                        <path d="M20 6L9 17l-5-5" stroke={currentAccentObj.color} strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                      </svg>
                    )}
                  </button>
                ))}
              </div>
            )}
          </div>

          {/* Confirm / Cancel bar — only when there is a pending preview */}
          {hasPending && (
            <div style={{
              borderTop:  '1px solid rgba(255,255,255,0.08)',
              padding:    '10px 14px',
              display:    'flex',
              gap:        8,
              background: 'rgba(0,0,0,0.3)',
            }}>
              <button onClick={() => { confirmTheme(); setOpen(false) }} style={{
                flex: 1, padding: '8px 0',
                background: currentAccentObj.color,
                border:     'none', borderRadius: 7,
                color:      '#000', fontSize: 12, fontWeight: 700,
                cursor:     'pointer',
              }}>
                Apply Theme
              </button>
              <button onClick={cancelPreview} style={{
                flex: 1, padding: '8px 0',
                background: 'rgba(255,255,255,0.06)',
                border:     '1px solid rgba(255,255,255,0.1)',
                borderRadius: 7,
                color:      'rgba(255,255,255,0.6)',
                fontSize:   12,
                cursor:     'pointer',
              }}>
                Cancel
              </button>
            </div>
          )}

          {/* Current theme label */}
          {!hasPending && (
            <div style={{
              borderTop:  '1px solid rgba(255,255,255,0.06)',
              padding:    '8px 14px',
              display:    'flex',
              alignItems: 'center',
              justifyContent: 'space-between',
            }}>
              <span style={{ color: 'rgba(255,255,255,0.25)', fontSize: 10, fontFamily: "'Space Mono', monospace" }}>
                {currentThemeObj.label} · {currentAccentObj.label}
              </span>
              <span style={{ fontSize: 10, color: 'rgba(255,255,255,0.2)' }}>
                Hover to preview, click to select
              </span>
            </div>
          )}
        </div>
      )}
    </div>
  )
}
