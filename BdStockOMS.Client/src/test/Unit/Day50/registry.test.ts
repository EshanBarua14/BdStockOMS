// @ts-nocheck
import { describe, it, expect } from "vitest"
import { WIDGET_REGISTRY_LIST, WIDGET_REGISTRY, CATEGORIES } from "@/components/widgets/registry"

describe("Widget Registry", () => {
  it("has exactly 17 widgets", () => {
    expect(WIDGET_REGISTRY_LIST).toHaveLength(17)
  })
  it("every widget has required fields", () => {
    WIDGET_REGISTRY_LIST.forEach(w => {
      expect(w.id).toBeTruthy()
      expect(w.label).toBeTruthy()
      expect(w.icon).toBeTruthy()
      expect(w.minW).toBeGreaterThan(0)
      expect(w.minH).toBeGreaterThan(0)
      expect(w.defaultW).toBeGreaterThan(0)
      expect(w.defaultH).toBeGreaterThan(0)
      expect(w.category).toBeTruthy()
      expect(w.component).toBeTruthy()
    })
  })
  it("all widget ids are unique", () => {
    const ids = WIDGET_REGISTRY_LIST.map(w => w.id)
    expect(new Set(ids).size).toBe(ids.length)
  })
  it("has all 16 expected widget ids", () => {
    const expected = ["ticker","watchlist","order","orderbook","executions","movers","heatmap","depth","pressure","portfolio","chart","notif","ai","index","news","rms"]
    expected.forEach(id => {
      expect(WIDGET_REGISTRY[id]).toBeDefined()
    })
  })
  it("CATEGORIES is derived from registry", () => {
    expect(CATEGORIES.length).toBeGreaterThan(0)
    CATEGORIES.forEach(cat => {
      expect(WIDGET_REGISTRY_LIST.some(w => w.category === cat)).toBe(true)
    })
  })
  it("defaultW is always >= minW", () => {
    WIDGET_REGISTRY_LIST.forEach(w => {
      expect(w.defaultW).toBeGreaterThanOrEqual(w.minW)
    })
  })
  it("defaultH is always >= minH (except ticker)", () => {
    WIDGET_REGISTRY_LIST.filter(w => w.id !== "ticker").forEach(w => {
      expect(w.defaultH).toBeGreaterThanOrEqual(w.minH)
    })
  })
  it("ticker widget spans full width by default", () => {
    const ticker = WIDGET_REGISTRY["ticker"]
    expect(ticker?.defaultW).toBe(24)
  })
  it("all widgets have a valid category", () => {
    const validCats = ["Market","Trading","Portfolio","System","AI","News","Risk"]
    WIDGET_REGISTRY_LIST.forEach(w => {
      expect(validCats).toContain(w.category)
    })
  })
})
