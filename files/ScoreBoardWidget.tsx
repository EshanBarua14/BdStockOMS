// @ts-nocheck
// src/components/widgets/ScoreBoardWidget.tsx
// Day 61 — ScoreBoard: sector-wise Gainer/Loser/Unchanged/Not-Traded/Value/Volume/Trade breakdown
// Matches XFL reference widget #9. Live data from useMarketData, demo fallback.

import { useMemo, useState } from "react"
import { useMarketData } from "@/hooks/useMarketData"

const mono = "'JetBrains Mono', monospace"

// ─── Demo sector data (fallback when no live data) ────────────────────────────
const DEMO_SECTORS = [
  { sector: "Bank",             gainers: 12, losers: 8,  unchanged: 3, notTraded: 1, value: 4821.4, volume: 98432000,  trades: 24310 },
  { sector: "Pharma & Chem",   gainers: 9,  losers: 11, unchanged: 2, notTraded: 0, value: 2134.7, volume: 43210000,  trades: 11240 },
  { sector: "Textile",          gainers: 7,  losers: 14, unchanged: 5, notTraded: 3, value: 1243.2, volume: 31240000,  trades: 8720  },
  { sector: "Engineering",      gainers: 5,  losers: 4,  unchanged: 2, notTraded: 1, value: 876.5,  volume: 18760000,  trades: 4320  },
  { sector: "Food & Allied",    gainers: 8,  losers: 3,  unchanged: 1, notTraded: 0, value: 1987.3, volume: 27430000,  trades: 6540  },
  { sector: "Fuel & Power",     gainers: 4,  losers: 7,  unchanged: 3, notTraded: 2, value: 3214.8, volume: 54320000,  trades: 12870 },
  { sector: "Telecom",          gainers: 2,  losers: 1,  unchanged: 1, notTraded: 0, value: 2876.4, volume: 62100000,  trades: 9870  },
  { sector: "IT",               gainers: 6,  losers: 3,  unchanged: 2, notTraded: 1, value: 432.1,  volume: 9870000,   trades: 2340  },
  { sector: "Insurance",        gainers: 11, losers: 9,  unchanged: 4, notTraded: 2, value: 987.6,  volume: 23400000,  trades: 5430  },
  { sector: "NBFI",             gainers: 6,  losers: 8,  unchanged: 3, notTraded: 1, value: 1432.7, volume: 31240000,  trades: 7650  },
  { sector: "Cement",           gainers: 3,  losers: 5,  unchanged: 2, notTraded: 0, value: 765.4,  volume: 14320000,  trades: 3210  },
  { sector: "Ceramics",         gainers: 2,  losers: 4,  unchanged: 1, notTraded: 2, value: 234.5,  volume: 4320000,   trades: 980   },
  { sector: "Tannery",          gainers: 3,  losers: 2,  unchanged: 1, notTraded: 0, value: 312.8,  volume: 6540000,   trades: 1430  },
  { sector: "Paper & Printing", gainers: 1,  losers: 3,  unchanged: 2, notTraded: 1, value: 87.4,   volume: 1870000,   trades: 430   },
  { sector: "Travel & Leisure", gainers: 4,  losers: 2,  unchanged: 1, notTraded: 1, value: 543.2,  volume: 9870000,   trades: 2100  },
  { sector: "Jute",             gainers: 2,  losers: 1,  unchanged: 0, notTraded: 2, value: 43.2,   volume: 870000,    trades: 210   },
]

const SORT_KEYS = ["sector","gainers","losers","unchanged","notTraded","value","volume","trades"] as const
type SortKey = typeof SORT_KEYS[number]

const COL_HEADERS: { key: SortKey; label: string; w: number; align: "left" | "right" }[] = [
  { key: "sector",     label: "SECTOR",      w: 140, align: "left"  },
  { key: "gainers",    label: "G▲",          w: 44,  align: "right" },
  { key: "losers",     label: "L▼",          w: 44,  align: "right" },
  { key: "unchanged",  label: "=",           w: 36,  align: "right" },
  { key: "notTraded",  label: "NT",          w: 36,  align: "right" },
  { key: "value",      label: "VAL(mn)",     w: 72,  align: "right" },
  { key: "volume",     label: "VOL",         w: 68,  align: "right" },
  { key: "trades",     label: "TRADES",      w: 60,  align: "right" },
]

