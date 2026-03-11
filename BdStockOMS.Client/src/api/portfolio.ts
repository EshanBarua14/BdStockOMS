import { apiClient } from './client'

export const portfolioApi = {
  getSummary:   () => apiClient.get('/PortfolioSnapshot/summary').then(r => r.data),
  getHoldings:  () => apiClient.get('/PortfolioSnapshot/holdings').then(r => r.data),
  getSnapshots: (params?: Record<string, unknown>) =>
    apiClient.get('/PortfolioSnapshot', { params }).then(r => r.data),
}
