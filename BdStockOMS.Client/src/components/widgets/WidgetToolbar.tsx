// @ts-nocheck
import { useState } from "react"
import { WIDGET_REGISTRY, CATEGORIES } from "./registry"

interface Props {
  activeIds:   string[]
  onToggle:    (id: string) => void
  onReset:     () => void
  onSave:      () => void
}

export function WidgetToolbar({ activeIds, onToggle, onReset, onSave }: Props) {
  const [open, setOpen]   = useState(false)
  const [cat, setCat]     = useState("All")
  const [search, setSearch] = useState("")

  const filtered = WIDGET_REGISTRY.filter(w =>
    (cat === "All" || w.category === cat) &&
    (search === "" || w.label.toLowerCase().includes(search.toLowerCase()))
  )

  return (
    <div style={{ position: "relative" }}>
      {/* Toggle button */}
      <button onClick={() => setOpen(v => !v)} title="Add/Remove Widgets"
        style={{ display: "flex", alignItems: "center", gap: 6, padding: "6px 12px", background: open ? "rgba(0,212,170,0.15)" : "rgba(255,255,255,0.05)", border: `1px solid ${open ? "rgba(0,212,170,0.4)" : "rgba(255,255,255,0.1)"}`, borderRadius: 7, color: open ? "#00D4AA" : "rgba(255,255,255,0.6)", fontSize: 12, cursor: "pointer", fontFamily: "'Space Mono',monospace", transition: "all 0.15s" }}>
        <span style={{ fontSize: 14 }}>⊞</span>
        Widgets
        <span style={{ background: "#00D4AA", color: "#000", fontSize: 9, fontWeight: 700, padding: "1px 5px", borderRadius: 10 }}>{activeIds.length}</span>
      </button>

      {/* Dropdown panel */}
      {open && (
        <div style={{ position: "absolute", top: "calc(100% + 8px)", left: 0, width: 340, background: "#0D1320", border: "1px solid rgba(255,255,255,0.1)", borderRadius: 12, boxShadow: "0 24px 48px rgba(0,0,0,0.6)", zIndex: 200, overflow: "hidden" }}>
          {/* Search */}
          <div style={{ padding: "10px 12px", borderBottom: "1px solid rgba(255,255,255,0.06)" }}>
            <input value={search} onChange={e => setSearch(e.target.value)} placeholder="Search widgets…"
              style={{ width: "100%", boxSizing: "border-box", background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 6, padding: "6px 10px", color: "#fff", fontSize: 12, outline: "none" }} />
          </div>

          {/* Category tabs */}
          <div style={{ display: "flex", gap: 4, padding: "6px 12px", borderBottom: "1px solid rgba(255,255,255,0.06)", overflowX: "auto" }}>
            {["All", ...CATEGORIES].map(c => (
              <button key={c} onClick={() => setCat(c)} style={{ padding: "3px 8px", background: cat === c ? "rgba(0,212,170,0.15)" : "none", border: `1px solid ${cat === c ? "rgba(0,212,170,0.3)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: cat === c ? "#00D4AA" : "rgba(255,255,255,0.35)", fontSize: 10, cursor: "pointer", whiteSpace: "nowrap", fontFamily: "'Space Mono',monospace" }}>{c}</button>
            ))}
          </div>

          {/* Widget list */}
          <div style={{ maxHeight: 320, overflowY: "auto", padding: "6px 0" }}>
            {filtered.map(w => {
              const active = activeIds.includes(w.id)
              return (
                <div key={w.id} onClick={() => onToggle(w.id)}
                  style={{ display: "flex", alignItems: "center", gap: 10, padding: "7px 14px", cursor: "pointer", background: active ? "rgba(0,212,170,0.05)" : "transparent", transition: "background 0.1s" }}
                  onMouseEnter={e => e.currentTarget.style.background = active ? "rgba(0,212,170,0.08)" : "rgba(255,255,255,0.03)"}
                  onMouseLeave={e => e.currentTarget.style.background = active ? "rgba(0,212,170,0.05)" : "transparent"}>
                  <span style={{ fontSize: 16, flexShrink: 0 }}>{w.icon}</span>
                  <div style={{ flex: 1 }}>
                    <div style={{ color: active ? "#00D4AA" : "#fff", fontSize: 12, fontWeight: active ? 600 : 400 }}>{w.label}</div>
                    <div style={{ color: "rgba(255,255,255,0.25)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>{w.category} · {w.defaultW}×{w.defaultH}</div>
                  </div>
                  <div style={{ width: 18, height: 18, borderRadius: "50%", border: `2px solid ${active ? "#00D4AA" : "rgba(255,255,255,0.15)"}`, background: active ? "#00D4AA" : "transparent", display: "flex", alignItems: "center", justifyContent: "center", flexShrink: 0, transition: "all 0.15s" }}>
                    {active && <svg width="10" height="10" viewBox="0 0 12 12"><path d="M2 6l3 3 5-5" stroke="#000" strokeWidth="2" strokeLinecap="round" fill="none"/></svg>}
                  </div>
                </div>
              )
            })}
          </div>

          {/* Footer */}
          <div style={{ borderTop: "1px solid rgba(255,255,255,0.06)", padding: "8px 12px", display: "flex", gap: 6 }}>
            <button onClick={onSave} style={{ flex: 1, padding: "7px", background: "rgba(0,212,170,0.15)", border: "1px solid rgba(0,212,170,0.3)", borderRadius: 6, color: "#00D4AA", fontSize: 11, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>Save Layout</button>
            <button onClick={onReset} style={{ padding: "7px 12px", background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 6, color: "rgba(255,255,255,0.4)", fontSize: 11, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>Reset</button>
          </div>
        </div>
      )}
    </div>
  )
}
