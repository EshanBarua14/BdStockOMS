// @ts-nocheck
// src/components/widgets/WidgetPanel.tsx

import React, { useRef, useEffect } from "react"
import type { ColorGroup } from "../../hooks/useDashboardPersistence"

const COLOR_RING: Record<string, string> = {
  teal:   "ring-1 ring-teal-500/50 border-teal-500/30",
  blue:   "ring-1 ring-blue-500/50 border-blue-500/30",
  amber:  "ring-1 ring-amber-500/50 border-amber-500/30",
  purple: "ring-1 ring-purple-500/50 border-purple-500/30",
  red:    "ring-1 ring-red-500/50 border-red-500/30",
}
const COLOR_DOT: Record<string, string> = {
  teal: "bg-teal-500", blue: "bg-blue-500", amber: "bg-amber-500",
  purple: "bg-purple-500", red: "bg-red-500",
}
const COLOR_OPTIONS = [
  { id: "teal",   label: "Teal",   hex: "#14b8a6" },
  { id: "blue",   label: "Blue",   hex: "#3b82f6" },
  { id: "amber",  label: "Amber",  hex: "#f59e0b" },
  { id: "purple", label: "Purple", hex: "#a855f7" },
  { id: "red",    label: "Red",    hex: "#ef4444" },
]

interface WidgetPanelProps {
  id: string
  title: string
  children: React.ReactNode
  colorGroup: ColorGroup
  onColorChange: (c: ColorGroup) => void
  onFullscreen: () => void
  onClose: () => void
  menuOpen: boolean
  onMenuToggle: () => void
}

export function WidgetPanel({
  id, title, children, colorGroup,
  onColorChange, onFullscreen, onClose,
  menuOpen, onMenuToggle,
}: WidgetPanelProps) {
  const menuRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!menuOpen) return
    const h = (e: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(e.target as Node)) onMenuToggle()
    }
    document.addEventListener("mousedown", h)
    return () => document.removeEventListener("mousedown", h)
  }, [menuOpen, onMenuToggle])

  return (
    <div className={`flex flex-col h-full bg-zinc-900 border border-zinc-800 rounded-lg overflow-hidden transition-all duration-150 ${colorGroup ? COLOR_RING[colorGroup] : ""}`}>
      {/* Header */}
      <div className="widget-drag-handle flex items-center justify-between px-2 py-1 border-b border-zinc-800 cursor-grab active:cursor-grabbing select-none flex-shrink-0 group bg-zinc-900/90">
        <div className="flex items-center gap-1.5 min-w-0">
          {colorGroup && <span className={`w-2 h-2 rounded-full flex-shrink-0 ${COLOR_DOT[colorGroup]}`} />}
          <span className="text-[10px] font-bold text-zinc-500 uppercase tracking-widest truncate">{title}</span>
        </div>

        {/* Controls — visible on hover */}
        <div className="flex items-center gap-0.5 opacity-0 group-hover:opacity-100 transition-opacity" onClick={e => e.stopPropagation()}>
          {/* Color group */}
          <div className="relative" ref={menuRef}>
            <button onClick={onMenuToggle}
              className="w-5 h-5 rounded flex items-center justify-center text-zinc-600 hover:text-zinc-300 hover:bg-zinc-700/60 transition-colors"
              title="Link group">
              <svg className="w-3 h-3" viewBox="0 0 12 12" fill="none">
                <circle cx="6" cy="6" r="2" stroke="currentColor" strokeWidth="1.2"/>
                <path d="M6 1v1.5M6 9.5V11M1 6h1.5M9.5 6H11" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
              </svg>
            </button>
            {menuOpen && (
              <div className="absolute right-0 top-6 z-[200] w-36 rounded-lg border border-zinc-700 shadow-2xl p-2" style={{ backgroundColor: "#18181b" }}>
                <div className="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider mb-1.5 px-1">Link group</div>
                {COLOR_OPTIONS.map(o => (
                  <button key={o.id} onClick={() => { onColorChange(colorGroup === o.id ? null : o.id as ColorGroup); onMenuToggle() }}
                    className={`flex items-center gap-2 w-full px-2 py-1.5 rounded text-xs transition-colors ${colorGroup === o.id ? "bg-zinc-700 text-zinc-100" : "text-zinc-400 hover:text-zinc-200 hover:bg-zinc-800"}`}>
                    <span className="w-2.5 h-2.5 rounded-full flex-shrink-0" style={{ backgroundColor: o.hex }} />
                    {o.label}
                    {colorGroup === o.id && <span className="ml-auto text-[10px] text-zinc-500">✓</span>}
                  </button>
                ))}
                <div className="border-t border-zinc-700/50 mt-1 pt-1">
                  <button onClick={() => { onColorChange(null); onMenuToggle() }}
                    className="flex items-center gap-2 w-full px-2 py-1.5 rounded text-xs text-zinc-500 hover:text-zinc-300 hover:bg-zinc-800 transition-colors">
                    <span className="w-2.5 h-2.5 rounded-full border border-zinc-600 flex-shrink-0" />
                    None
                  </button>
                </div>
              </div>
            )}
          </div>

          {/* Fullscreen */}
          <button onClick={onFullscreen} title="Fullscreen"
            className="w-5 h-5 rounded flex items-center justify-center text-zinc-600 hover:text-zinc-300 hover:bg-zinc-700/60 transition-colors">
            <svg className="w-3 h-3" viewBox="0 0 12 12" fill="none">
              <path d="M1 4.5V1h3.5M7.5 1H11v3.5M11 7.5V11H7.5M4.5 11H1V7.5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
          </button>

          {/* Close */}
          <button onClick={onClose} title="Hide widget"
            className="w-5 h-5 rounded flex items-center justify-center text-zinc-600 hover:text-red-400 hover:bg-red-500/10 transition-colors">
            <svg className="w-3 h-3" viewBox="0 0 12 12" fill="none">
              <path d="M1 1l10 10M11 1L1 11" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round"/>
            </svg>
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="flex-1 overflow-hidden min-h-0">{children}</div>
    </div>
  )
}
