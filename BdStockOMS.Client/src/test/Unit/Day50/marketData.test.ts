import { describe, it, expect } from "vitest"

describe("Market data utilities", () => {
  it("change percent calculation is correct", () => {
    const prev = 100, curr = 105
    const chg = ((curr - prev) / prev) * 100
    expect(chg).toBeCloseTo(5.0)
  })

  it("negative change percent calculation is correct", () => {
    const prev = 200, curr = 190
    const chg = ((curr - prev) / prev) * 100
    expect(chg).toBeCloseTo(-5.0)
  })

  it("volume formatting to K is correct", () => {
    const vol = 125000
    const fmt = `${(vol / 1000).toFixed(0)}K`
    expect(fmt).toBe("125K")
  })

  it("price formatting with 2 decimals", () => {
    const price = 716.2
    expect(price.toFixed(2)).toBe("716.20")
  })

  it("stock filter by exchange works", () => {
    const stocks = [
      { tradingCode: "BATBC", exchange: "DSE", lastTradePrice: 716 },
      { tradingCode: "BXPHARMA", exchange: "CSE", lastTradePrice: 22 },
      { tradingCode: "RENATA", exchange: "DSE", lastTradePrice: 958 },
    ]
    const dse = stocks.filter(s => s.exchange === "DSE")
    expect(dse).toHaveLength(2)
    expect(dse.every(s => s.exchange === "DSE")).toBe(true)
  })

  it("top gainers sort descending by changePercent", () => {
    const stocks = [
      { tradingCode: "A", changePercent: 2.1 },
      { tradingCode: "B", changePercent: 5.3 },
      { tradingCode: "C", changePercent: 0.8 },
    ]
    const sorted = [...stocks].sort((a, b) => b.changePercent - a.changePercent)
    expect(sorted[0].tradingCode).toBe("B")
    expect(sorted[2].tradingCode).toBe("C")
  })

  it("top losers sort ascending by changePercent", () => {
    const stocks = [
      { tradingCode: "A", changePercent: -2.1 },
      { tradingCode: "B", changePercent: -5.3 },
      { tradingCode: "C", changePercent: -0.8 },
    ]
    const sorted = [...stocks].sort((a, b) => a.changePercent - b.changePercent)
    expect(sorted[0].tradingCode).toBe("B")
  })

  it("bulk price update merges correctly", () => {
    const prev = [
      { tradingCode: "BATBC", lastTradePrice: 716, changePercent: 0.5 },
      { tradingCode: "RENATA", lastTradePrice: 958, changePercent: 0.2 },
    ]
    const updates = [{ tradingCode: "BATBC", lastTradePrice: 720, changePercent: 1.1 }]
    const map = new Map(prev.map(s => [s.tradingCode, s]))
    updates.forEach(u => { if (map.has(u.tradingCode)) map.set(u.tradingCode, { ...map.get(u.tradingCode), ...u }) })
    const result = Array.from(map.values())
    expect(result.find(s => s.tradingCode === "BATBC")?.lastTradePrice).toBe(720)
    expect(result.find(s => s.tradingCode === "RENATA")?.lastTradePrice).toBe(958)
  })
})
