/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — Market Data Store (React Context)
   Central data provider for all widgets — real-time via SignalR
   ═══════════════════════════════════════════════════════════════ */

import React, { createContext, useContext, useEffect, useReducer, useCallback, useRef } from 'react';
import { signalRService, StockPriceData } from '../services/signalRService';
import { apiService } from '../services/apiService';
import * as signalR from '@microsoft/signalr';

// ── Types ────────────────────────────────────────────────────
export type Exchange = 'DSE' | 'CSE' | 'ALL';

export interface StockMap {
  [symbol: string]: StockPriceData & {
    prevPrice?: number;
    priceDirection?: 'up' | 'down' | 'unchanged';
  };
}

export interface IndexData {
  name: string;
  value: number;
  change: number;
  changePercent: number;
  volume: number;
  exchange: string;
}

export interface MarketStatus {
  exchange: string;
  status: 'OPEN' | 'CLOSED' | 'PRE_OPEN' | 'POST_CLOSE';
  phase: string;
  sessionTime: string;
  turnover: number;
  totalTrades: number;
}

export interface MarketDataState {
  stocks: StockMap;
  indexes: IndexData[];
  marketStatus: MarketStatus[];
  selectedExchange: Exchange;
  connectionState: signalR.HubConnectionState;
  isLoading: boolean;
  lastUpdate: string | null;
  error: string | null;
  selectedSymbol: string | null;
}

type MarketDataAction =
  | { type: 'SET_STOCKS'; payload: StockPriceData[] }
  | { type: 'UPDATE_STOCKS'; payload: StockPriceData[] }
  | { type: 'SET_INDEXES'; payload: IndexData[] }
  | { type: 'SET_MARKET_STATUS'; payload: MarketStatus[] }
  | { type: 'SET_EXCHANGE'; payload: Exchange }
  | { type: 'SET_CONNECTION_STATE'; payload: signalR.HubConnectionState }
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SELECT_SYMBOL'; payload: string | null };

const initialState: MarketDataState = {
  stocks: {},
  indexes: [],
  marketStatus: [],
  selectedExchange: 'DSE',
  connectionState: signalR.HubConnectionState.Disconnected,
  isLoading: true,
  lastUpdate: null,
  error: null,
  selectedSymbol: null,
};

// ── Reducer ──────────────────────────────────────────────────
function marketDataReducer(state: MarketDataState, action: MarketDataAction): MarketDataState {
  switch (action.type) {
    case 'SET_STOCKS': {
      const stocks: StockMap = {};
      action.payload.forEach(s => {
        stocks[s.symbol] = { ...s, priceDirection: 'unchanged' };
      });
      return { ...state, stocks, isLoading: false, lastUpdate: new Date().toISOString() };
    }

    case 'UPDATE_STOCKS': {
      const updated = { ...state.stocks };
      action.payload.forEach(s => {
        const prev = updated[s.symbol];
        const prevPrice = prev?.lastTradePrice ?? s.lastTradePrice;
        let direction: 'up' | 'down' | 'unchanged' = 'unchanged';
        if (s.lastTradePrice > prevPrice) direction = 'up';
        else if (s.lastTradePrice < prevPrice) direction = 'down';

        updated[s.symbol] = {
          ...s,
          prevPrice,
          priceDirection: direction,
        };
      });
      return { ...state, stocks: updated, lastUpdate: new Date().toISOString() };
    }

    case 'SET_INDEXES':
      return { ...state, indexes: action.payload };

    case 'SET_MARKET_STATUS':
      return { ...state, marketStatus: action.payload };

    case 'SET_EXCHANGE':
      return { ...state, selectedExchange: action.payload };

    case 'SET_CONNECTION_STATE':
      return { ...state, connectionState: action.payload };

    case 'SET_LOADING':
      return { ...state, isLoading: action.payload };

    case 'SET_ERROR':
      return { ...state, error: action.payload };

    case 'SELECT_SYMBOL':
      return { ...state, selectedSymbol: action.payload };

    default:
      return state;
  }
}

