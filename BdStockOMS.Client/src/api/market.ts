import { apiClient } from './client'

export const marketApi = {
  getStocks:    () => apiClient.get('/MarketData/stocks').then(r => r.data),
  getStock:     (symbol: string) => apiClient.get(`/MarketData/stocks/${symbol}`).then(r => r.data),
  getIndices:   () => apiClient.get('/MarketData/indices').then(r => r.data),
  getDepth:     (symbol: string) => apiClient.get(`/MarketData/depth/${symbol}`).then(r => r.data),
}
