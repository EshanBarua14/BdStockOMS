// src/api/client.ts
// Fixed Day 61 audit:
// - Removed hardcoded BASE "https://localhost:7219" → use relative URLs (Vite proxy handles it)
// - searchStocks fixed: ?q= (matches backend /api/stocks/search?query=... via proxy rewrite)
// - cancelOrder fixed: uses PUT not POST (matches backend HttpPut("{id}/cancel"))

function token() {
  try {
    const raw = localStorage.getItem("bd_oms_auth_v2")
    if (raw) {
      const t = JSON.parse(raw)?.state?.user?.token
      if (t) return t
    }
  } catch {}
  return localStorage.getItem("token") ?? sessionStorage.getItem("token") ?? ""
}

function headers(): HeadersInit {
  const t = token()
  return { "Content-Type": "application/json", ...(t ? { Authorization: `Bearer ${t}` } : {}) }
}

async function handle<T>(res: Response): Promise<T> {
  if (res.status === 401) {
    try { const b = await res.clone().json(); if (b?.errorCode === "TOKEN_REVOKED" || b?.errorCode === "TOKEN_EXPIRED") { localStorage.removeItem("bd_oms_auth_v2"); window.location.href = "/login"; } } catch {}
  }
  if (!res.ok) {
    let msg = `HTTP ${res.status}`
    try { const b = await res.json(); msg = b?.message ?? b?.title ?? b?.error ?? msg } catch {}
    throw new Error(msg)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

// ─── Orders ──────────────────────────────────────────────────────────────────
export const getOrders   = () =>
  fetch(`/api/orders`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

export const placeOrder  = (dto: any) =>
  fetch(`/api/orders`, { method: "POST", headers: headers(), body: JSON.stringify(dto), cache: "no-store" }).then(r => handle(r))

// Backend: [HttpPut("{id:int}/cancel")] — use PUT not POST
export const cancelOrder = (id: number) =>
  fetch(`/api/orders/${id}/cancel`, { method: "PUT", headers: headers(), cache: "no-store" }).then(r => handle<void>(r))

// ─── Market ───────────────────────────────────────────────────────────────────
export const getMarketDepth    = (code: string) =>
  fetch(`/api/marketdepth/${code}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

export const getMarketPressure = (code: string) =>
  fetch(`/api/marketdepth/pressure/${code}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

// ─── Portfolio ────────────────────────────────────────────────────────────────
export const getPortfolioSnapshot = (uid: number) =>
  fetch(`/api/PortfolioSnapshot/latest/${uid}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

export const getPortfolioROI      = (uid: number) =>
  fetch(`/api/PortfolioSnapshot/roi/${uid}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

export const getPortfolioHistory  = (uid: number) =>
  fetch(`/api/PortfolioSnapshot/history/${uid}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

// ─── RMS ──────────────────────────────────────────────────────────────────────
export const getRmsLimits  = () =>
  fetch(`/api/rms/my-limits`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

export const validateOrder = (dto: any) =>
  fetch(`/api/rms/validate-order`, { method: "POST", headers: headers(), body: JSON.stringify(dto) }).then(r => handle(r))

// ─── Watchlist / News / Stocks ────────────────────────────────────────────────
export const getWatchlists = () =>
  fetch(`/api/watchlists`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

export const getNews = (params?: { keyword?: string; board?: string; category?: string; page?: number; pageSize?: number }) => {
  const qs = new URLSearchParams();
  if (params?.keyword)  qs.set("keyword",  params.keyword);
  if (params?.board)    qs.set("board",    params.board);
  if (params?.category) qs.set("category", params.category);
  qs.set("page",     String(params?.page     ?? 1));
  qs.set("pageSize", String(params?.pageSize ?? 20));
  return fetch(`/api/news?${qs}`, { headers: headers(), cache: "no-store" }).then(r => handle(r));
}

export const getStocks = () =>
  fetch(`/api/stocks`, { headers: headers() }).then(r => handle(r))

// Fixed: backend uses ?query= param
export const searchStocks = (q: string) =>
  fetch(`/api/stocks/search?query=${encodeURIComponent(q)}`, { headers: headers() }).then(r => handle(r))

// ─── Investors / BO Accounts (for Buy/Sell Console) ──────────────────────────
export const getMyInvestors = (traderId: number) =>
  fetch(`/api/traders/${traderId}/investors`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

export const getBOAccounts  = () =>
  fetch(`/api/ccd/bo-accounts`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

export const getStockByCode = (code: string) =>
  fetch(`/api/stocks/search?query=${encodeURIComponent(code)}`, { headers: headers() }).then(r => handle(r))

// ─── Axios-compatible shim ────────────────────────────────────────────────────
// Keeps old api/*.ts files working without changes
const _h = () => {
  const t = token()
  return { "Content-Type": "application/json", ...(t ? { Authorization: `Bearer ${t}` } : {}) }
}

async function _fetch(url: string, opts: RequestInit = {}) {
  // Ensure all URLs go through Vite proxy — strip any absolute base if present
  const relUrl = url.startsWith("http") ? url.replace(/^https?:\/\/[^/]+/, "") : url
  const res = await fetch(relUrl, { ...opts, headers: { ..._h(), ...(opts.headers ?? {}) }, cache: "no-store" })
  if (!res.ok) {
    let msg = `HTTP ${res.status}`
    try { const b = await res.json(); msg = b?.message ?? b?.title ?? msg } catch {}
    throw new Error(msg)
  }
  const data = res.status === 204 ? undefined : await res.json()
  return { data }
}

export const apiClient = {
  get: (url: string, opts?: { params?: Record<string, unknown> }) => {
    let u = url
    if (opts?.params) {
      const qs = new URLSearchParams(
        Object.entries(opts.params)
          .filter(([, v]) => v != null)
          .map(([k, v]) => [k, String(v)])
      ).toString()
      if (qs) u += (u.includes("?") ? "&" : "?") + qs
    }
    return _fetch(u)
  },
  post:   (url: string, body?: unknown, config?: { withCredentials?: boolean }) =>
    _fetch(url, { method: "POST", body: JSON.stringify(body ?? {}), credentials: config?.withCredentials ? "include" : "same-origin" }),
  put:    (url: string, body?: unknown) => _fetch(url, { method: "PUT",    body: JSON.stringify(body ?? {}) }),
  delete: (url: string)                 => _fetch(url, { method: "DELETE" }),
  patch:  (url: string, body?: unknown) => _fetch(url, { method: "PATCH",  body: JSON.stringify(body ?? {}) }),
}

// ── Broker Management ─────────────────────────────────────────
export const getBrokerages       = () => fetch("/api/broker-management/brokerages", { headers: headers() }).then(r => handle(r));
export const getBrokerage        = (id: number) => fetch(`/api/broker-management/brokerages/${id}`, { headers: headers() }).then(r => handle(r));
export const createBrokerage     = (dto: any) => fetch("/api/broker-management/brokerages", { method: "POST", headers: headers(), body: JSON.stringify(dto) }).then(r => handle(r));
export const updateBrokerage     = (id: number, dto: any) => fetch(`/api/broker-management/brokerages/${id}`, { method: "PUT", headers: headers(), body: JSON.stringify(dto) }).then(r => handle(r));
export const toggleBrokerage     = (id: number, active: boolean) => fetch(`/api/broker-management/brokerages/${id}/${active ? "activate" : "deactivate"}`, { method: "PUT", headers: headers() }).then(r => handle(r));

export const getBranches         = (brokerageHouseId?: number) => fetch(`/api/broker-management/branches${brokerageHouseId ? "?brokerageHouseId=" + brokerageHouseId : ""}`, { headers: headers() }).then(r => handle(r));
export const getBranch           = (id: number) => fetch(`/api/broker-management/branches/${id}`, { headers: headers() }).then(r => handle(r));
export const createBranch        = (dto: any) => fetch("/api/broker-management/branches", { method: "POST", headers: headers(), body: JSON.stringify(dto) }).then(r => handle(r));
export const updateBranch        = (id: number, dto: any) => fetch(`/api/broker-management/branches/${id}`, { method: "PUT", headers: headers(), body: JSON.stringify(dto) }).then(r => handle(r));
export const toggleBranch        = (id: number, active: boolean) => fetch(`/api/broker-management/branches/${id}/${active ? "activate" : "deactivate"}`, { method: "PUT", headers: headers() }).then(r => handle(r));

export const getManagedBOAccounts = (brokerageHouseId?: number) => fetch(`/api/broker-management/bo-accounts${brokerageHouseId ? "?brokerageHouseId=" + brokerageHouseId : ""}`, { headers: headers() }).then(r => handle(r));
export const updateBOAccount      = (userId: number, dto: any) => fetch(`/api/broker-management/bo-accounts/${userId}`, { method: "PUT", headers: headers(), body: JSON.stringify(dto) }).then(r => handle(r));

// ── RMS Management ────────────────────────────────────────────
export const getRMSLimitsAll     = (brokerageHouseId?: number) => fetch(`/api/rms/all${brokerageHouseId ? "?brokerageHouseId=" + brokerageHouseId : ""}`, { headers: headers() }).then(r => handle(r))
export const getRMSLimitsForInvestor = (investorId: number) => fetch(`/api/rms/investor/${investorId}`, { headers: headers() }).then(r => handle(r))
export const setRMSLimit         = (dto: any) => fetch("/api/rms/set-limit", { method: "POST", headers: headers(), body: JSON.stringify(dto) }).then(r => handle(r))
export const getMyRMSLimits      = () => fetch("/api/rms/my-limits", { headers: headers() }).then(r => handle(r))
