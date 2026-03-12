import { apiClient } from './client'
import type { PlaceOrderRequest, CancelOrderRequest } from '@/types'

export const ordersApi = {
  getAll:    (params?: Record<string, unknown>) =>
    apiClient.get('/orders', { params }).then(r => r.data),
  getById:   (id: string) => apiClient.get(`/orders/${id}`).then(r => r.data),
  place:     (data: PlaceOrderRequest) => apiClient.post('/orders', data).then(r => r.data),
  cancel:    (id: string, data: CancelOrderRequest) =>
    apiClient.post(`/orders/${id}/cancel`, data).then(r => r.data),
  getHistory: (params?: Record<string, unknown>) =>
    apiClient.get('/orders/history', { params }).then(r => r.data),
}
