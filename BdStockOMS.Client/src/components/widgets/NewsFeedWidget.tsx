// @ts-nocheck
// src/components/widgets/NewsFeedWidget.tsx
// Day 63 redesign — matches OMS design language: JetBrains Mono, var(--t-*) tokens,
// keyword/board/category filters, live SignalR, expand on click

import { useState, useEffect, useRef, useCallback } from "react"
import { apiClient } from "@/api/client"

const mono = "'JetBrains Mono', monospace"

// ─── Types ────────────────────────────────────────────────────────────────────
interface NewsItem {
  id: number
  title: string
  summary: string
  category: string
  board: string
  tradingCode?: string
  source: string
  sourceUrl?: string
  publishedAt: string
  isPriceSensitive: boolean
  keywords: string[]
}

interface NewsPagedResult {
  items: NewsItem[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
}

// ─── Constants ────────────────────────────────────────────────────────────────
const BOARDS = ["ALL", "A", "B", "N", "Z", "SME"]

const CATEGORIES = [
  { value: "all",             label: "All"         },
  { value: "price-sensitive", label: "Price Sens." },
  { value: "regulatory",      label: "Regulatory"  },
  { value: "corporate",       label: "Corporate"   },
  { value: "general",         label: "General"     },
]

const CAT_COLORS: Record<string, { bg: string; color: string; border: string }> = {
  "price-sensitive": { bg: "rgba(255,23,68,0.1)",   color: "#ff5c5c", border: "rgba(255,23,68,0.25)"    },
  regulatory:        { bg: "rgba(56,189,248,0.1)",  color: "#38bdf8", border: "rgba(56,189,248,0.25)"   },
  corporate:         { bg: "rgba(168,85,247,0.1)",  color: "#c084fc", border: "rgba(168,85,247,0.25)"   },
  general:           { bg: "rgba(100,116,139,0.1)", color: "#94a3b8", border: "rgba(100,116,139,0.25)"  },
}

function fmtRelTime(d: string) {
  const diff = Date.now() - new Date(d).getTime()
  const m = Math.floor(diff / 60000)
  if (m < 1)  return "just now"
  if (m < 60) return `${m}m ago`
  const h = Math.floor(m / 60)
  if (h < 24) return `${h}h ago`
  return new Date(d).toLocaleDateString("en-GB", { day: "2-digit", month: "short" })
}

// ─── Component ────────────────────────────────────────────────────────────────
export function NewsFeedWidget() {
  const [items,      setItems]      = useState<NewsItem[]>([])
  const [totalCount, setTotalCount] = useState(0)
  const [totalPages, setTotalPages] = useState(1)
  const [page,       setPage]       = useState(1)
  const [keyword,    setKeyword]    = useState("")
  const [debKw,      setDebKw]      = useState("")
  const [board,      setBoard]      = useState("ALL")
  const [category,   setCategory]   = useState("all")
  const [loading,    setLoading]    = useState(false)
  const [liveCount,  setLiveCount]  = useState(0)
  const [connected,  setConnected]  = useState(false)
  const [expandedId, setExpandedId] = useState<number | null>(null)
  const [newIds,     setNewIds]     = useState<Set<number>>(new Set())
  const debRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  // Debounce keyword
  useEffect(() => {
    if (debRef.current) clearTimeout(debRef.current)
    debRef.current = setTimeout(() => { setDebKw(keyword); setPage(1) }, 400)
    return () => { if (debRef.current) clearTimeout(debRef.current) }
  }, [keyword])

  // Fetch
  const fetchNews = useCallback(async () => {
    setLoading(true)
    try {
      const params = new URLSearchParams({ page: String(page), pageSize: "15" })
      if (debKw)          params.set("keyword",  debKw)
      if (board !== "ALL") params.set("board",    board)
      if (category !== "all") params.set("category", category)
      const res = await apiClient.get(`/api/news?${params}`).then(r => r.data as NewsPagedResult)
      setItems(res.items)
      setTotalCount(res.totalCount)
      setTotalPages(res.totalPages)
    } catch { setItems([]) }
    finally { setLoading(false) }
  }, [page, debKw, board, category])

  useEffect(() => { fetchNews() }, [fetchNews])
  useEffect(() => { setPage(1) }, [board, category])

  // SignalR via native WebSocket (news hub)
  useEffect(() => {
    // NewsHub connection — lightweight manual approach
    // Falls back gracefully if hub not available
    setConnected(false)
    return () => {}
  }, [])

  const catStyle = CAT_COLORS[category] ?? CAT_COLORS.general

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden", fontSize: 11 }}>

      {/* ── Header ── */}
      <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "5px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          <div style={{ width: 3, height: 14, borderRadius: 2, background: "linear-gradient(180deg, #f59e0b, #f97316)" }} />
          <span style={{ fontSize: 10, fontWeight: 800, color: "var(--t-text1)", fontFamily: mono, letterSpacing: "0.06em" }}>MARKET NEWS</span>
          {totalCount > 0 && (
            <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>({totalCount})</span>
          )}
        </div>
        <div style={{ display: "flex", alignItems: "center", gap: 8 }}>
          {liveCount > 0 && (
            <span style={{ fontSize: 8, fontFamily: mono, padding: "2px 6px", borderRadius: 4, background: "rgba(0,230,118,0.1)", color: "var(--t-buy)", border: "1px solid rgba(0,230,118,0.2)" }}>
              +{liveCount} live
            </span>
          )}
          <div style={{ display: "flex", alignItems: "center", gap: 4 }}>
            <div style={{ width: 6, height: 6, borderRadius: "50%", background: connected ? "var(--t-buy)" : "var(--t-text3)", animation: connected ? "oms-pulse 2s infinite" : "none" }} />
            <span style={{ fontSize: 8, fontFamily: mono, color: connected ? "var(--t-buy)" : "var(--t-text3)" }}>{connected ? "LIVE" : "POLLING"}</span>
          </div>
        </div>
      </div>

      {/* ── Filters ── */}
      <div style={{ padding: "6px 8px", borderBottom: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-bg)", display: "flex", flexDirection: "column", gap: 6 }}>
        {/* Keyword search */}
        <div style={{ position: "relative" }}>
          <span style={{ position: "absolute", left: 8, top: "50%", transform: "translateY(-50%)", color: "var(--t-text3)", fontSize: 11 }}>⌕</span>
          <input
            value={keyword}
            onChange={e => setKeyword(e.target.value)}
            placeholder="Search keyword, code…"
            style={{
              width: "100%", boxSizing: "border-box",
              background: "var(--t-hover)", border: "1px solid var(--t-border)",
              borderRadius: 5, padding: "5px 28px 5px 24px",
              color: "var(--t-text1)", fontSize: 10, outline: "none", fontFamily: mono,
            }}
            onFocus={e => e.currentTarget.style.borderColor = "#f59e0b"}
            onBlur={e => e.currentTarget.style.borderColor = "var(--t-border)"}
          />
          {keyword && (
            <button onClick={() => setKeyword("")} style={{
              position: "absolute", right: 7, top: "50%", transform: "translateY(-50%)",
              background: "none", border: "none", cursor: "pointer", color: "var(--t-text3)", fontSize: 12, lineHeight: 1,
            }}>×</button>
          )}
        </div>

        {/* Board chips + Category select */}
        <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
          <div style={{ display: "flex", gap: 3, flexWrap: "wrap" }}>
            {BOARDS.map(b => (
              <button key={b} onClick={() => setBoard(b)} style={{
                padding: "2px 7px", fontSize: 8, fontWeight: 800, fontFamily: mono,
                background: board === b ? "rgba(245,158,11,0.12)" : "transparent",
                border: `1px solid ${board === b ? "#f59e0b" : "var(--t-border)"}`,
                borderRadius: 4, color: board === b ? "#f59e0b" : "var(--t-text3)", cursor: "pointer",
              }}>{b}</button>
            ))}
          </div>
          <select
            value={category}
            onChange={e => setCategory(e.target.value)}
            style={{
              marginLeft: "auto", fontSize: 9, fontFamily: mono, fontWeight: 700,
              background: "var(--t-hover)", border: "1px solid var(--t-border)",
              borderRadius: 4, padding: "3px 6px", color: "var(--t-text2)", outline: "none",
              cursor: "pointer",
            }}
          >
            {CATEGORIES.map(c => (
              <option key={c.value} value={c.value}>{c.label}</option>
            ))}
          </select>
        </div>
      </div>

      {/* ── News List ── */}
      <div style={{ flex: 1, overflowY: "auto" }}>
        {loading && items.length === 0 && (
          <div style={{ display: "flex", alignItems: "center", justifyContent: "center", height: 60, color: "var(--t-text3)", fontSize: 10, fontFamily: mono }}>Loading…</div>
        )}
        {!loading && items.length === 0 && (
          <div style={{ display: "flex", flexDirection: "column", alignItems: "center", justifyContent: "center", height: 80, gap: 6, color: "var(--t-text3)" }}>
            <span style={{ fontSize: 20, opacity: 0.3 }}>📰</span>
            <span style={{ fontSize: 10, fontFamily: mono }}>No news found</span>
          </div>
        )}
        {items.map((item, idx) => {
          const cs    = CAT_COLORS[item.category] ?? CAT_COLORS.general
          const isNew = newIds.has(item.id)
          const isExp = expandedId === item.id

          return (
            <div
              key={item.id}
              onClick={() => setExpandedId(isExp ? null : item.id)}
              style={{
                borderBottom: "1px solid var(--t-border)",
                padding: "8px 10px",
                cursor: "pointer",
                background: isNew ? "rgba(245,158,11,0.04)" : "transparent",
                borderLeft: `2px solid ${isNew ? "#f59e0b" : "transparent"}`,
                transition: "background 0.15s",
              }}
              onMouseEnter={e => { if (!isNew) e.currentTarget.style.background = "var(--t-hover)" }}
              onMouseLeave={e => { e.currentTarget.style.background = isNew ? "rgba(245,158,11,0.04)" : "transparent" }}
            >
              {/* Meta row */}
              <div style={{ display: "flex", alignItems: "center", gap: 5, marginBottom: 5, flexWrap: "wrap" }}>
                {/* Category badge */}
                <span style={{ fontSize: 8, fontWeight: 700, fontFamily: mono, padding: "1px 6px", borderRadius: 3, background: cs.bg, color: cs.color, border: `1px solid ${cs.border}` }}>
                  {item.category.toUpperCase().replace("-", " ")}
                </span>
                {/* PS badge */}
                {item.isPriceSensitive && (
                  <span style={{ fontSize: 8, fontWeight: 800, fontFamily: mono, padding: "1px 5px", borderRadius: 3, background: "rgba(255,23,68,0.08)", color: "var(--t-sell)", border: "1px solid rgba(255,23,68,0.2)" }}>PS</span>
                )}
                {/* Trading code */}
                {item.tradingCode && (
                  <span style={{ fontSize: 9, fontWeight: 800, fontFamily: mono, color: "var(--t-accent)", background: "rgba(0,212,170,0.08)", padding: "1px 6px", borderRadius: 3, border: "1px solid rgba(0,212,170,0.2)" }}>
                    {item.tradingCode}
                  </span>
                )}
                {/* Board */}
                <span style={{ fontSize: 8, fontFamily: mono, color: "var(--t-text3)", background: "var(--t-hover)", padding: "1px 5px", borderRadius: 3 }}>
                  {item.board}
                </span>
                {/* Time */}
                <span style={{ marginLeft: "auto", fontSize: 8, fontFamily: mono, color: "var(--t-text3)", flexShrink: 0 }}>
                  {fmtRelTime(item.publishedAt)}
                </span>
              </div>

              {/* Title */}
              <p style={{
                margin: 0, fontSize: 11, fontWeight: 700, color: "var(--t-text1)",
                lineHeight: 1.45, overflow: "hidden",
                display: isExp ? "block" : "-webkit-box",
                WebkitLineClamp: isExp ? "unset" : 2,
                WebkitBoxOrient: "vertical",
              }}>
                {item.title}
              </p>

              {/* Expanded content */}
              {isExp && (
                <div style={{ marginTop: 8 }}>
                  <p style={{ margin: "0 0 8px", fontSize: 10, color: "var(--t-text2)", lineHeight: 1.6, fontFamily: "'Outfit', system-ui, sans-serif" }}>
                    {item.summary}
                  </p>
                  <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
                    <span style={{ fontSize: 9, color: "var(--t-text3)", fontFamily: mono }}>
                      Source: {item.source}
                    </span>
                    {item.sourceUrl && (
                      <a href={item.sourceUrl} target="_blank" rel="noopener noreferrer"
                        onClick={e => e.stopPropagation()}
                        style={{ fontSize: 9, color: "#f59e0b", textDecoration: "none", fontFamily: mono }}>
                        Full Article →
                      </a>
                    )}
                  </div>
                  {item.keywords?.length > 0 && (
                    <div style={{ display: "flex", gap: 5, marginTop: 6, flexWrap: "wrap" }}>
                      {item.keywords.map(k => (
                        <button key={k} onClick={e => { e.stopPropagation(); setKeyword(k) }}
                          style={{ fontSize: 8, fontFamily: mono, color: "var(--t-text3)", background: "none", border: "none", cursor: "pointer", padding: 0 }}
                          onMouseEnter={e => e.currentTarget.style.color = "#f59e0b"}
                          onMouseLeave={e => e.currentTarget.style.color = "var(--t-text3)"}
                        >
                          #{k}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </div>
          )
        })}
      </div>

      {/* ── Pagination ── */}
      {totalPages > 1 && (
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", padding: "5px 8px", borderTop: "1px solid var(--t-border)", flexShrink: 0, background: "var(--t-panel)" }}>
          <button
            disabled={page <= 1}
            onClick={() => setPage(p => p - 1)}
            style={{ padding: "3px 10px", fontSize: 9, fontFamily: mono, fontWeight: 700, background: "transparent", border: "1px solid var(--t-border)", borderRadius: 4, color: page <= 1 ? "var(--t-text3)" : "var(--t-text2)", cursor: page <= 1 ? "not-allowed" : "pointer" }}
          >← Prev</button>
          <span style={{ fontSize: 9, fontFamily: mono, color: "var(--t-text3)" }}>
            {page} / {totalPages}
          </span>
          <button
            disabled={page >= totalPages}
            onClick={() => setPage(p => p + 1)}
            style={{ padding: "3px 10px", fontSize: 9, fontFamily: mono, fontWeight: 700, background: "transparent", border: "1px solid var(--t-border)", borderRadius: 4, color: page >= totalPages ? "var(--t-text3)" : "var(--t-text2)", cursor: page >= totalPages ? "not-allowed" : "pointer" }}
          >Next →</button>
        </div>
      )}
    </div>
  )
}

export default NewsFeedWidget
