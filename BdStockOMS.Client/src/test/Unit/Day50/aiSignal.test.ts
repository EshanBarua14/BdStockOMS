import { describe, it, expect } from "vitest"

function momentumSignal(stocks: any[], symbol: string) {
  const s = stocks.find((x: any) => x.tradingCode === symbol)
  if (!s) return null
  const chg     = s.changePercent ?? 0
  const vol     = s.volume ?? 0
  const avgVol  = stocks.reduce((a: number, x: any) => a + (x.volume ?? 0), 0) / (stocks.length || 1)
  const volRatio = vol / (avgVol || 1)
  const price   = s.lastTradePrice ?? 0
  const high    = s.highPrice ?? price
  const low     = s.lowPrice  ?? price
  const range   = high - low
  const pos     = range > 0 ? (price - low) / range : 0.5

  let score = 50
  score += chg * 4
  score += (volRatio - 1) * 8
  score += (pos - 0.5) * 20
  score = Math.max(5, Math.min(95, score))

  const signal = score >= 65 ? "BUY" : score <= 35 ? "SELL" : "HOLD"
  const conf   = Math.round(Math.abs(score - 50) * 2)
  return { score: Math.round(score), signal, confidence: conf }
}

const baseStocks = [
  { tradingCode: "BATBC",   changePercent: 3.5,  volume: 150000, lastTradePrice: 720, highPrice: 725, lowPrice: 710 },
  { tradingCode: "RENATA",  changePercent: -4.2, volume: 80000,  lastTradePrice: 920, highPrice: 960, lowPrice: 915 },
  { tradingCode: "NEUTRAL", changePercent: 0.1,  volume: 100000, lastTradePrice: 200, highPrice: 202, lowPrice: 199 },
]

describe("AI Momentum Signal", () => {
  it("returns null for unknown symbol", () => {
    expect(momentumSignal(baseStocks, "UNKNOWN")).toBeNull()
  })

  it("strong gainer gets BUY signal", () => {
    const result = momentumSignal(baseStocks, "BATBC")
    expect(result?.signal).toBe("BUY")
  })

  it("strong loser gets SELL signal", () => {
    const result = momentumSignal(baseStocks, "RENATA")
    expect(result?.signal).toBe("SELL")
  })

  it("score is always between 5 and 95", () => {
    baseStocks.forEach(s => {
      const r = momentumSignal(baseStocks, s.tradingCode)
      expect(r?.score).toBeGreaterThanOrEqual(5)
      expect(r?.score).toBeLessThanOrEqual(95)
    })
  })

  it("confidence is non-negative", () => {
    baseStocks.forEach(s => {
      const r = momentumSignal(baseStocks, s.tradingCode)
      expect(r?.confidence).toBeGreaterThanOrEqual(0)
    })
  })

  it("BUY signal when score >= 65", () => {
    const highChg = [{ ...baseStocks[0], changePercent: 8, volume: 500000 }, ...baseStocks.slice(1)]
    const r = momentumSignal(highChg, "BATBC")
    expect(r?.signal).toBe("BUY")
    expect(r?.score).toBeGreaterThanOrEqual(65)
  })

  it("SELL signal when score <= 35", () => {
    const lowChg = [{ ...baseStocks[1], changePercent: -9, volume: 20000 }, baseStocks[0], baseStocks[2]]
    const r = momentumSignal(lowChg, "RENATA")
    expect(r?.signal).toBe("SELL")
  })
})
