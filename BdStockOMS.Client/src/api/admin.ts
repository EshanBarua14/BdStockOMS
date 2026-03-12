// @ts-nocheck
import { apiClient } from './client'

// ── Tenant Provisioning (SuperAdmin only) ────────────────────────────────
export const tenantApi = {
  getAll:       ()                   => apiClient.get('/TenantProvisioning/all').then(r => r.data),
  provision:    (brokerageHouseId: number) =>
    apiClient.post('/TenantProvisioning/provision', { brokerageHouseId }).then(r => r.data),
  activate:     (id: number) => apiClient.post(`/TenantProvisioning/${id}/activate`).then(r => r.data),
  deactivate:   (id: number) => apiClient.post(`/TenantProvisioning/${id}/deactivate`).then(r => r.data),
  migrate:      (id: number) => apiClient.post(`/TenantProvisioning/${id}/migrate`).then(r => r.data),
  health:       (id: number) => apiClient.get(`/TenantProvisioning/${id}/health`).then(r => r.data),
}

// ── Admin Dashboard (SuperAdmin + Admin) ─────────────────────────────────
export const adminApi = {
  getDashboard:      () => apiClient.get('/AdminDashboard').then(r => r.data),
  getUserStats:      () => apiClient.get('/AdminDashboard/users').then(r => r.data),
  getOrderStats:     () => apiClient.get('/AdminDashboard/orders').then(r => r.data),
  getFundStats:      () => apiClient.get('/AdminDashboard/fund-requests').then(r => r.data),
  getSystemStats:    () => apiClient.get('/AdminDashboard/system').then(r => r.data),
  getRecentActivity: (count = 10) => apiClient.get(`/AdminDashboard/activity?count=${count}`).then(r => r.data),
}

// ── Brokerage Houses ─────────────────────────────────────────────────────
export const brokerageApi = {
  getAll:    ()       => apiClient.get('/BrokerageSettings').then(r => r.data),
  getById:   (id: number) => apiClient.get(`/BrokerageSettings/${id}`).then(r => r.data),
  update:    (id: number, data: unknown) => apiClient.put(`/BrokerageSettings/${id}`, data).then(r => r.data),
}