// ── Context ──────────────────────────────────────────────────
interface MarketDataContextType {
  state: MarketDataState;
  dispatch: React.Dispatch<MarketDataAction>;
  selectExchange: (exchange: Exchange) => void;
  selectSymbol: (symbol: string | null) => void;
  getStocksByExchange: (exchange?: Exchange) => StockPriceData[];
  getTopGainers: (exchange?: Exchange, limit?: number) => StockPriceData[];
  getTopLosers: (exchange?: Exchange, limit?: number) => StockPriceData[];
}

const MarketDataContext = createContext<MarketDataContextType | null>(null);

// ── Provider ─────────────────────────────────────────────────
export function MarketDataProvider({ children }: { children: React.ReactNode }) {
  const [state, dispatch] = useReducer(marketDataReducer, initialState);
  const initialLoadDone = useRef(false);

  // Connect SignalR on mount
  useEffect(() => {
    const unsubPrice = signalRService.onStockPrices((data) => {
      if (!initialLoadDone.current) {
        dispatch({ type: 'SET_STOCKS', payload: data });
        initialLoadDone.current = true;
      } else {
        dispatch({ type: 'UPDATE_STOCKS', payload: data });
      }
    });

    const unsubState = signalRService.onConnectionStateChange((connState) => {
      dispatch({ type: 'SET_CONNECTION_STATE', payload: connState });
    });

    signalRService.startStockHub();

    // Load initial REST data
    loadInitialData();

    return () => {
      unsubPrice();
      unsubState();
    };
  }, []);

  async function loadInitialData() {
    try {
      // Load indexes and market status via REST
      const [indexes, status] = await Promise.allSettled([
        apiService.getIndexes(),
        apiService.getMarketStatus(),
      ]);

      if (indexes.status === 'fulfilled') {
        dispatch({ type: 'SET_INDEXES', payload: indexes.value });
      }
      if (status.status === 'fulfilled') {
        const statusArr = Array.isArray(status.value) ? status.value : [status.value];
        dispatch({ type: 'SET_MARKET_STATUS', payload: statusArr });
      }
    } catch (err) {
      console.warn('[MarketData] Initial REST load partial failure:', err);
    }
  }

  const selectExchange = useCallback((exchange: Exchange) => {
    dispatch({ type: 'SET_EXCHANGE', payload: exchange });
  }, []);

  const selectSymbol = useCallback((symbol: string | null) => {
    dispatch({ type: 'SELECT_SYMBOL', payload: symbol });
  }, []);

  const getStocksByExchange = useCallback((exchange?: Exchange) => {
    const ex = exchange || state.selectedExchange;
    return Object.values(state.stocks).filter(
      s => ex === 'ALL' || s.exchange === ex
    );
  }, [state.stocks, state.selectedExchange]);

  const getTopGainers = useCallback((exchange?: Exchange, limit = 10) => {
    return getStocksByExchange(exchange)
      .filter(s => s.changePercent > 0)
      .sort((a, b) => b.changePercent - a.changePercent)
      .slice(0, limit);
  }, [getStocksByExchange]);

  const getTopLosers = useCallback((exchange?: Exchange, limit = 10) => {
    return getStocksByExchange(exchange)
      .filter(s => s.changePercent < 0)
      .sort((a, b) => a.changePercent - b.changePercent)
      .slice(0, limit);
  }, [getStocksByExchange]);

  const value: MarketDataContextType = {
    state,
    dispatch,
    selectExchange,
    selectSymbol,
    getStocksByExchange,
    getTopGainers,
    getTopLosers,
  };

  return (
    <MarketDataContext.Provider value={value}>
      {children}
    </MarketDataContext.Provider>
  );
}

// ── Hook ─────────────────────────────────────────────────────
export function useMarketData(): MarketDataContextType {
  const context = useContext(MarketDataContext);
  if (!context) {
    throw new Error('useMarketData must be used within MarketDataProvider');
  }
  return context;
}
