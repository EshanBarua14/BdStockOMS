import axios, {
  AxiosInstance,
  AxiosRequestConfig,
  AxiosResponse,
  InternalAxiosRequestConfig,
} from 'axios'
import { useAuthStore } from '@/store/authStore'

const BASE_URL = (import.meta as any).env?.VITE_API_BASE_URL ?? '/api'
const TOKEN_REFRESH_THRESHOLD_MS = 60 * 1000

export const apiClient: AxiosInstance = axios.create({
  baseURL: BASE_URL,
  headers: { 'Content-Type': 'application/json' },
  timeout: 30_000,
  withCredentials: true,   // ← CRITICAL: sends httpOnly refresh token cookie automatically
})

let isRefreshing = false
let refreshQueue: Array<{
  resolve: (token: string) => void
  reject: (err: unknown) => void
}> = []

function processRefreshQueue(token: string | null, error?: unknown) {
  refreshQueue.forEach((cb) => (token ? cb.resolve(token) : cb.reject(error)))
  refreshQueue = []
}

async function refreshAccessToken(): Promise<string> {
  const { user, setUser, logout } = useAuthStore.getState()
  try {
    // Backend reads refresh token from httpOnly cookie automatically
    // No need to send it in body
    const res = await axios.post(
      `${BASE_URL}/auth/refresh`,
      {},
      { withCredentials: true }
    )
    const data = res.data
    const newToken: string = data.token ?? data.Token
    if (!newToken) throw new Error('Refresh returned no token')
    if (user) {
      setUser({
        ...user,
        token: newToken,
        expiresAt: new Date(data.expiresAt ?? data.ExpiresAt).getTime(),
      })
    }
    return newToken
  } catch (err) {
    logout()
    throw err
  }
}

// Request interceptor — attach JWT, proactive refresh
apiClient.interceptors.request.use(
  async (config: InternalAxiosRequestConfig) => {
    const { user } = useAuthStore.getState()
    if (!user?.token) return config

    if (user.expiresAt - Date.now() < TOKEN_REFRESH_THRESHOLD_MS) {
      if (!isRefreshing) {
        isRefreshing = true
        try {
          const newToken = await refreshAccessToken()
          processRefreshQueue(newToken)
          config.headers.Authorization = `Bearer ${newToken}`
        } catch (err) {
          processRefreshQueue(null, err)
          return Promise.reject(err)
        } finally {
          isRefreshing = false
        }
      } else {
        const token = await new Promise<string>((resolve, reject) => {
          refreshQueue.push({ resolve, reject })
        })
        config.headers.Authorization = `Bearer ${token}`
        return config
      }
    } else {
      config.headers.Authorization = `Bearer ${user.token}`
    }
    return config
  },
  (error) => Promise.reject(error)
)

// Response interceptor — reactive 401 refresh
apiClient.interceptors.response.use(
  (response: AxiosResponse) => response,
  async (error) => {
    const originalRequest = error.config as AxiosRequestConfig & {
      _retry?: boolean
    }
    if (error.response?.status === 401 && !originalRequest._retry) {
      originalRequest._retry = true
      if (isRefreshing) {
        try {
          const token = await new Promise<string>((resolve, reject) => {
            refreshQueue.push({ resolve, reject })
          })
          if (originalRequest.headers)
            (originalRequest.headers as Record<string, string>)[
              'Authorization'
            ] = `Bearer ${token}`
          return apiClient(originalRequest)
        } catch (e) {
          return Promise.reject(e)
        }
      }
      isRefreshing = true
      try {
        const newToken = await refreshAccessToken()
        processRefreshQueue(newToken)
        if (originalRequest.headers)
          (originalRequest.headers as Record<string, string>)[
            'Authorization'
          ] = `Bearer ${newToken}`
        return apiClient(originalRequest)
      } catch (refreshError) {
        processRefreshQueue(null, refreshError)
        return Promise.reject(refreshError)
      } finally {
        isRefreshing = false
      }
    }
    return Promise.reject(error)
  }
)

export async function apiGet<T>(
  url: string,
  config?: AxiosRequestConfig
): Promise<T> {
  const res = await apiClient.get<T>(url, config)
  return res.data
}
export async function apiPost<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): Promise<T> {
  const res = await apiClient.post<T>(url, data, config)
  return res.data
}
export async function apiPut<T>(
  url: string,
  data?: unknown,
  config?: AxiosRequestConfig
): Promise<T> {
  const res = await apiClient.put<T>(url, data, config)
  return res.data
}
export async function apiDelete<T>(
  url: string,
  config?: AxiosRequestConfig
): Promise<T> {
  const res = await apiClient.delete<T>(url, config)
  return res.data
}
