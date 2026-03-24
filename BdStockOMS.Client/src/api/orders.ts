// src/api/orders.ts
// Fixed Day 61 audit: added /api/ prefix to all routes
import { apiClient } from './client'
import type { PlaceOrderRequest, CancelOrderRequest } from '@/types'

export const ordersApi = {
  getAll:    (params?: Record<string, unknown>) =>
    apiClient.get('/api/orders', { params }).then(r => r.data),

  getById:   (id: string) =>
    apiClient.get(`/api/orders/${id}`).then(r => r.data),

  place:     (data: PlaceOrderRequest) =>
    apiClient.post('/api/orders', data).then(r => r.data),

  // Backend: [HttpPut("{id}/cancel")] — use PUT
  cancel:    (id: string, data: CancelOrderRequest) =>
    apiClient.put(`/api/orders/${id}/cancel`, data).then(r => r.data),

  getHistory: (params?: Record<string, unknown>) =>
    apiClient.get('/api/orders/history', { params }).then(r => r.data),

  // Get orders for a specific investor
  getByInvestor: (investorId: number) =>
    apiClient.get(`/api/orders/portfolio/${investorId}`).then(r => r.data),
}
