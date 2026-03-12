import { apiClient } from './client'

export const watchlistApi = {
  getAll:      ()                          => apiClient.get('/watchlists').then(r => r.data),
  getById:     (id: number)               => apiClient.get(`/watchlists/${id}`).then(r => r.data),
  create:      (name: string)             => apiClient.post('/watchlists', { name }).then(r => r.data),
  rename:      (id: number, name: string) => apiClient.put(`/watchlists/${id}/rename`, { name }).then(r => r.data),
  remove:      (id: number)               => apiClient.delete(`/watchlists/${id}`).then(r => r.data),
  addStock:    (id: number, stockId: number) => apiClient.post(`/watchlists/${id}/stocks`, { stockId }).then(r => r.data),
  removeStock: (id: number, stockId: number) => apiClient.delete(`/watchlists/${id}/stocks/${stockId}`).then(r => r.data),
}
