// Trading types — matches backend DTOs exactly

export type OrderType = 'Buy' | 'Sell';
export type OrderCategory = 'Market' | 'Limit';
export type OrderStatus = 'Pending' | 'Executed' | 'Completed' | 'Cancelled' | 'Rejected';
export type SettlementType = 'T2' | 'T0';
export type StockCategory = 'A' | 'B' | 'G' | 'N' | 'Z' | 'Spot';

export interface Stock {
  id: number;
  tradingCode: string;
  companyName: string;
  exchange: string;
  category: StockCategory;
  lastTradePrice: number;
  highPrice: number;
  lowPrice: number;
  closePrice: number;
  change: number;
  changePercent: number;
  volume: number;
  circuitBreakerHigh: number;
  circuitBreakerLow: number;
  boardLotSize: number;
  isActive: boolean;
  lastUpdatedAt: string;
}

export interface PlaceOrderRequest {
  stockId: number;
  orderType: OrderType;
  orderCategory: OrderCategory;
  quantity: number;
  limitPrice?: number;
  investorId?: number;
  notes?: string;
}

export interface OrderResponse {
  id: number;
  investorId: number;
  investorName: string;
  traderId?: number;
  traderName?: string;
  stockId: number;
  tradingCode: string;
  companyName: string;
  exchange: string;
  orderType: OrderType;
  orderCategory: OrderCategory;
  quantity: number;
  priceAtOrder: number;
  limitPrice?: number;
  executionPrice?: number;
  settlementType: SettlementType;
  placedBy: string;
  status: OrderStatus;
  rejectionReason?: string;
  notes?: string;
  totalValue: number;
  createdAt: string;
  executedAt?: string;
  completedAt?: string;
  cancelledAt?: string;
}

export interface PortfolioHolding {
  portfolioId: number;
  stockId: number;
  tradingCode: string;
  companyName: string;
  exchange: string;
  category: string;
  quantity: number;
  averageBuyPrice: number;
  currentPrice: number;
  costBasis: number;
  currentValue: number;
  unrealizedPnL: number;
  pnLPercent: number;
  lastUpdatedAt: string;
}

export interface PortfolioSummary {
  investorId: number;
  investorName: string;
  brokerageHouseName: string;
  cashBalance: number;
  totalCostBasis: number;
  totalCurrentValue: number;
  totalUnrealizedPnL: number;
  totalPnLPercent: number;
  totalPortfolioValue: number;
  holdings: PortfolioHolding[];
}

export interface PriceUpdate {
  stockId: number;
  tradingCode: string;
  companyName: string;
  exchange: string;
  lastTradePrice: number;
  change: number;
  changePercent: number;
  updatedAt: string;
}
