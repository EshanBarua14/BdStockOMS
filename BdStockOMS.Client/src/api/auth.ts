import { apiPost } from './client'
import type { ApiResponse, LoginRequest, LoginResponse, RefreshTokenRequest } from '@/types'

export const authApi = {
  login(body: LoginRequest):   Promise<ApiResponse<LoginResponse>> { return apiPost<LoginResponse>('/auth/login', body) },
  refresh(body: RefreshTokenRequest): Promise<ApiResponse<LoginResponse>> { return apiPost<LoginResponse>('/auth/refresh', body) },
  logout(): Promise<ApiResponse<void>>         { return apiPost<void>('/auth/logout') },
  me():     Promise<ApiResponse<LoginResponse>> { return apiPost<LoginResponse>('/auth/me') },
}
