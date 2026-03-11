import { useState, useRef, useEffect } from 'react'
import { useThemeStore, THEMES, ACCENTS, DENSITIES, type ThemeId, type AccentId, type DensityId } from '@/store/themeStore'

function PaletteIcon() {
  return (
    <svg width="15" height="15" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
      <circle cx="13.5" cy="6.5" r=".5" fill="currentColor"/><circle cx="17.5" cy="10.5" r=".5" fill="currentColor"/>
      <circle cx="8.5" cy="7.5" r=".5" fill="currentColor"/><circle cx="6.5" cy="12.5" r=".5" fill="currentColor"/>
      <path d="M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10c.926 0 1.648-.746 1.648-1.688 0-.437-.18-.835-.437-1.125-.29-.289-.438-.652-.438-1.125a1.64 1.64 0 011.668-1.668h1.996c3.051 0 5.555-2.503 5.555-5.554C21.965 6.012 17.461 2 12 2z"/>
    </svg>
  )
}

function CheckIcon() {
  return (
    <svg width="10" height="10" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="3.5" strokeLinecap="round" strokeLinejoin="round">
      <polyline points="20 6 9 17 4 12"/>
    </svg>
  )
}

const THEME_PREVIEW: Record<string, { bg: string; border: string; text: string }> = {
  obsidian: { bg: '#080C18', border: 'rgba(255,255,255,.08)', text: '#EEF2FF' },
  midnight: { bg: '#030303', border: 'rgba(255,255,255,.07)', text: '#F8F8F8' },
  slate:    { bg: '#131620', border: 'rgba(148,163,184,.10)', text: '#E2E8F4' },
  aurora:   { bg: '#051428', border: 'rgba(56,189,248,.10)',  text: '#E0F2FE' },
  arctic:   { bg: '#FFFFFF', border: 'rgba(0,0,0,.10)',       text: '#0D1117' },
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <p style={{
      fontSize: 9.5, fontWeight: 700, letterSpacing: '.10em',
      textTransform: 'uppercase', color: 'var(--text-tertiary)',
      marginBottom: 8,
    }}>
      {children}
    </p>
  )
}

