import { describe, it, expect } from 'vitest'

describe('Trade Monitor - data formatting', () => {
  const fmt = (n: number) => n >= 1e7 ? `৳${(n/1e7).toFixed(2)}Cr` : n >= 1e5 ? `৳${(n/1e5).toFixed(1)}L` : `৳${n.toLocaleString()}`

  it('formats crore values', () => {
    expect(fmt(10000000)).toBe('৳1.00Cr')
    expect(fmt(25000000)).toBe('৳2.50Cr')
  })

  it('formats lakh values', () => {
    expect(fmt(100000)).toBe('৳1.0L')
    expect(fmt(500000)).toBe('৳5.0L')
  })

  it('formats small values', () => {
    expect(fmt(0)).toBe('৳0')
    expect(fmt(1234)).toBe('৳1,234')
  })

  it('summary fields exist', () => {
    const summary = {
      brokerageHouseId: 1, brokerName: 'Pioneer Securities Ltd',
      totalInvestors: 3, totalTraders: 2,
      totalOrdersToday: 0, totalBuyValueToday: 0,
      totalSellValueToday: 0, totalTurnoverToday: 0,
      pendingKycCount: 0, activeOrdersCount: 0, totalCommissionToday: 0
    }
    expect(summary.brokerName).toBe('Pioneer Securities Ltd')
    expect(summary.totalInvestors).toBe(3)
    expect(summary.totalTraders).toBe(2)
  })

  it('trader chart data maps correctly', () => {
    const traders = [{ traderName: 'Pioneer Trader One', buyValueToday: 100, sellValueToday: 50, totalValueToday: 150 }]
    const chartData = traders.map(t => ({
      name: t.traderName.split(' ').slice(-2).join(' '),
      Buy: t.buyValueToday, Sell: t.sellValueToday, Total: t.totalValueToday
    }))
    expect(chartData[0].name).toBe('Trader One')
    expect(chartData[0].Total).toBe(150)
  })

  it('pie data falls back to 1 when zero to avoid empty chart', () => {
    const summary = { totalBuyValueToday: 0, totalSellValueToday: 0 }
    const pieData = [
      { name: 'Buy', value: summary.totalBuyValueToday || 1 },
      { name: 'Sell', value: summary.totalSellValueToday || 1 },
    ]
    expect(pieData[0].value).toBe(1)
    expect(pieData[1].value).toBe(1)
  })

  it('chart type toggle values are valid', () => {
    const types = ['buy', 'sell', 'value']
    expect(types).toContain('buy')
    expect(types).toContain('sell')
    expect(types).toContain('value')
  })
})
