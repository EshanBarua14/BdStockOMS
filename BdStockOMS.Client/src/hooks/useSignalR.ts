import { useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import type { PriceUpdate } from '../types/trading';

interface UseSignalROptions {
  onPriceUpdate?: (update: PriceUpdate) => void;
  onMarketUpdate?: (data: any) => void;
  subscribeTo?: string[];
}

export function useSignalR(options: UseSignalROptions) {
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const { onPriceUpdate, onMarketUpdate, subscribeTo } = options;

  const connect = useCallback(async () => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) return;

    const token = (window as any).__authToken;

    const connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/stockprice', {
        accessTokenFactory: () => token ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    if (onPriceUpdate) {
      connection.on('PriceUpdate', (data: PriceUpdate) => {
        onPriceUpdate(data);
      });
    }

    if (onMarketUpdate) {
      connection.on('MarketUpdate', (data: any) => {
        onMarketUpdate(data);
      });
    }

    try {
      await connection.start();
      connectionRef.current = connection;

      if (subscribeTo?.length) {
        for (const code of subscribeTo) {
          await connection.invoke('SubscribeToStock', code);
        }
      }
    } catch (err) {
      console.warn('SignalR connection failed:', err);
    }
  }, [onPriceUpdate, onMarketUpdate, subscribeTo]);

  useEffect(() => {
    connect();
    return () => {
      connectionRef.current?.stop();
      connectionRef.current = null;
    };
  }, [connect]);

  const subscribeToStock = useCallback(async (tradingCode: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('SubscribeToStock', tradingCode);
    }
  }, []);

  const unsubscribeFromStock = useCallback(async (tradingCode: string) => {
    if (connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      await connectionRef.current.invoke('UnsubscribeFromStock', tradingCode);
    }
  }, []);

  return { subscribeToStock, unsubscribeFromStock };
}
