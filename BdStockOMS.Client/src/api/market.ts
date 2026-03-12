import { apiClient } from "./client"

export const marketApi = {
  getStocks:  () => apiClient.get("/MarketData").then(r => r.data),
  getById:    (id: number) => apiClient.get(`/MarketData/${id}`).then(r => r.data),
  getStockByCode: (stockId: number, exchange: string) =>
    apiClient.get(`/MarketData/stock/${stockId}/${exchange}`).then(r => r.data),
  searchStocks: (q: string) => apiClient.get(`/stocks/search?q=${encodeURIComponent(q)}`).then(r => r.data),
  getAllStocks:  () => apiClient.get("/stocks").then(r => r.data),
}
