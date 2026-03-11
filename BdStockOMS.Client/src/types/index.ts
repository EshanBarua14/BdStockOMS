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

export interface LoginRequest {
  email: string
  password: string
}

export interface LoginResponse {
  token: string
  refreshToken: string
  expiresAt: string
  userId: number
  fullName: string
  email: string
  role: UserRole
  brokerageHouseId: number
  brokerageHouseName: string
}

export interface RegisterBrokerageRequest {
  firmName: string
  licenseNumber: string
  firmEmail: string
  firmPhone?: string
  firmAddress: string
  fullName: string
  email: string
  password: string
}

// Matches backend DB seed exactly — 'Trader' not 'Broker'
export type UserRole =
  | 'SuperAdmin'
  | 'Admin'
  | 'BrokerageHouse'
  | 'BrokerageAdmin'
  | 'Trader'
  | 'Investor'
  | 'ITSupport'
  | 'CCD'

export interface AuthUser {
  userId: number
  fullName: string
  email: string
  role: UserRole
  brokerageHouseId: number
  brokerageHouseName: string
  token: string
  expiresAt: number
}

export type OrderSide = 'Buy' | 'Sell'
export type OrderType = 'Market' | 'Limit' | 'StopLoss' | 'StopLimit'
export type OrderStatus =
  | 'Pending' | 'Open' | 'PartiallyFilled'
  | 'Filled' | 'Cancelled' | 'Rejected' | 'Expired'

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
  brokerageId?: number
  investorId?: number
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
