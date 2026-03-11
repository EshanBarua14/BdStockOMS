import { apiClient } from './client'
import type { LoginRequest, LoginResponse, RegisterBrokerageRequest } from '@/types'

export const authApi = {
  async login(body: LoginRequest): Promise<LoginResponse> {
    const res = await apiClient.post<LoginResponse>('/auth/login', body, {
      withCredentials: true,
    })
    return res.data
  },

  async registerBrokerage(body: RegisterBrokerageRequest): Promise<LoginResponse> {
    const res = await apiClient.post<LoginResponse>('/auth/register-brokerage', body, {
      withCredentials: true,
    })
    return res.data
  },

  async refresh(): Promise<LoginResponse> {
    const res = await apiClient.post<LoginResponse>(
      '/auth/refresh', {}, { withCredentials: true }
    )
    return res.data
  },

  async getBrokerages(): Promise<{ id: number; name: string; email: string; phone?: string }[]> {
    const res = await apiClient.get('/auth/brokerages')
    return res.data
  },

  async registerInvestor(body: {
    fullName: string; email: string; phone: string
    password: string; brokerageHouseId: number; boNumber?: string
  }): Promise<import('@/types').LoginResponse> {
    const res = await apiClient.post('/auth/register-investor', body, { withCredentials: true })
    return res.data
  },

  async logout(): Promise<void> {
    await apiClient.post('/auth/logout', {}, { withCredentials: true })
  },
}
