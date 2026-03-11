import { apiGet, apiPost } from './client'
import type {
  ApiResponse,
  Order,
  PlaceOrderRequest,
  CancelOrderRequest,
  PaginatedResponse,
} from '@/types'

export const ordersApi = {
  list(params?: {
    page?: number
    pageSize?: number
    status?: string
    symbol?: string
  }): Promise<ApiResponse<PaginatedResponse<Order>>> {
    return apiGet<PaginatedResponse<Order>>('/orders', { params })
  },

  getById(orderId: string): Promise<ApiResponse<Order>> {
    return apiGet<Order>(`/orders/${orderId}`)
  },

  place(body: PlaceOrderRequest): Promise<ApiResponse<Order>> {
    return apiPost<Order>('/orders', body)
  },

  cancel(body: CancelOrderRequest): Promise<ApiResponse<void>> {
    return apiPost<void>(`/orders/${body.orderId}/cancel`, {
      reason: body.reason,
    })
  },
}
