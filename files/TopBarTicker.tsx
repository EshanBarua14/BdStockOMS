// src/components/ui/TopBarTicker.tsx
// Bloomberg-style live scrolling price ticker for the top navigation bar
// Consumes useMarketData ticks — auto-scrolls, pauses on hover, flashes on price change

import React, { useEffect, useRef, useState, useMemo } from "react"
import type { StockTick } from "../../hooks/useMarketData"

interface TopBarTickerProps {
  ticks: StockTick[]
  isMarketOpen: boolean
  /** Which stocks to show — defaults to top 30 by volume */
  watchlist?: string[]
  speed?: number   // px per second, default 60
}

interface TickFlash {
  code: string
  dir: "up" | "down"
  ts: number
}

function fmt(n: number | undefined): string {
  if (n == null || isNaN(n)) return "—"
  return n.toFixed(2)
}

function fmtChange(n: number | undefined): string {
  if (n == null || isNaN(n)) return "—"
  const sign = n >= 0 ? "+" : ""
  return `${sign}${n.toFixed(2)}%`
}

export function TopBarTicker({
  ticks,
  isMarketOpen,
  watchlist,
  speed = 55,
}: TopBarTickerProps) {
  const trackRef = useRef<HTMLDivElement>(null)
  const animRef = useRef<number>(0)
  const posRef = useRef(0)
  const pausedRef = useRef(false)
  const prevPrices = useRef<Map<string, number>>(new Map())
  const [flashes, setFlashes] = useState<Map<string, TickFlash>>(new Map())

  // Pick displayed stocks: watchlist or top 30 by volume
  const displayed = useMemo(() => {
    let pool = ticks
    if (watchlist && watchlist.length > 0) {
      pool = ticks.filter(t => watchlist.includes(t.tradingCode))
    }
    return pool
      .sort((a, b) => (b.volume ?? 0) - (a.volume ?? 0))
      .slice(0, 40)
  }, [ticks, watchlist])

  // Detect price changes → flash
  useEffect(() => {
    const newFlashes = new Map(flashes)
    let changed = false
    displayed.forEach(t => {
      const prev = prevPrices.current.get(t.tradingCode)
      if (prev !== undefined && prev !== t.lastPrice) {
        newFlashes.set(t.tradingCode, {
          code: t.tradingCode,
          dir: t.lastPrice > prev ? "up" : "down",
          ts: Date.now(),
        })
        changed = true
        setTimeout(() => {
          setFlashes(f => {
            const next = new Map(f)
            next.delete(t.tradingCode)
            return next
          })
        }, 800)
      }
      prevPrices.current.set(t.tradingCode, t.lastPrice)
    })
    if (changed) setFlashes(newFlashes)
  }, [displayed])

  // Scroll animation
  useEffect(() => {
    if (!trackRef.current || displayed.length === 0) return
    let lastTime = performance.now()

    const animate = (now: number) => {
      const dt = now - lastTime
      lastTime = now
      if (!pausedRef.current && trackRef.current) {
        posRef.current -= (speed * dt) / 1000
        const trackWidth = trackRef.current.scrollWidth / 2  // duplicated for seamless loop
        if (Math.abs(posRef.current) >= trackWidth) {
          posRef.current = 0
        }
        trackRef.current.style.transform = `translateX(${posRef.current}px)`
      }
      animRef.current = requestAnimationFrame(animate)
    }
    animRef.current = requestAnimationFrame(animate)
    return () => cancelAnimationFrame(animRef.current)
  }, [displayed.length, speed])

  if (displayed.length === 0) return null

  // Duplicate items for seamless infinite scroll
  const items = [...displayed, ...displayed]

  return (
    <div
      className="relative flex items-center overflow-hidden flex-1 min-w-0 h-full"
      onMouseEnter={() => { pausedRef.current = true }}
      onMouseLeave={() => { pausedRef.current = false }}
    >
      {/* Left fade */}
      <div className="absolute left-0 top-0 bottom-0 w-8 z-10 pointer-events-none"
           style={{ background: "linear-gradient(to right, #09090b, transparent)" }} />

      {/* Right fade */}
      <div className="absolute right-0 top-0 bottom-0 w-8 z-10 pointer-events-none"
           style={{ background: "linear-gradient(to left, #09090b, transparent)" }} />

      {/* Scrolling track */}
      <div
        ref={trackRef}
        className="flex items-center gap-0 will-change-transform whitespace-nowrap"
        style={{ transform: "translateX(0px)" }}
      >
        {items.map((tick, i) => {
          const flash = flashes.get(tick.tradingCode)
          const isUp = (tick.changePercent ?? tick.change ?? 0) >= 0
          const flashBg = flash
            ? flash.dir === "up"
              ? "bg-emerald-500/20"
              : "bg-red-500/20"
            : ""

          return (
            <div
              key={`${tick.tradingCode}-${i}`}
              className={`
                flex items-center gap-2 px-3 h-full border-r border-zinc-800/60
                transition-colors duration-300 cursor-default
                ${flashBg}
              `}
              title={tick.stockName ?? tick.tradingCode}
            >
              {/* Ticker symbol */}
              <span className="text-[11px] font-bold text-zinc-200 tracking-wide font-mono">
                {tick.tradingCode}
              </span>

              {/* Price */}
              <span className={`text-[11px] font-mono font-semibold
                ${flash
                  ? flash.dir === "up" ? "text-emerald-300" : "text-red-300"
                  : isUp ? "text-emerald-400" : "text-red-400"
                }`}>
                {fmt(tick.lastPrice)}
              </span>

              {/* Change % */}
              <span className={`text-[10px] font-mono
                ${isUp ? "text-emerald-500" : "text-red-500"}`}>
                {isUp ? "▲" : "▼"} {fmtChange(tick.changePercent)}
              </span>
            </div>
          )
        })}
      </div>
    </div>
  )
}

// ─── Compact index display (DSE/CSE indices) ──────────────────────────────────
export interface IndexData {
  name: string
  value: number
  change: number
  changePercent: number
}

export function IndexBar({ indices }: { indices: IndexData[] }) {
  if (!indices.length) return null
  return (
    <div className="flex items-center gap-0 flex-shrink-0 border-r border-zinc-800">
      {indices.map(idx => {
        const isUp = idx.changePercent >= 0
        return (
          <div key={idx.name} className="flex items-center gap-1.5 px-3 border-r border-zinc-800/60 h-full">
            <span className="text-[10px] font-semibold text-zinc-500 uppercase tracking-wider">{idx.name}</span>
            <span className="text-[11px] font-mono font-bold text-zinc-200">{idx.value.toFixed(2)}</span>
            <span className={`text-[10px] font-mono ${isUp ? "text-emerald-400" : "text-red-400"}`}>
              {isUp ? "▲" : "▼"}{Math.abs(idx.changePercent).toFixed(2)}%
            </span>
          </div>
        )
      })}
    </div>
  )
}
