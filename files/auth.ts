// src/api/auth.ts
// Fixed Day 61 audit: logout now calls backend POST /api/auth/logout
// @ts-nocheck
import { apiClient } from "./client"

export const authApi = {
  login:             (dto) => apiClient.post("/api/auth/login", dto).then(r => r.data),

  // Fixed: actually call backend to invalidate token (Redis blacklist)
  logout:            ()    => apiClient.post("/api/auth/logout").then(r => r.data).catch(() => {}),

  getBrokerages:     ()    => apiClient.get("/api/auth/brokerages").then(r => r.data),
  registerInvestor:  (dto) => apiClient.post("/api/auth/register-investor", dto).then(r => r.data),
  registerBrokerage: (dto) => apiClient.post("/api/auth/register-brokerage", dto).then(r => r.data),
  getMe:             ()    => apiClient.get("/api/auth/me").then(r => r.data),
}

export const loginUser        = (e, p) => authApi.login({ email: e, password: p })
export const registerInvestor = (dto)  => authApi.registerInvestor(dto)
export const getBrokerages    = ()     => authApi.getBrokerages()
