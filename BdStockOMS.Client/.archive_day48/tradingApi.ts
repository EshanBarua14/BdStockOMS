import api from '../api/axios';
import type { Stock, OrderResponse, PlaceOrderRequest, PortfolioSummary } from '../types/trading';

// ── Stocks ────────────────────────────────────────────────────
export async function fetchStocks(): Promise<Stock[]> {
  const res = await api.get<Stock[]>('/stocks');
  return res.data;
}

export async function fetchStock(id: number): Promise<Stock> {
  const res = await api.get<Stock>(`/stocks/${id}`);
  return res.data;
}

export async function searchStocks(query: string): Promise<Stock[]> {
  const res = await api.get<Stock[]>(`/stocks/search?query=${encodeURIComponent(query)}`);
  return res.data;
}

// ── Orders ────────────────────────────────────────────────────
export async function placeOrder(dto: PlaceOrderRequest): Promise<OrderResponse> {
  const res = await api.post<OrderResponse>('/orders', dto);
  return res.data;
}

export async function fetchOrders(): Promise<OrderResponse[]> {
  const res = await api.get<OrderResponse[]>('/orders');
  return res.data;
}

export async function fetchOrderById(id: number): Promise<OrderResponse> {
  const res = await api.get<OrderResponse>(`/orders/${id}`);
  return res.data;
}

export async function cancelOrder(id: number, reason: string): Promise<OrderResponse> {
  const res = await api.put<OrderResponse>(`/orders/${id}/cancel`, { reason });
  return res.data;
}

export async function executeOrder(id: number): Promise<OrderResponse> {
  const res = await api.put<OrderResponse>(`/orders/${id}/execute`);
  return res.data;
}

// ── Portfolio ─────────────────────────────────────────────────
export async function fetchPortfolioSummary(investorId: number): Promise<PortfolioSummary> {
  const res = await api.get<PortfolioSummary>(`/portfolio/${investorId}/summary`);
  return res.data;
}
