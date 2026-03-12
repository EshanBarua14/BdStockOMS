/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — API Service
   REST API calls with auth token management
   ═══════════════════════════════════════════════════════════════ */

const BASE_URL = '/api';

class ApiService {
  private token: string | null = null;

  setToken(token: string) {
    this.token = token;
    localStorage.setItem('oms_token', token);
  }

  getToken(): string | null {
    if (!this.token) {
      this.token = localStorage.getItem('oms_token');
    }
    return this.token;
  }

  clearToken() {
    this.token = null;
    localStorage.removeItem('oms_token');
  }

  private async request<T>(
    endpoint: string,
    options: RequestInit = {}
  ): Promise<T> {
    const token = this.getToken();
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      ...(options.headers as Record<string, string>),
    };

    if (token) {
      headers['Authorization'] = `Bearer ${token}`;
    }

    const response = await fetch(`${BASE_URL}${endpoint}`, {
      ...options,
      headers,
    });

    if (response.status === 401) {
      this.clearToken();
      window.location.href = '/login';
      throw new Error('Unauthorized');
    }

    if (!response.ok) {
      const errorBody = await response.text().catch(() => '');
      throw new Error(`API Error ${response.status}: ${errorBody}`);
    }

    // Handle 204 No Content
    if (response.status === 204) {
      return {} as T;
    }

    return response.json();
  }

  get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'GET' });
  }

  post<T>(endpoint: string, body?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'POST',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  put<T>(endpoint: string, body?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: 'PUT',
      body: body ? JSON.stringify(body) : undefined,
    });
  }

  delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: 'DELETE' });
  }

  // ── Auth ─────────────────────────────────────────────────
  async login(email: string, password: string): Promise<{ token: string; user: any }> {
    const result = await this.post<{ token: string; user: any }>('/auth/login', {
      email,
      password,
    });
    this.setToken(result.token);
    return result;
  }

  // ── Market Data ──────────────────────────────────────────
  async getStockPrices(exchange?: string): Promise<any[]> {
    const query = exchange ? `?exchange=${exchange}` : '';
    return this.get(`/marketdata/prices${query}`);
  }

  async getIndexes(exchange?: string): Promise<any[]> {
    const query = exchange ? `?exchange=${exchange}` : '';
    return this.get(`/marketdata/indexes${query}`);
  }

  async getMarketStatus(): Promise<any> {
    return this.get('/marketdata/status');
  }

  async getTopMovers(exchange?: string, limit = 10): Promise<any> {
    const query = exchange ? `?exchange=${exchange}&limit=${limit}` : `?limit=${limit}`;
    return this.get(`/marketdata/movers${query}`);
  }

  async getSectorHeatmap(exchange?: string): Promise<any[]> {
    const query = exchange ? `?exchange=${exchange}` : '';
    return this.get(`/marketdata/heatmap${query}`);
  }

  // ── Orders ───────────────────────────────────────────────
  async placeOrder(order: any): Promise<any> {
    return this.post('/orders', order);
  }

  async getOrders(params?: Record<string, string>): Promise<any[]> {
    const query = params ? '?' + new URLSearchParams(params).toString() : '';
    return this.get(`/orders${query}`);
  }

  async cancelOrder(orderId: number): Promise<any> {
    return this.put(`/orders/${orderId}/cancel`);
  }

  // ── Portfolio ────────────────────────────────────────────
  async getPortfolio(): Promise<any> {
    return this.get('/portfolio');
  }

  async getPositions(): Promise<any[]> {
    return this.get('/portfolio/positions');
  }

  // ── Executions ───────────────────────────────────────────
  async getExecutions(params?: Record<string, string>): Promise<any[]> {
    const query = params ? '?' + new URLSearchParams(params).toString() : '';
    return this.get(`/executions${query}`);
  }

  // ── Watchlist ────────────────────────────────────────────
  async getWatchlists(): Promise<any[]> {
    return this.get('/watchlists');
  }

  async createWatchlist(name: string, symbols: string[]): Promise<any> {
    return this.post('/watchlists', { name, symbols });
  }

  async addToWatchlist(watchlistId: number, symbol: string): Promise<any> {
    return this.post(`/watchlists/${watchlistId}/symbols`, { symbol });
  }

  // ── News ─────────────────────────────────────────────────
  async getNews(params?: Record<string, string>): Promise<any[]> {
    const query = params ? '?' + new URLSearchParams(params).toString() : '';
    return this.get(`/news${query}`);
  }

  // ── Notifications ────────────────────────────────────────
  async getNotifications(): Promise<any[]> {
    return this.get('/notifications');
  }

  async markNotificationRead(id: number): Promise<void> {
    return this.put(`/notifications/${id}/read`);
  }

  // ── Risk / Limits ────────────────────────────────────────
  async getRiskMetrics(): Promise<any> {
    return this.get('/risk/metrics');
  }

  async getLimitRequests(): Promise<any[]> {
    return this.get('/limits');
  }

  async submitLimitRequest(request: any): Promise<any> {
    return this.post('/limits', request);
  }

  // ── Search ───────────────────────────────────────────────
  async searchSymbols(query: string): Promise<any[]> {
    return this.get(`/search/symbols?q=${encodeURIComponent(query)}`);
  }
}

export const apiService = new ApiService();