function fmtVol(v: number) {
  return v >= 1e6 ? `${(v / 1e6).toFixed(1)}M` : v >= 1e3 ? `${(v / 1e3).toFixed(0)}K` : String(v)
}

// Build sector rows from live stock ticks
function buildSectorRows(stocks: any[]) {
  const map = new Map<string, any>()
  for (const s of stocks) {
    const sec = s.sector || s.companyName?.split(" ")[0] || "Other"
    if (!map.has(sec)) map.set(sec, { sector: sec, gainers: 0, losers: 0, unchanged: 0, notTraded: 0, value: 0, volume: 0, trades: 0 })
    const row = map.get(sec)!
    const chg = s.changePercent ?? 0
    const vol = s.volume ?? 0
    if (vol === 0) row.notTraded++
    else if (chg > 0) row.gainers++
    else if (chg < 0) row.losers++
    else row.unchanged++
    row.value  += (s.turnover ?? 0) / 1e6
    row.volume += vol
    row.trades += s.tradeCount ?? s.trades ?? 0
  }
  return Array.from(map.values())
}

export function ScoreBoardWidget({ onSymbolClick }: { onSymbolClick?: (sym: string) => void }) {
  const { ticksArray, connected } = useMarketData()
  const [sortKey, setSortKey] = useState<SortKey>("value")
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc")
  const [exch, setExch] = useState<"All" | "DSE" | "CSE">("All")

  const rows = useMemo(() => {
    const live = exch === "All" ? ticksArray : ticksArray.filter(s => s.exchange === exch)
    const base = live.length > 0 ? buildSectorRows(live) : DEMO_SECTORS
    return [...base].sort((a, b) => {
      const av = a[sortKey]
      const bv = b[sortKey]
      const cmp = typeof av === "number" ? av - bv : String(av).localeCompare(String(bv))
      return sortDir === "desc" ? -cmp : cmp
    })
  }, [ticksArray, sortKey, sortDir, exch])

  // Totals
  const totals = useMemo(() => rows.reduce((acc, r) => ({
    gainers:   acc.gainers   + r.gainers,
    losers:    acc.losers    + r.losers,
    unchanged: acc.unchanged + r.unchanged,
    notTraded: acc.notTraded + r.notTraded,
    value:     acc.value     + r.value,
    volume:    acc.volume    + r.volume,
    trades:    acc.trades    + r.trades,
  }), { gainers: 0, losers: 0, unchanged: 0, notTraded: 0, value: 0, volume: 0, trades: 0 }), [rows])

  const handleSort = (key: SortKey) => {
    if (sortKey === key) setSortDir(d => d === "asc" ? "desc" : "asc")
    else { setSortKey(key); setSortDir("desc") }
  }

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>

      {/* ── Toolbar ── */}
      <div style={{ display: "flex", alignItems: "center", gap: 6, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        <span style={{ fontSize: 9, fontWeight: 700, color: "var(--t-text3)", fontFamily: mono, letterSpacing: "0.08em" }}>EXCHANGE</span>
        {(["All", "DSE", "CSE"] as const).map(e => (
          <button key={e} onClick={() => setExch(e)} style={{
            padding: "2px 8px", fontSize: 10, fontFamily: mono, cursor: "pointer",
            borderRadius: 4, border: `1px solid ${exch === e ? "var(--t-accent)" : "var(--t-border)"}`,
            background: exch === e ? "var(--t-accent)" : "transparent",
            color: exch === e ? "#000" : "var(--t-text3)", fontWeight: exch === e ? 700 : 400,
            transition: "all 0.1s",
          }}>{e}</button>
        ))}
        <div style={{ flex: 1 }} />
        <span style={{ color: connected ? "var(--t-buy)" : "var(--t-sell)", fontSize: 9, fontFamily: mono }}>
          {connected ? "● LIVE" : "○ DEMO"}
        </span>
      </div>

      {/* ── Summary bar ── */}
      <div style={{ display: "flex", gap: 0, padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, flexWrap: "wrap", gap: 12 }}>
        {[
          { label: "Gainers",   val: totals.gainers,   color: "var(--t-buy)"  },
          { label: "Losers",    val: totals.losers,    color: "var(--t-sell)" },
          { label: "Unchanged", val: totals.unchanged, color: "var(--t-text3)"},
          { label: "NT",        val: totals.notTraded, color: "var(--t-text3)"},
        ].map(({ label, val, color }) => (
          <div key={label} style={{ display: "flex", flexDirection: "column", alignItems: "center" }}>
            <span style={{ fontSize: 14, fontWeight: 700, color, fontFamily: mono }}>{val}</span>
            <span style={{ fontSize: 8, color: "var(--t-text3)", fontFamily: mono }}>{label}</span>
          </div>
        ))}
        <div style={{ flex: 1 }} />
        {/* Breadth bar */}
        <div style={{ display: "flex", flexDirection: "column", justifyContent: "center", minWidth: 80 }}>
          <div style={{ display: "flex", height: 5, borderRadius: 3, overflow: "hidden", gap: 1, width: "100%" }}>
            <div style={{ flex: totals.gainers,   background: "var(--t-buy)",  borderRadius: "3px 0 0 3px" }} />
            <div style={{ flex: totals.unchanged, background: "rgba(255,255,255,0.15)" }} />
            <div style={{ flex: totals.losers,    background: "var(--t-sell)", borderRadius: "0 3px 3px 0" }} />
          </div>
          <span style={{ fontSize: 8, color: "var(--t-text3)", fontFamily: mono, marginTop: 2, textAlign: "center" }}>
            {rows.length} sectors
          </span>
        </div>
      </div>

      {/* ── Column headers ── */}
      <div style={{ display: "flex", background: "var(--t-panel)", borderBottom: "1px solid var(--t-border)", flexShrink: 0 }}>
        {COL_HEADERS.map(col => (
          <div key={col.key} onClick={() => handleSort(col.key)}
            style={{ width: col.w, minWidth: col.w, padding: "4px 6px", cursor: "pointer", userSelect: "none", display: "flex", alignItems: "center", gap: 3, justifyContent: col.align === "right" ? "flex-end" : "flex-start" }}>
            <span style={{ fontSize: 9, fontWeight: 700, fontFamily: mono, color: sortKey === col.key ? "var(--t-accent)" : "var(--t-text3)", whiteSpace: "nowrap" }}>
              {col.label}
            </span>
            {sortKey === col.key && <span style={{ fontSize: 8, color: "var(--t-accent)" }}>{sortDir === "desc" ? "▼" : "▲"}</span>}
          </div>
        ))}
      </div>

      {/* ── Rows ── */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {rows.map((row, i) => {
          const total = row.gainers + row.losers + row.unchanged + row.notTraded
          const gPct = total > 0 ? (row.gainers / total) * 100 : 0
          const lPct = total > 0 ? (row.losers  / total) * 100 : 0
          return (
            <div key={row.sector} style={{
              display: "flex", alignItems: "center",
              borderBottom: "1px solid var(--t-border)",
              background: i % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)",
              transition: "background 0.08s", position: "relative", overflow: "hidden",
            }}
              onMouseEnter={e => e.currentTarget.style.background = "var(--t-hover)"}
              onMouseLeave={e => e.currentTarget.style.background = i % 2 === 0 ? "transparent" : "rgba(255,255,255,0.012)"}
            >
              {/* Mini breadth bar as row background */}
              <div style={{ position: "absolute", left: 0, top: 0, bottom: 0, width: `${gPct}%`, background: "rgba(0,212,170,0.04)", pointerEvents: "none" }} />
              <div style={{ position: "absolute", right: 0, top: 0, bottom: 0, width: `${lPct}%`, background: "rgba(255,107,107,0.04)", pointerEvents: "none" }} />

              {/* Sector name */}
              <div style={{ width: 140, minWidth: 140, padding: "6px 8px" }}>
                <div style={{ fontSize: 11, fontWeight: 700, color: "var(--t-text1)", fontFamily: mono, whiteSpace: "nowrap", overflow: "hidden", textOverflow: "ellipsis" }}>{row.sector}</div>
                {/* mini bar */}
                <div style={{ display: "flex", height: 3, borderRadius: 2, overflow: "hidden", marginTop: 2, gap: 1 }}>
                  <div style={{ flex: row.gainers,   background: "var(--t-buy)"  }} />
                  <div style={{ flex: row.unchanged, background: "rgba(255,255,255,0.15)" }} />
                  <div style={{ flex: row.losers,    background: "var(--t-sell)" }} />
                </div>
              </div>

              {/* Gainers */}
              <div style={{ width: 44, minWidth: 44, padding: "6px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 11, fontWeight: 700, color: "var(--t-buy)", fontFamily: mono }}>{row.gainers}</span>
              </div>
              {/* Losers */}
              <div style={{ width: 44, minWidth: 44, padding: "6px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 11, fontWeight: 700, color: "var(--t-sell)", fontFamily: mono }}>{row.losers}</span>
              </div>
              {/* Unchanged */}
              <div style={{ width: 36, minWidth: 36, padding: "6px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 10, color: "var(--t-text3)", fontFamily: mono }}>{row.unchanged}</span>
              </div>
              {/* Not traded */}
              <div style={{ width: 36, minWidth: 36, padding: "6px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 10, color: "var(--t-text3)", fontFamily: mono }}>{row.notTraded}</span>
              </div>
              {/* Value */}
              <div style={{ width: 72, minWidth: 72, padding: "6px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 10, color: "var(--t-text2)", fontFamily: mono }}>{row.value.toFixed(1)}</span>
              </div>
              {/* Volume */}
              <div style={{ width: 68, minWidth: 68, padding: "6px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 10, color: "var(--t-text2)", fontFamily: mono }}>{fmtVol(row.volume)}</span>
              </div>
              {/* Trades */}
              <div style={{ width: 60, minWidth: 60, padding: "6px 6px", textAlign: "right" }}>
                <span style={{ fontSize: 10, color: "var(--t-text2)", fontFamily: mono }}>{fmtVol(row.trades)}</span>
              </div>
            </div>
          )
        })}
      </div>

      {/* ── Footer totals ── */}
      <div style={{ borderTop: "1px solid var(--t-border)", display: "flex", background: "var(--t-panel)", flexShrink: 0 }}>
        <div style={{ width: 140, minWidth: 140, padding: "5px 8px" }}>
          <span style={{ fontSize: 9, fontWeight: 700, color: "var(--t-text3)", fontFamily: mono }}>TOTAL</span>
        </div>
        <div style={{ width: 44, minWidth: 44, padding: "5px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 10, fontWeight: 700, color: "var(--t-buy)", fontFamily: mono }}>{totals.gainers}</span>
        </div>
        <div style={{ width: 44, minWidth: 44, padding: "5px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 10, fontWeight: 700, color: "var(--t-sell)", fontFamily: mono }}>{totals.losers}</span>
        </div>
        <div style={{ width: 36, minWidth: 36, padding: "5px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 10, color: "var(--t-text3)", fontFamily: mono }}>{totals.unchanged}</span>
        </div>
        <div style={{ width: 36, minWidth: 36, padding: "5px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 10, color: "var(--t-text3)", fontFamily: mono }}>{totals.notTraded}</span>
        </div>
        <div style={{ width: 72, minWidth: 72, padding: "5px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 10, color: "var(--t-text2)", fontFamily: mono }}>{totals.value.toFixed(1)}</span>
        </div>
        <div style={{ width: 68, minWidth: 68, padding: "5px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 10, color: "var(--t-text2)", fontFamily: mono }}>{fmtVol(totals.volume)}</span>
        </div>
        <div style={{ width: 60, minWidth: 60, padding: "5px 6px", textAlign: "right" }}>
          <span style={{ fontSize: 10, color: "var(--t-text2)", fontFamily: mono }}>{fmtVol(totals.trades)}</span>
        </div>
      </div>
    </div>
  )
}
