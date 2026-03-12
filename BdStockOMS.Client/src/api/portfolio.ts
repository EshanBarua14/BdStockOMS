import { apiClient } from './client'

export const portfolioApi = {
  getSummary:   () => apiClient.get('/portfoliosnapshot/summary').then(r => r.data),
  getHoldings:  () => apiClient.get('/portfoliosnapshot/holdings').then(r => r.data),
  getSnapshots: (params?: Record<string, unknown>) =>
    apiClient.get('/portfoliosnapshot', { params }).then(r => r.data),
}
