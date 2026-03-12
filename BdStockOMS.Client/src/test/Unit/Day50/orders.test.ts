import { describe, it, expect } from "vitest"
import type { Order } from "@/types"

const makeOrder = (overrides: Partial<Order> = {}): Order => ({
  orderId: "ord-001",
  symbol: "BATBC",
  side: "Buy",
  type: "Limit",
  quantity: 100,
  filledQuantity: 0,
  price: 716,
  status: "Open",
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...overrides,
})

describe("Order utilities", () => {
  it("order value calculation is correct", () => {
    const o = makeOrder({ quantity: 100, price: 716 })
    expect((o.quantity * (o.price ?? 0))).toBe(71600)
  })

  it("filter open orders correctly", () => {
    const orders = [
      makeOrder({ status: "Open" }),
      makeOrder({ status: "Filled" }),
      makeOrder({ status: "Cancelled" }),
      makeOrder({ status: "Open" }),
    ]
    expect(orders.filter(o => o.status === "Open")).toHaveLength(2)
  })

  it("filter filled orders (executions)", () => {
    const orders = [
      makeOrder({ status: "Filled" }),
      makeOrder({ status: "Open" }),
      makeOrder({ status: "Filled" }),
    ]
    expect(orders.filter(o => o.status === "Filled")).toHaveLength(2)
  })

  it("cancellable order check", () => {
    const cancellable = (o: Order) => o.status === "Open" || o.status === "Pending"
    expect(cancellable(makeOrder({ status: "Open" }))).toBe(true)
    expect(cancellable(makeOrder({ status: "Pending" }))).toBe(true)
    expect(cancellable(makeOrder({ status: "Filled" }))).toBe(false)
    expect(cancellable(makeOrder({ status: "Cancelled" }))).toBe(false)
  })

  it("buy/sell total value calculation", () => {
    const orders = [
      makeOrder({ side: "Buy",  quantity: 100, price: 716, status: "Filled" }),
      makeOrder({ side: "Buy",  quantity: 50,  price: 958, status: "Filled" }),
      makeOrder({ side: "Sell", quantity: 80,  price: 720, status: "Filled" }),
    ]
    const bought = orders.filter(o => o.side === "Buy").reduce((a, o) => a + o.quantity * (o.price ?? 0), 0)
    const sold   = orders.filter(o => o.side === "Sell").reduce((a, o) => a + o.quantity * (o.price ?? 0), 0)
    expect(bought).toBe(71600 + 47900)
    expect(sold).toBe(57600)
  })

  it("search filter by symbol", () => {
    const orders = [
      makeOrder({ symbol: "BATBC" }),
      makeOrder({ symbol: "RENATA" }),
      makeOrder({ symbol: "BXPHARMA" }),
    ]
    const q = "BAT"
    const filtered = orders.filter(o => o.symbol.toUpperCase().includes(q.toUpperCase()))
    expect(filtered).toHaveLength(1)
    expect(filtered[0].symbol).toBe("BATBC")
  })

  it("order status badge color mapping covers all statuses", () => {
    const STATUS_COLORS = {
      Pending: "#F59E0B", Open: "#3B82F6", PartiallyFilled: "#8B5CF6",
      Filled: "#00D4AA", Cancelled: "rgba(255,255,255,0.25)",
      Rejected: "#FF6B6B", Expired: "rgba(255,255,255,0.2)",
    }
    const allStatuses: Order["status"][] = ["Pending","Open","PartiallyFilled","Filled","Cancelled","Rejected","Expired"]
    allStatuses.forEach(s => expect(STATUS_COLORS[s]).toBeDefined())
  })
})
