// @ts-nocheck
import { useState, useEffect } from "react"
import { useSignalR } from "@/hooks/useSignalR"

const TYPE_COLORS = { info: "#3B82F6", success: "#00D4AA", warning: "#F59E0B", error: "#FF6B6B", trade: "#8B5CF6" }

export function NotificationsWidget() {
  const [notes, setNotes]   = useState([])
  const [filter, setFilter] = useState("All")
  const [unread, setUnread] = useState(0)

  useSignalR({
    hub: "notification",
    events: {
      ReceiveNotification: (msg) => {
        setNotes(prev => [{ id: Date.now(), ...msg, ts: new Date(), read: false }, ...prev.slice(0, 49)])
        setUnread(u => u + 1)
      },
      OrderUpdate: (msg) => {
        setNotes(prev => [{ id: Date.now(), type: "trade", title: "Order Update", message: msg, ts: new Date(), read: false }, ...prev.slice(0, 49)])
        setUnread(u => u + 1)
      },
    },
  })

  // Demo notifications on mount
  useEffect(() => {
    const demos = [
      { id: 1, type: "success", title: "System Ready", message: "BD Stock OMS connected to DSE live feed", ts: new Date(), read: false },
      { id: 2, type: "info",    title: "Market Open",  message: "DSE trading session started — 10:00 AM", ts: new Date(Date.now() - 60000), read: false },
    ]
    setNotes(demos)
    setUnread(2)
  }, [])

  const markAll = () => { setNotes(n => n.map(x => ({ ...x, read: true }))); setUnread(0) }

  const filtered = filter === "All" ? notes : notes.filter(n => n.type === filter.toLowerCase())

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>
      <div style={{ padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.06)", display: "flex", gap: 6, alignItems: "center", flexShrink: 0 }}>
        <span style={{ color: "rgba(255,255,255,0.5)", fontSize: 10, fontFamily: "'Space Mono',monospace" }}>NOTIFICATIONS</span>
        {unread > 0 && <span style={{ background: "#FF6B6B", color: "#fff", fontSize: 9, fontWeight: 700, padding: "1px 5px", borderRadius: 10 }}>{unread}</span>}
        <button onClick={markAll} style={{ marginLeft: "auto", background: "none", border: "none", color: "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>Mark all read</button>
      </div>
      <div style={{ padding: "4px 8px", borderBottom: "1px solid rgba(255,255,255,0.04)", display: "flex", gap: 4, flexShrink: 0 }}>
        {["All","Info","Success","Warning","Error","Trade"].map(f => (
          <button key={f} onClick={() => setFilter(f)} style={{ padding: "3px 7px", background: filter === f ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${filter === f ? "rgba(255,255,255,0.15)" : "transparent"}`, borderRadius: 4, color: filter === f ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 9, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{f}</button>
        ))}
      </div>
      <div style={{ flex: 1, overflowY: "auto" }}>
        {filtered.length === 0
          ? <div style={{ textAlign: "center", color: "rgba(255,255,255,0.2)", fontSize: 11, padding: 16, fontFamily: "'Space Mono',monospace" }}>No notifications</div>
          : filtered.map(n => (
            <div key={n.id} onClick={() => { setNotes(prev => prev.map(x => x.id === n.id ? { ...x, read: true } : x)); setUnread(u => Math.max(0, u - (n.read ? 0 : 1))) }}
              style={{ padding: "8px 10px", borderBottom: "1px solid rgba(255,255,255,0.03)", cursor: "pointer", background: n.read ? "transparent" : "rgba(255,255,255,0.02)", display: "flex", gap: 8, alignItems: "flex-start" }}>
              <span style={{ width: 6, height: 6, borderRadius: "50%", background: TYPE_COLORS[n.type] ?? "#fff", flexShrink: 0, marginTop: 4 }} />
              <div style={{ flex: 1 }}>
                <div style={{ color: "#fff", fontSize: 11, fontWeight: n.read ? 400 : 600 }}>{n.title}</div>
                <div style={{ color: "rgba(255,255,255,0.4)", fontSize: 10, marginTop: 2 }}>{n.message}</div>
                <div style={{ color: "rgba(255,255,255,0.2)", fontSize: 9, marginTop: 3, fontFamily: "'Space Mono',monospace" }}>{n.ts?.toLocaleTimeString()}</div>
              </div>
              {!n.read && <span style={{ width: 6, height: 6, borderRadius: "50%", background: TYPE_COLORS[n.type] ?? "#fff", flexShrink: 0 }} />}
            </div>
          ))
        }
      </div>
    </div>
  )
}
