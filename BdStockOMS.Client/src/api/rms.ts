import { apiClient } from './client'

export const rmsApi = {
  validateOrder: (order: unknown) => apiClient.post('/RMS/validate-order', order).then(r => r.data),
  getMyLimits:   ()               => apiClient.get('/RMS/my-limits').then(r => r.data),
  setLimit:      (data: unknown)  => apiClient.post('/RMS/set-limit', data).then(r => r.data),
}
