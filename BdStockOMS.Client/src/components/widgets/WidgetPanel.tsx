// @ts-nocheck
// src/components/widgets/WidgetPanel.tsx
// Premium glass widget panel — all colors from CSS vars

import React, { useRef, useEffect } from "react"
import type { ColorGroup } from "../../hooks/useDashboardPersistence"

const COLOR_HEX: Record<string, string> = {
  teal: "#14b8a6", blue: "#3b82f6", amber: "#f59e0b", purple: "#a855f7", red: "#ef4444",
}
const COLOR_OPTIONS = [
  { id: "teal", label: "Teal", hex: "#14b8a6" },
  { id: "blue", label: "Blue", hex: "#3b82f6" },
  { id: "amber", label: "Amber", hex: "#f59e0b" },
  { id: "purple", label: "Purple", hex: "#a855f7" },
  { id: "red", label: "Red", hex: "#ef4444" },
]

interface WidgetPanelProps {
  id: string; title: string; children: React.ReactNode
  colorGroup: ColorGroup; onColorChange: (c: ColorGroup) => void
  onFullscreen: () => void; onClose: () => void
  menuOpen: boolean; onMenuToggle: () => void
}

function IconBtn({ onClick, title, children, danger = false }: any) {
  return (
    <button onClick={onClick} title={title} style={{
      width: 20, height: 20, borderRadius: 4,
      display: 'flex', alignItems: 'center', justifyContent: 'center',
      background: 'transparent', border: 'none', cursor: 'pointer',
      color: 'var(--t-text3)', transition: 'all 0.12s',
    }}
      onMouseEnter={e => {
        e.currentTarget.style.color = danger ? 'var(--t-sell)' : 'var(--t-text1)'
        e.currentTarget.style.background = danger ? 'rgba(255,23,68,0.08)' : 'var(--t-hover)'
      }}
      onMouseLeave={e => {
        e.currentTarget.style.color = 'var(--t-text3)'
        e.currentTarget.style.background = 'transparent'
      }}
    >{children}</button>
  )
}