export function ThemePanel() {
  const [open, setOpen] = useState(false)
  const panelRef = useRef<HTMLDivElement>(null)
  const btnRef   = useRef<HTMLButtonElement>(null)
  const { theme, accent, density, setTheme, setAccent, setDensity, tickerEnabled, toggleTicker } = useThemeStore()

  useEffect(() => {
    function handle(e: MouseEvent) {
      if (
        panelRef.current && !panelRef.current.contains(e.target as Node) &&
        btnRef.current   && !btnRef.current.contains(e.target as Node)
      ) setOpen(false)
    }
    if (open) document.addEventListener('mousedown', handle)
    return () => document.removeEventListener('mousedown', handle)
  }, [open])

  useEffect(() => {
    function handleKey(e: KeyboardEvent) { if (e.key === 'Escape') setOpen(false) }
    if (open) document.addEventListener('keydown', handleKey)
    return () => document.removeEventListener('keydown', handleKey)
  }, [open])

  const currentTheme  = THEMES.find(t => t.id === theme)
  const currentAccent = ACCENTS.find(a => a.id === accent)

  return (
    <div style={{ position: 'relative' }}>
      <button
        ref={btnRef}
        className="btn btn-ghost btn-icon btn-sm"
        onClick={() => setOpen(o => !o)}
        title="Appearance settings"
        aria-label="Open appearance settings"
        style={{ color: open ? 'var(--accent-400)' : 'var(--text-secondary)' }}
      >
        <PaletteIcon />
      </button>

      {open && (
        <div
          ref={panelRef}
          className="animate-slide-down"
          style={{
            position: 'absolute', right: 0, top: 'calc(100% + 10px)',
            width: 300, zIndex: 'var(--z-dropdown)' as any,
            background: 'var(--bg-overlay)',
            border: '1px solid var(--border-strong)',
            borderRadius: 'var(--r-xl)',
            boxShadow: 'var(--shadow-xl)',
            overflow: 'hidden',
          }}
        >
          {/* Header */}
          <div style={{
            padding: '12px 16px',
            borderBottom: '1px solid var(--border-subtle)',
            display: 'flex', alignItems: 'center', justifyContent: 'space-between',
          }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <PaletteIcon />
              <span style={{ fontSize: 13, fontWeight: 600, color: 'var(--text-primary)' }}>Appearance</span>
            </div>
            <span style={{
              fontSize: 10.5, color: 'var(--accent-400)',
              background: 'var(--accent-glow)',
              border: '1px solid color-mix(in srgb, var(--accent-500) 35%, transparent)',
              borderRadius: 99, padding: '2px 8px', fontWeight: 600,
            }}>
              {currentTheme?.label} · {currentAccent?.label}
            </span>
          </div>

          <div style={{ padding: '14px 16px', display: 'flex', flexDirection: 'column', gap: 18 }}>

            {/* Theme Selector */}
            <div>
              <SectionLabel>Theme</SectionLabel>
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5,1fr)', gap: 6 }}>
                {THEMES.map(t => {
                  const preview = THEME_PREVIEW[t.id]
                  const isActive = theme === t.id
                  return (
                    <button key={t.id} onClick={() => setTheme(t.id as ThemeId)} title={t.label}
                      style={{
                        display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 5,
                        padding: '8px 4px 7px', borderRadius: 10,
                        border: `1.5px solid ${isActive ? 'var(--accent-500)' : 'var(--border-default)'}`,
                        background: isActive ? 'var(--accent-glow)' : 'var(--bg-elevated)',
                        cursor: 'pointer',
                        transition: 'all 120ms',
                        position: 'relative',
                      }}
                    >
                      {/* Mini preview swatch */}
                      <div style={{
                        width: 28, height: 18, borderRadius: 4,
                        background: preview.bg,
                        border: `1px solid ${preview.border}`,
                        display: 'flex', alignItems: 'flex-end', padding: '2px 3px', gap: 1,
                        overflow: 'hidden',
                      }}>
                        {[40,65,50,80,55].map((h, i) => (
                          <div key={i} style={{
                            flex: 1, borderRadius: '1px 1px 0 0',
                            height: `${h}%`,
                            background: isActive ? 'var(--accent-500)' : preview.text,
                            opacity: 0.4 + i * 0.12,
                          }}/>
                        ))}
                      </div>
                      <span style={{
                        fontSize: 9.5, fontWeight: isActive ? 600 : 400,
                        color: isActive ? 'var(--accent-300)' : 'var(--text-tertiary)',
                      }}>
                        {t.label}
                      </span>
                      {isActive && (
                        <span style={{
                          position: 'absolute', top: 3, right: 3,
                          width: 12, height: 12, borderRadius: '50%',
                          background: 'var(--accent-500)',
                          display: 'flex', alignItems: 'center', justifyContent: 'center',
                          color: '#fff',
                        }}>
                          <CheckIcon />
                        </span>
                      )}
                    </button>
                  )
                })}
              </div>
            </div>

            {/* Accent Colors */}
            <div>
              <SectionLabel>Accent Color</SectionLabel>
              <div style={{ display: 'flex', gap: 8 }}>
                {ACCENTS.map(a => {
                  const isActive = accent === a.id
                  return (
                    <button key={a.id} onClick={() => setAccent(a.id as AccentId)} title={a.label}
                      style={{
                        flex: 1, height: 28, borderRadius: 8,
                        background: a.color + (isActive ? '' : '40'),
                        border: `2px solid ${isActive ? a.color : 'transparent'}`,
                        cursor: 'pointer',
                        display: 'flex', alignItems: 'center', justifyContent: 'center',
                        transition: 'all 120ms',
                        boxShadow: isActive ? `0 0 12px ${a.color}60` : 'none',
                      }}
                    >
                      {isActive && <span style={{ color: '#fff', lineHeight: 1 }}><CheckIcon /></span>}
                    </button>
                  )
                })}
              </div>
              <div style={{ display: 'flex', gap: 8, marginTop: 5 }}>
                {ACCENTS.map(a => (
                  <span key={a.id} style={{
                    flex: 1, textAlign: 'center', fontSize: 9,
                    color: accent === a.id ? 'var(--accent-300)' : 'var(--text-tertiary)',
                    fontWeight: accent === a.id ? 600 : 400,
                  }}>
                    {a.label}
                  </span>
                ))}
              </div>
            </div>

            {/* Density */}
            <div>
              <SectionLabel>Data Density</SectionLabel>
              <div style={{ display: 'flex', flexDirection: 'column', gap: 4 }}>
                {DENSITIES.map(d => {
                  const isActive = density === d.id
                  return (
                    <button key={d.id} onClick={() => setDensity(d.id as DensityId)}
                      style={{
                        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
                        padding: '8px 12px', borderRadius: 8,
                        border: `1px solid ${isActive ? 'var(--accent-500)' : 'var(--border-default)'}`,
                        background: isActive ? 'var(--accent-glow)' : 'var(--bg-elevated)',
                        cursor: 'pointer', textAlign: 'left',
                        transition: 'all 120ms',
                      }}
                    >
                      <div>
                        <div style={{ fontSize: 12, fontWeight: 500, color: isActive ? 'var(--accent-300)' : 'var(--text-primary)' }}>
                          {d.label}
                        </div>
                        <div style={{ fontSize: 10, color: 'var(--text-tertiary)', marginTop: 1 }}>{d.desc}</div>
                      </div>
                      {isActive && <span style={{ color: 'var(--accent-400)' }}><CheckIcon /></span>}
                    </button>
                  )
                })}
              </div>
            </div>

            {/* Ticker toggle */}
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
              <div>
                <div style={{ fontSize: 12, fontWeight: 500, color: 'var(--text-primary)' }}>Market Ticker</div>
                <div style={{ fontSize: 10, color: 'var(--text-tertiary)', marginTop: 1 }}>Show live price bar</div>
              </div>
              <button
                onClick={toggleTicker}
                style={{
                  width: 40, height: 22, borderRadius: 99,
                  background: tickerEnabled ? 'var(--accent-600)' : 'var(--border-strong)',
                  border: 'none', cursor: 'pointer',
                  position: 'relative',
                  transition: 'background 200ms',
                  flexShrink: 0,
                }}
              >
                <span style={{
                  position: 'absolute', top: 2,
                  left: tickerEnabled ? 20 : 2,
                  width: 18, height: 18, borderRadius: '50%',
                  background: '#fff',
                  transition: 'left 200ms var(--ease-spring)',
                  boxShadow: '0 1px 4px rgba(0,0,0,.3)',
                }}/>
              </button>
            </div>
          </div>

          <div style={{
            padding: '8px 16px 10px',
            borderTop: '1px solid var(--border-subtle)',
            fontSize: 10.5, color: 'var(--text-tertiary)', textAlign: 'center',
          }}>
            Preferences saved automatically
          </div>
        </div>
      )}
    </div>
  )
}
