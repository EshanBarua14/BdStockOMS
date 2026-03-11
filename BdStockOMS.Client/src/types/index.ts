// ─── API Response Envelope ────────────────────────────────────────────────────
export interface ApiResponse<T = unknown> {
  success: boolean
  data?: T
  message?: string
  errorCode?: string
  traceId?: string
}

export interface PaginatedResponse<T> {
  items: T[]
  totalCount: number
  pageNumber: number
  pageSize: number
  totalPages: number
}

// ─── Auth ─────────────────────────────────────────────────────────────────────
export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  accessToken: string
  refreshToken: string
  expiresIn: number
  requiresMfa: boolean
  userId: string
  email: string
  role: UserRole
  permissions: string[]
}

export interface RefreshTokenRequest {
  refreshToken: string
}

export type UserRole =
  | 'SuperAdmin'
  | 'Admin'
  | 'BrokerageAdmin'
  | 'Broker'
  | 'Investor'

// ─── User ─────────────────────────────────────────────────────────────────────
export interface AuthUser {
  userId: string
  email: string
  role: UserRole
  permissions: string[]
  accessToken: string
  refreshToken: string
  expiresAt: number   // unix ms
}

// ─── Orders ──────────────────────────────────────────────────────────────────
export type OrderSide   = 'Buy' | 'Sell'
export type OrderType   = 'Market' | 'Limit' | 'StopLoss' | 'StopLimit'
export type OrderStatus =
  | 'Pending' | 'Open' | 'PartiallyFilled'
  | 'Filled'  | 'Cancelled' | 'Rejected' | 'Expired'

export interface Order {
  orderId: string
  symbol: string
  side: OrderSide
  type: OrderType
  quantity: number
  filledQuantity: number
  price?: number
  stopPrice?: number
  status: OrderStatus
  createdAt: string
  updatedAt: string
  brokerageId?: string
  investorId?: string
}

export interface PlaceOrderRequest {
  symbol: string
  side: OrderSide
  type: OrderType
  quantity: number
  price?: number
  stopPrice?: number
}

export interface CancelOrderRequest {
  orderId: string
  reason?: string
}

// ─── Portfolio ────────────────────────────────────────────────────────────────
export interface PortfolioSummary {
  totalValue: number
  cashBalance: number
  investedAmount: number
  todayPnl: number
  todayPnlPercent: number
  totalPnl: number
  totalPnlPercent: number
}

export interface Holding {
  symbol: string
  companyName: string
  quantity: number
  avgCostPrice: number
  currentPrice: number
  currentValue: number
  unrealizedPnl: number
  unrealizedPnlPercent: number
}

// ─── Market Data ──────────────────────────────────────────────────────────────
export interface MarketTicker {
  symbol: string
  name: string
  lastPrice: number
  change: number
  changePercent: number
  volume: number
  high: number
  low: number
  open: number
  previousClose: number
}

// ─── Navigation ───────────────────────────────────────────────────────────────
export interface NavItem {
  label: string
  path: string
  icon: React.ReactNode
  roles?: UserRole[]
  badge?: number
}