export function WidgetPanel({
  id, title, children, colorGroup, onColorChange, onFullscreen, onClose, menuOpen, onMenuToggle,
}: WidgetPanelProps) {
  const menuRef = useRef<HTMLDivElement>(null)
  useEffect(() => {
    if (!menuOpen) return
    const h = (e: MouseEvent) => { if (menuRef.current && !menuRef.current.contains(e.target as Node)) onMenuToggle() }
    document.addEventListener("mousedown", h)
    return () => document.removeEventListener("mousedown", h)
  }, [menuOpen, onMenuToggle])

  const gc = colorGroup ? COLOR_HEX[colorGroup] : null

  return (
    <div style={{
      display: 'flex', flexDirection: 'column', height: '100%',
      background: 'var(--t-surface)', border: `1px solid ${gc ? gc + '35' : 'var(--t-border)'}`,
      borderRadius: 10, overflow: 'visible', transition: 'border-color 0.15s, box-shadow 0.15s',
      boxShadow: gc ? `0 0 12px ${gc}12` : 'none', position: 'relative',
    }}>
      {/* Neon top line */}
      <div style={{
        position: 'absolute', top: 0, left: gc ? '5%' : '15%', right: gc ? '5%' : '15%',
        height: 1, zIndex: 1, pointerEvents: 'none',
        background: gc ? `linear-gradient(90deg, transparent, ${gc}60, transparent)` : 'linear-gradient(90deg, transparent, var(--t-accent), transparent)',
        opacity: gc ? 0.7 : 0.12,
      }} />

      {/* Header */}
      <div className="widget-drag-handle" style={{
        display: 'flex', alignItems: 'center', justifyContent: 'space-between',
        padding: '0 8px', height: 28, flexShrink: 0,
        background: 'var(--t-panel)', borderBottom: '1px solid var(--t-border)',
        cursor: 'grab', userSelect: 'none',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 6, minWidth: 0 }}>
          <span style={{
            width: 5, height: 5, borderRadius: '50%', flexShrink: 0,
            background: gc || 'var(--t-accent)', boxShadow: `0 0 4px ${gc || 'var(--t-accent-glow)'}`, opacity: 0.7,
          }} />
          <span style={{
            fontSize: 10, fontWeight: 700, letterSpacing: '0.06em', textTransform: 'uppercase',
            color: 'var(--t-text2)', fontFamily: "'JetBrains Mono', monospace",
            overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap',
          }}>{title}</span>
          {colorGroup && gc && (
            <span title={`Link group: ${colorGroup}`} style={{
              width: 7, height: 7, borderRadius: '50%', flexShrink: 0,
              background: gc, boxShadow: `0 0 5px ${gc}`,
              display: 'inline-block', marginLeft: 1,
            }} />
          )}
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: 2 }} onClick={e => e.stopPropagation()}>
          <div style={{ position: 'relative' }} ref={menuRef}>
            <IconBtn onClick={onMenuToggle} title="Link group">
              <svg width="11" height="11" viewBox="0 0 12 12" fill="none"><circle cx="6" cy="6" r="2" stroke="currentColor" strokeWidth="1.2"/><path d="M6 1v1.5M6 9.5V11M1 6h1.5M9.5 6H11" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/></svg>
            </IconBtn>
            {menuOpen && (
              <div style={{
                position: 'absolute', right: 0, top: 24, zIndex: 200,
                width: 140, borderRadius: 10, padding: 6,
                background: 'var(--t-elevated)', border: '1px solid var(--t-border)',
                boxShadow: '0 12px 32px rgba(0,0,0,0.5)',
              }}>
                <div style={{ fontSize: 9, fontWeight: 700, color: 'var(--t-text3)', letterSpacing: '0.08em', padding: '2px 6px 6px', fontFamily: "'JetBrains Mono', monospace" }}>LINK GROUP</div>
                {COLOR_OPTIONS.map(o => (
                  <button key={o.id} onClick={() => { onColorChange(colorGroup === o.id ? null : o.id as ColorGroup); onMenuToggle() }}
                    style={{
                      display: 'flex', alignItems: 'center', gap: 8, width: '100%', padding: '5px 6px',
                      borderRadius: 6, background: colorGroup === o.id ? 'var(--t-hover)' : 'transparent',
                      border: 'none', cursor: 'pointer', color: colorGroup === o.id ? 'var(--t-text1)' : 'var(--t-text2)', fontSize: 11,
                    }}
                    onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
                    onMouseLeave={e => { if (colorGroup !== o.id) e.currentTarget.style.background = 'transparent' }}
                  >
                    <span style={{ width: 10, height: 10, borderRadius: '50%', background: o.hex }} />
                    {o.label}
                    {colorGroup === o.id && <span style={{ marginLeft: 'auto', fontSize: 9, color: 'var(--t-text3)' }}>✓</span>}
                  </button>
                ))}
                <div style={{ height: 1, background: 'var(--t-border)', margin: '4px 0' }} />
                <button onClick={() => { onColorChange(null); onMenuToggle() }}
                  style={{ display: 'flex', alignItems: 'center', gap: 8, width: '100%', padding: '5px 6px', borderRadius: 6, background: 'transparent', border: 'none', cursor: 'pointer', color: 'var(--t-text3)', fontSize: 11 }}
                  onMouseEnter={e => e.currentTarget.style.background = 'var(--t-hover)'}
                  onMouseLeave={e => e.currentTarget.style.background = 'transparent'}
                >
                  <span style={{ width: 10, height: 10, borderRadius: '50%', border: '1px solid var(--t-border)' }} />
                  None
                </button>
              </div>
            )}
          </div>
          <IconBtn onClick={onFullscreen} title="Fullscreen">
            <svg width="11" height="11" viewBox="0 0 12 12" fill="none"><path d="M1 4.5V1h3.5M7.5 1H11v3.5M11 7.5V11H7.5M4.5 11H1V7.5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/></svg>
          </IconBtn>
          <IconBtn onClick={onClose} title="Hide" danger>
            <svg width="11" height="11" viewBox="0 0 12 12" fill="none"><path d="M1 1l10 10M11 1L1 11" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/></svg>
          </IconBtn>
        </div>
      </div>

      {/* Content */}
      <div style={{ flex: 1, overflow: 'hidden', minHeight: 0, borderRadius: '0 0 10px 10px' }}>{children}</div>
    </div>
  )
}
