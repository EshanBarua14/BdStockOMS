// @ts-nocheck
import { useState, useEffect, useMemo } from "react"
import { LineChart, ComposedChart, Line, Area, Bar, XAxis, YAxis, Tooltip, ResponsiveContainer, ReferenceLine } from "recharts"
import { useMarketData } from "@/hooks/useMarketData"
import { useLinkedSymbol } from "@/hooks/useColorGroupSync"

export function PriceChartWidget({ linkedSymbol, onSymbolClick, colorGroup }: { linkedSymbol?: string; onSymbolClick?: (c: string) => void; colorGroup?: string | null }) {
  const { stocks: _s } = useMarketData()
  const stocks = _s ?? []
  const [_linked, emitSymbol] = useLinkedSymbol(colorGroup ?? null)
  const [symbol, setSymbol] = useState(linkedSymbol ?? _linked ?? "GP")
  const [chartType, setChartType] = useState("line")
  const [period, setPeriod]   = useState("1D")
  const [history, setHistory] = useState([])
  const [showTA, setShowTA] = useState<"none"|"rsi"|"macd"|"bb">("none")

  useEffect(() => { if (_linked) setSymbol(_linked); else if (linkedSymbol) setSymbol(linkedSymbol) }, [_linked, linkedSymbol])

  const stock = stocks.find(s => s.tradingCode === symbol)

  useEffect(() => {
    if (!stock) return
    const base = stock.lastTradePrice
    const pts = 60
    setHistory(Array.from({ length: pts }, (_, i) => {
      const noise = (Math.random() - 0.5) * base * 0.02
      const trend = ((i / pts) - 0.5) * base * (stock.changePercent ?? 0) * 0.02
      const p = Math.max(0.01, base + trend + noise)
      return {
        t: new Date(Date.now() - (pts - i) * 60000).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }),
        price: parseFloat(p.toFixed(2)),
        vol: Math.round(Math.random() * 5000 + 500),
      }
    }))
  }, [stock?.tradingCode])

  useEffect(() => {
    if (!stock || history.length === 0) return
    const id = setInterval(() => {
      const last = history[history.length - 1]?.price ?? stock.lastTradePrice
      const noise = (Math.random() - 0.5) * last * 0.004
      const newPt = { t: new Date().toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" }), price: parseFloat((last + noise).toFixed(2)), vol: Math.round(Math.random() * 3000 + 200) }
      setHistory(h => [...h.slice(-59), newPt])
    }, 3000)
    return () => clearInterval(id)
  }, [stock?.tradingCode, history.length])

  const up     = stock ? (stock.changePercent ?? 0) >= 0 : true
  const color  = up ? "#00D4AA" : "#FF6B6B"
  const minP   = useMemo(() => Math.min(...history.map(h => h.price)), [history])
  const maxP   = useMemo(() => Math.max(...history.map(h => h.price)), [history])
  const firstP = history[0]?.price

  // ── TA Calculations ────────────────────────────────────────
  const calcSMA = (data: number[], period: number) =>
    data.map((_, i) => i < period - 1 ? null : data.slice(i - period + 1, i + 1).reduce((a, b) => a + b, 0) / period)

  const calcRSI = (prices: number[], period = 14) => {
    if (prices.length < period + 1) return prices.map(() => null)
    return prices.map((_, i) => {
      if (i < period) return null
      const changes = prices.slice(i - period, i).map((p, j) => prices[i - period + j + 1] - p)
      const gains = changes.filter(c => c > 0).reduce((a, b) => a + b, 0) / period
      const losses = Math.abs(changes.filter(c => c < 0).reduce((a, b) => a + b, 0)) / period
      if (losses === 0) return 100
      const rs = gains / losses
      return Math.round((100 - 100 / (1 + rs)) * 100) / 100
    })
  }

  const calcMACD = (prices: number[]) => {
    const ema = (data: number[], n: number) => {
      const k = 2 / (n + 1)
      return data.reduce<number[]>((acc, p, i) => {
        if (i === 0) return [p]
        return [...acc, p * k + acc[i-1] * (1-k)]
      }, [])
    }
    if (prices.length < 26) return prices.map(() => ({ macd: null, signal: null, hist: null }))
    const ema12 = ema(prices, 12)
    const ema26 = ema(prices, 26)
    const macdLine = ema12.map((v, i) => v - ema26[i])
    const signal = ema(macdLine.slice(-9), 9)
    return prices.map((_, i) => {
      const m = macdLine[i] ?? null
      const s = i >= prices.length - 9 ? signal[i - (prices.length - 9)] : null
      return { macd: m ? Math.round(m * 100) / 100 : null, signal: s ? Math.round(s * 100) / 100 : null, hist: m && s ? Math.round((m - s) * 100) / 100 : null }
    })
  }

  const calcBB = (prices: number[], period = 20, stdMult = 2) => {
    const sma = calcSMA(prices, period)
    return prices.map((_, i) => {
      if (i < period - 1) return { upper: null, mid: null, lower: null }
      const slice = prices.slice(i - period + 1, i + 1)
      const mean = sma[i]!
      const std = Math.sqrt(slice.reduce((s, p) => s + (p - mean) ** 2, 0) / period)
      return {
        upper: Math.round((mean + stdMult * std) * 100) / 100,
        mid:   Math.round(mean * 100) / 100,
        lower: Math.round((mean - stdMult * std) * 100) / 100,
      }
    })
  }

  const prices = history.map(h => h.price)
  const rsiData  = useMemo(() => calcRSI(prices), [history])
  const macdData = useMemo(() => calcMACD(prices), [history])
  const bbData   = useMemo(() => calcBB(prices),   [history])

  const chartData = useMemo(() => history.map((h, i) => ({
    ...h,
    rsi:    rsiData[i],
    macd:   macdData[i]?.macd,
    signal: macdData[i]?.signal,
    hist:   macdData[i]?.hist,
    bbUpper: showTA === "bb" ? bbData[i]?.upper : null,
    bbMid:   showTA === "bb" ? bbData[i]?.mid   : null,
    bbLower: showTA === "bb" ? bbData[i]?.lower  : null,
  })), [history, rsiData, macdData, bbData, showTA])

  return (
    <div style={{ height: "100%", display: "flex", flexDirection: "column", background: "var(--t-surface)", overflow: "hidden" }}>
      {/* Toolbar */}
      <div style={{ padding: "5px 8px", borderBottom: "1px solid rgba(255,255,255,0.06)", display: "flex", gap: 6, alignItems: "center", flexShrink: 0 }}>
        <input value={symbol} onChange={e => setSymbol(e.target.value.toUpperCase())} placeholder="Symbol…"
          style={{ width: 80, background: "rgba(255,255,255,0.04)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 5, padding: "4px 8px", color: "#fff", fontSize: 11, outline: "none", fontFamily: "'Space Mono',monospace" }} />
        <div style={{ display: "flex", gap: 2, marginLeft: "auto" }}>
          {(["none","rsi","macd","bb"] as const).map(ta => (
            <button key={ta} onClick={() => setShowTA(ta)}
              style={{ padding: "3px 7px", fontSize: 9, fontFamily: "'Space Mono',monospace", fontWeight: 600,
                background: showTA === ta ? "rgba(34,211,238,0.12)" : "none",
                border: `1px solid ${showTA === ta ? "rgba(34,211,238,0.4)" : "rgba(255,255,255,0.06)"}`,
                borderRadius: 4, color: showTA === ta ? "#22d3ee" : "rgba(255,255,255,0.3)", cursor: "pointer" }}>
              {ta === "none" ? "OFF" : ta.toUpperCase()}
            </button>
          ))}
        </div>
        {stock && (
          <div style={{ display: "flex", gap: 6, alignItems: "center" }}>
            <span style={{ color: "#fff", fontSize: 13, fontFamily: "'Space Mono',monospace", fontWeight: 700 }}>৳{stock.lastTradePrice?.toFixed(2)}</span>
            <span style={{ color, fontSize: 11, fontFamily: "'Space Mono',monospace" }}>{up ? "+" : ""}{stock.changePercent?.toFixed(2)}%</span>
          </div>
        )}
        <div style={{ marginLeft: "auto", display: "flex", gap: 3 }}>
          {["1D","1W","1M"].map(p => (
            <button key={p} onClick={() => setPeriod(p)} style={{ padding: "3px 6px", background: period === p ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${period === p ? "rgba(255,255,255,0.15)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: period === p ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{p}</button>
          ))}
          {["line","area"].map(t => (
            <button key={t} onClick={() => setChartType(t)} style={{ padding: "3px 6px", background: chartType === t ? "rgba(255,255,255,0.08)" : "none", border: `1px solid ${chartType === t ? "rgba(255,255,255,0.15)" : "rgba(255,255,255,0.06)"}`, borderRadius: 4, color: chartType === t ? "#fff" : "rgba(255,255,255,0.3)", fontSize: 10, cursor: "pointer", fontFamily: "'Space Mono',monospace" }}>{t}</button>
          ))}
        </div>
      </div>

      {/* Chart */}
      <div style={{ flex: 1, minHeight: 0 }}>
        {history.length === 0
          ? <div style={{ height: "100%", display: "flex", alignItems: "center", justifyContent: "center", color: "rgba(255,255,255,0.2)", fontSize: 12, fontFamily: "'Space Mono',monospace" }}>Select a symbol to chart</div>
          : (
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={history} margin={{ top: 8, right: 8, left: 0, bottom: 0 }}>
                <XAxis dataKey="t" tick={{ fill: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "Space Mono" }} interval="preserveStartEnd" axisLine={false} tickLine={false} />
                <YAxis domain={[minP * 0.999, maxP * 1.001]} tick={{ fill: "rgba(255,255,255,0.2)", fontSize: 9, fontFamily: "Space Mono" }} width={50} axisLine={false} tickLine={false} tickFormatter={v => `৳${v.toFixed(1)}`} />
                <Tooltip contentStyle={{ background: "var(--t-surface)", border: `1px solid ${color}44`, borderRadius: 6, fontSize: 11, fontFamily: "Space Mono" }} labelStyle={{ color: "rgba(255,255,255,0.4)" }} itemStyle={{ color }} formatter={v => [`৳${v}`, "Price"]} />
                {firstP && <ReferenceLine y={firstP} stroke="rgba(255,255,255,0.1)" strokeDasharray="3 3" />}
                <Line type="monotone" dataKey="price" stroke={color} strokeWidth={1.5} dot={false} isAnimationActive={false} />
              </LineChart>
            </ResponsiveContainer>
          )
        }
      </div>

      {/* OHLCV strip */}
      {stock && (
        <div style={{ borderTop: "1px solid rgba(255,255,255,0.05)", padding: "4px 8px", display: "flex", gap: 12, flexShrink: 0 }}>
          {[["O", stock.closePrice], ["H", stock.highPrice], ["L", stock.lowPrice], ["V", `${((stock.volume ?? 0) / 1000).toFixed(0)}K`]].map(([l, v]) => (
            <span key={l} style={{ fontSize: 10, fontFamily: "'Space Mono',monospace" }}>
              <span style={{ color: "rgba(255,255,255,0.3)" }}>{l} </span>
              <span style={{ color: "#fff" }}>{typeof v === "number" ? `৳${v?.toFixed(2)}` : v}</span>
            </span>
          ))}
        </div>
      )}
    </div>
  )
}
