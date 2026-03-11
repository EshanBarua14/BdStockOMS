import axios, {
  AxiosInstance, AxiosRequestConfig, AxiosResponse, InternalAxiosRequestConfig,
} from 'axios'
import { useAuthStore } from '@/store/authStore'
import type { ApiResponse, RefreshTokenRequest, LoginResponse } from '@/types'

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'
const TOKEN_REFRESH_THRESHOLD_MS = 60 * 1000

export const apiClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 30_000,
})

let isRefreshing = false
let refreshQueue: Array<{ resolve: (token: string) => void; reject: (err: unknown) => void }> = []

function processRefreshQueue(token: string | null, error?: unknown) {
  refreshQueue.forEach((cb) => { if (token) cb.resolve(token); else cb.reject(error) })
  refreshQueue = []
}

async function refreshAccessToken(): Promise<string> {
  const { user, setUser, logout } = useAuthStore.getState()
  if (!user?.refreshToken) { logout(); throw new Error('No refresh token') }
  try {
    const body: RefreshTokenRequest = { refreshToken: user.refreshToken }
    const res = await axios.post<ApiResponse<LoginResponse>>(`${BASE_URL}/auth/refresh`, body)
    const data = res.data.data
    if (!data?.accessToken) throw new Error('Refresh returned no token')
    setUser({ ...user, accessToken: data.accessToken, refreshToken: data.refreshToken ?? user.refreshToken, expiresAt: Date.now() + data.expiresIn * 1000 })
    return data.accessToken
  } catch (err) { logout(); throw err }
}

apiClient.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    const { user } = useAuthStore.getState()
    if (!user?.accessToken) return config
    if (user.expiresAt - Date.now() < TOKEN_REFRESH_THRESHOLD_MS) {
      if (!isRefreshing) {
        isRefreshing = true
        try {
          const newToken = await refreshAccessToken()
          processRefreshQueue(newToken)
          config.headers.Authorization = `Bearer ${newToken}`
        } catch (err) { processRefreshQueue(null, err); return Promise.reject(err) }
        finally { isRefreshing = false }
      } else {
        const token = await new Promise<string>((resolve, reject) => { refreshQueue.push({ resolve, reject }) })
        config.headers.Authorization = `Bearer ${token}`
        return config
      }
    } else {
      config.headers.Authorization = `Bearer ${user.accessToken}`
    }
    return config
  },
  (error) => Promise.reject(error),
)

apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error) => {
    const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean }
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      if (isRefreshing) {
        try {
          const token = await new Promise<string>((resolve, reject) => { refreshQueue.push({ resolve, reject }) })
          if (originalRequest.headers) (originalRequest.headers as Record<string, string>)['Authorization'] = `Bearer ${token}`
          return apiClient(originalRequest)
        } catch (queueError) { return Promise.reject(queueError) }
      }
      isRefreshing = true
      try {
        const newToken = await refreshAccessToken()
        processRefreshQueue(newToken)
        if (originalRequest.headers) (originalRequest.headers as Record<string, string>)['Authorization'] = `Bearer ${newToken}`
        return apiClient(originalRequest)
      } catch (refreshError) { processRefreshQueue(null, refreshError); return Promise.reject(refreshError) }
      finally { isRefreshing = false }
    }
    return Promise.reject(error)
  },
)

export async function apiGet<T>(url: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const res = await apiClient.get<ApiResponse<T>>(url, config); return res.data
}
export async function apiPost<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const res = await apiClient.post<ApiResponse<T>>(url, data, config); return res.data
}
export async function apiPut<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const res = await apiClient.put<ApiResponse<T>>(url, data, config); return res.data
}
export async function apiDelete<T>(url: string, config?: AxiosRequestConfig): Promise<ApiResponse<T>> {
  const res = await apiClient.delete<ApiResponse<T>>(url, config); return res.data
}
