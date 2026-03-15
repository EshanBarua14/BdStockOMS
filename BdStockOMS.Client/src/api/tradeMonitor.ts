import { apiClient } from "./client"

export const getBrokerSummary = (brokerageHouseId: number) =>
  apiClient.get(`/BrokerSummary/${brokerageHouseId}`).then(r => r.data)

export const getTopTraders = (brokerageHouseId: number, type: 'buy' | 'sell' | 'value') =>
  apiClient.get(`/BrokerSummary/${brokerageHouseId}/top-traders/${type}`).then(r => r.data)

export const getTopClients = (brokerageHouseId: number) =>
  apiClient.get(`/BrokerSummary/${brokerageHouseId}/top-clients`).then(r => r.data)
