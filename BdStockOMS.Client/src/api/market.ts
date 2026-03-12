import { apiClient } from "./client"

export const marketApi = {
  getStocks:  () => apiClient.get("/marketdata").then(r => r.data),
  getById:    (id: number) => apiClient.get(`/marketdata/${id}`).then(r => r.data),
  getStockByCode: (stockId: number, exchange: string) =>
    apiClient.get(`/marketdata/stock/${stockId}/${exchange}`).then(r => r.data),
  searchStocks: (q: string) => apiClient.get(`/stocks/search?q=${encodeURIComponent(q)}`).then(r => r.data),
  getAllStocks:  () => apiClient.get("/stocks").then(r => r.data),
}

// Named export used by useMarketData hook
export const getMarketData = () => apiClient.get("/api/marketdata").then((r: any) => r.data)
