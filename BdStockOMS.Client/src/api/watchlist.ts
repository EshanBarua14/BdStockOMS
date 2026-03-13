import { apiClient } from './client'

export const watchlistApi = {
  getAll:      ()                              => apiClient.get('/api/watchlists').then(r => r.data),
  getById:     (id: number)                   => apiClient.get(`/api/watchlists/${id}`).then(r => r.data),
  create:      (name: string)                 => apiClient.post('/api/watchlists', { name }).then(r => r.data),
  rename:      (id: number, name: string)     => apiClient.put(`/api/watchlists/${id}/rename`, { name }).then(r => r.data),
  remove:      (id: number)                   => apiClient.delete(`/api/watchlists/${id}`).then(r => r.data),
  addStock:    (id: number, stockId: number)  => apiClient.post(`/api/watchlists/${id}/stocks`, { stockId }).then(r => r.data),
  removeStock: (id: number, stockId: number)  => apiClient.delete(`/api/watchlists/${id}/stocks/${stockId}`).then(r => r.data),
  reorder:     (id: number, items: { stockId: number; sortOrder: number }[]) =>
                 apiClient.put(`/api/watchlists/${id}/reorder`, items).then(r => r.data),
}
