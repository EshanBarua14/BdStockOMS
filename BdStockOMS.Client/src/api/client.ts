// src/api/client.ts
// All endpoints — cache: no-store, Bearer auth, clean error handling

const BASE = ""

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
  if (!res.ok) {
    let msg = `HTTP ${res.status}`
    try { const b = await res.json(); msg = b?.message ?? b?.title ?? b?.error ?? msg } catch {}
    throw new Error(msg)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

// Orders
export const getOrders     = () => fetch(`${BASE}/api/orders`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const placeOrder    = (dto: any) => fetch(`${BASE}/api/orders`, { method: "POST", headers: headers(), body: JSON.stringify(dto), cache: "no-store" }).then(r => handle(r))
export const cancelOrder   = (id: number) => fetch(`${BASE}/api/orders/${id}/cancel`, { method: "POST", headers: headers(), cache: "no-store" }).then(r => handle<void>(r))

// Market
export const getMarketDepth    = (code: string) => fetch(`${BASE}/api/marketdepth/${code}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const getMarketPressure = (code: string) => fetch(`${BASE}/api/marketdepth/pressure/${code}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

// Portfolio
export const getPortfolioSnapshot = (uid: number) => fetch(`${BASE}/api/portfoliosnapshot/latest/${uid}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const getPortfolioROI      = (uid: number) => fetch(`${BASE}/api/portfoliosnapshot/roi/${uid}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const getPortfolioHistory  = (uid: number) => fetch(`${BASE}/api/portfoliosnapshot/history/${uid}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))

// RMS
export const getRmsLimits    = () => fetch(`${BASE}/api/rms/my-limits`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const validateOrder   = (dto: any) => fetch(`${BASE}/api/rms/validate-order`, { method: "POST", headers: headers(), body: JSON.stringify(dto) }).then(r => handle(r))

// Watchlist / News / Stocks
export const getWatchlists = () => fetch(`${BASE}/api/watchlists`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const getNews       = (count = 20) => fetch(`${BASE}/api/news?count=${count}`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const getStocks     = () => fetch(`${BASE}/api/stocks`, { headers: headers() }).then(r => handle(r))
export const searchStocks  = (q: string) => fetch(`${BASE}/api/stocks/search?q=${encodeURIComponent(q)}`, { headers: headers() }).then(r => handle(r))


// ─── Investors / BO Accounts (for Buy/Sell Console) ──────────────────────────
export const getMyInvestors   = (traderId: number) => fetch(`${BASE}/api/traders/${traderId}/investors`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const getBOAccounts    = () => fetch(`${BASE}/api/ccd/bo-accounts`, { headers: headers(), cache: "no-store" }).then(r => handle(r))
export const getStockByCode   = (code: string) => fetch(`${BASE}/api/stocks/search?q=${encodeURIComponent(code)}`, { headers: headers() }).then(r => handle(r))

// ─── Axios-compatible shim ────────────────────────────────────────────────────
// Keeps old api/*.ts files (admin, auth, market, orders, portfolio, rms, watchlist)
// working without any changes to those files.
const _h = () => {
  const t = token()
  return { "Content-Type": "application/json", ...(t ? { Authorization: `Bearer ${t}` } : {}) }
}
async function _fetch(url: string, opts: RequestInit = {}) {
  const res = await fetch(url, { ...opts, headers: { ..._h(), ...(opts.headers ?? {}) }, cache: "no-store" })
  if (!res.ok) {
    let msg = `HTTP ${res.status}`
    try { const b = await res.json(); msg = b?.message ?? b?.title ?? msg } catch {}
    throw new Error(msg)
  }
  const data = res.status === 204 ? undefined : await res.json()
  return { data }
}
export const apiClient = {
  get:    (url: string, opts?: { params?: Record<string, unknown> }) => {
    let u = url
    if (opts?.params) {
      const qs = new URLSearchParams(
        Object.entries(opts.params)
          .filter(([,v]) => v != null)
          .map(([k,v]) => [k, String(v)])
      ).toString()
      if (qs) u += (u.includes("?") ? "&" : "?") + qs
    }
    return _fetch(u)
  },
  post:   (url: string, body?: unknown, config?: { withCredentials?: boolean }) => _fetch(url, { method: "POST", body: JSON.stringify(body ?? {}), credentials: config?.withCredentials ? 'include' : 'same-origin' }),
  put:    (url: string, body?: unknown) => _fetch(url, { method: "PUT",  body: JSON.stringify(body ?? {}) }),
  delete: (url: string)                 => _fetch(url, { method: "DELETE" }),
  patch:  (url: string, body?: unknown) => _fetch(url, { method: "PATCH", body: JSON.stringify(body ?? {}) }),
}
