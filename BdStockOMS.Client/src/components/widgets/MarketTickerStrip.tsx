// @ts-nocheck
import { useEffect, useRef } from "react"
import { useSetting } from '@/store/useSettingsStore'
import { useMarketData } from "@/hooks/useMarketData"

export function MarketTickerStrip() {
  const { stocks: _stocks, connected } = useMarketData()
  const stocks = _stocks ?? []
  const ref = useRef<HTMLDivElement>(null)

  useEffect(() => {
    const el = ref.current
    if (!el) return
    let pos = 0
    const tickerSpeed = useSetting('tickerSpeed')
    const speed = tickerSpeed === 'slow' ? 0.25 : tickerSpeed === 'fast' ? 1.2 : 0.5
    const tick = () => {
      pos -= speed
      if (pos < -(el.scrollWidth / 2)) pos = 0
      el.style.transform = `translateX(${pos}px)`
      requestAnimationFrame(tick)
    }
    const raf = requestAnimationFrame(tick)
    return () => cancelAnimationFrame(raf)
  }, [stocks.length])

  const items = [...stocks, ...stocks]

  return (
    <div style={{ overflow: "hidden", background: "#0A0F1C", borderBottom: "1px solid rgba(255,255,255,0.06)", height: "100%", display: "flex", alignItems: "center" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 2, padding: "0 8px", flexShrink: 0 }}>
        <span style={{ width: 6, height: 6, borderRadius: "50%", background: connected ? "#00D4AA" : "#FF6B6B", display: "inline-block", boxShadow: connected ? "0 0 6px #00D4AA" : "none" }} />
        <span style={{ color: "rgba(255,255,255,0.3)", fontSize: 9, fontFamily: "'Space Mono',monospace", letterSpacing: "0.1em" }}>LIVE</span>
      </div>
      <div style={{ flex: 1, overflow: "hidden" }}>
        <div ref={ref} style={{ display: "flex", gap: 24, whiteSpace: "nowrap", willChange: "transform" }}>
          {items.map((s, i) => (
            <span key={i} style={{ display: "inline-flex", gap: 6, alignItems: "center", fontSize: 11, fontFamily: "'Space Mono',monospace" }}>
              <span style={{ color: "rgba(255,255,255,0.7)", fontWeight: 700 }}>{s.tradingCode}</span>
              <span style={{ color: "#fff" }}>৳{(s.lastTradePrice ?? 0).toFixed(2)}</span>
              <span style={{ color: (s.change ?? 0) >= 0 ? "#00D4AA" : "#FF6B6B" }}>
                {(s.change ?? 0) >= 0 ? "▲" : "▼"}{Math.abs(s.changePercent ?? 0).toFixed(2)}%
              </span>
            </span>
          ))}
        </div>
      </div>
    </div>
  )
}
