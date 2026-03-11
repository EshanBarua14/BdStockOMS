import { apiClient } from './client'
import type { PlaceOrderRequest, CancelOrderRequest } from '@/types'

export const ordersApi = {
  getAll:    (params?: Record<string, unknown>) =>
    apiClient.get('/Order', { params }).then(r => r.data),
  getById:   (id: string) => apiClient.get(`/Order/${id}`).then(r => r.data),
  place:     (data: PlaceOrderRequest) => apiClient.post('/Order', data).then(r => r.data),
  cancel:    (id: string, data: CancelOrderRequest) =>
    apiClient.post(`/Order/${id}/cancel`, data).then(r => r.data),
  getHistory: (params?: Record<string, unknown>) =>
    apiClient.get('/Order/history', { params }).then(r => r.data),
}
