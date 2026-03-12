// @ts-nocheck
import { apiClient } from "./client"

export const authApi = {
  login:             (dto) => apiClient.post("/api/auth/login", dto).then(r => r.data),
  logout:            ()    => Promise.resolve(),
  getBrokerages:     ()    => apiClient.get("/api/auth/brokerages").then(r => r.data),
  registerInvestor:  (dto) => apiClient.post("/api/auth/register-investor", dto).then(r => r.data),
  registerBrokerage: (dto) => apiClient.post("/api/auth/register-brokerage", dto).then(r => r.data),
}

export const loginUser        = (e, p) => authApi.login({ email: e, password: p })
export const registerInvestor = (dto)  => authApi.registerInvestor(dto)
export const getBrokerages    = ()     => authApi.getBrokerages()
