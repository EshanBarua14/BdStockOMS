import { apiClient } from './client'

export const rmsApi = {
  validateOrder: (order: unknown) => apiClient.post('/rms/validate-order', order).then(r => r.data),
  getMyLimits:   ()               => apiClient.get('/rms/my-limits').then(r => r.data),
  setLimit:      (data: unknown)  => apiClient.post('/rms/set-limit', data).then(r => r.data),
}
