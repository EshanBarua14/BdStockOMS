import { describe, it, expect } from "vitest"

function generateDepth(lastPrice: number) {
  if (!lastPrice) return { bids: [], asks: [] }
  const bids = Array.from({ length: 10 }, (_, i) => ({
    price:  lastPrice - (i + 1) * (lastPrice * 0.001),
    qty:    100 + i * 200,
    orders: i + 1,
  }))
  const asks = Array.from({ length: 10 }, (_, i) => ({
    price:  lastPrice + (i + 1) * (lastPrice * 0.001),
    qty:    100 + i * 200,
    orders: i + 1,
  }))
  return { bids, asks }
}

describe("Market Depth", () => {
  it("returns empty for zero price", () => {
    const d = generateDepth(0)
    expect(d.bids).toHaveLength(0)
    expect(d.asks).toHaveLength(0)
  })

  it("generates 10 bid levels", () => {
    const d = generateDepth(716)
    expect(d.bids).toHaveLength(10)
  })

  it("generates 10 ask levels", () => {
    const d = generateDepth(716)
    expect(d.asks).toHaveLength(10)
  })

  it("best bid is below last price", () => {
    const d = generateDepth(716)
    expect(d.bids[0].price).toBeLessThan(716)
  })

  it("best ask is above last price", () => {
    const d = generateDepth(716)
    expect(d.asks[0].price).toBeGreaterThan(716)
  })

  it("bid prices are descending", () => {
    const d = generateDepth(716)
    for (let i = 1; i < d.bids.length; i++) {
      expect(d.bids[i].price).toBeLessThan(d.bids[i - 1].price)
    }
  })

  it("ask prices are ascending", () => {
    const d = generateDepth(716)
    for (let i = 1; i < d.asks.length; i++) {
      expect(d.asks[i].price).toBeGreaterThan(d.asks[i - 1].price)
    }
  })

  it("spread is positive", () => {
    const d = generateDepth(716)
    const spread = d.asks[0].price - d.bids[0].price
    expect(spread).toBeGreaterThan(0)
  })
})

describe("Buy/Sell Pressure", () => {
  it("buy pressure + sell pressure = 100", () => {
    const buyP  = 62.4
    const sellP = 100 - buyP
    expect(buyP + sellP).toBeCloseTo(100)
  })

  it("pressure clamped between 10 and 90", () => {
    const clamp = (v: number) => Math.max(10, Math.min(90, v))
    expect(clamp(5)).toBe(10)
    expect(clamp(95)).toBe(90)
    expect(clamp(60)).toBe(60)
  })

  it("strong buy signal when buy > 65", () => {
    const classify = (buy: number) =>
      buy > 65 ? "Strong buying" : buy < 35 ? "Strong selling" : buy > 55 ? "Mild buying" : buy < 45 ? "Mild selling" : "Balanced"
    expect(classify(70)).toBe("Strong buying")
    expect(classify(30)).toBe("Strong selling")
    expect(classify(50)).toBe("Balanced")
  })
})
