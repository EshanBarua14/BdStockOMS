/* ═══════════════════════════════════════════════════════════════
   BdStockOMS — SignalR Connection Service
   Centralized hub connection management with auto-reconnect
   ═══════════════════════════════════════════════════════════════ */

import * as signalR from '@microsoft/signalr';

export interface StockPriceData {
  stockId: number;
  symbol: string;
  companyName: string;
  lastTradePrice: number;
  closingPrice: number;
  openingPrice: number;
  dayHigh: number;
  dayLow: number;
  volume: number;
  value: number;
  trade: number;
  changeAmount: number;
  changePercent: number;
  exchange: string;
  lastUpdate: string;
}

export interface NotificationData {
  id: number;
  type: string;
  title: string;
  message: string;
  severity: 'info' | 'warning' | 'error' | 'success';
  timestamp: string;
  isRead: boolean;
}

type StockPriceCallback = (data: StockPriceData[]) => void;
type NotificationCallback = (data: NotificationData) => void;
type ConnectionStateCallback = (state: signalR.HubConnectionState) => void;

class SignalRService {
  private stockHubConnection: signalR.HubConnection | null = null;
  private notifHubConnection: signalR.HubConnection | null = null;
  private stockPriceCallbacks: Set<StockPriceCallback> = new Set();
  private notificationCallbacks: Set<NotificationCallback> = new Set();
  private connectionStateCallbacks: Set<ConnectionStateCallback> = new Set();
  private _connectionState: signalR.HubConnectionState = signalR.HubConnectionState.Disconnected;

  get connectionState(): signalR.HubConnectionState {
    return this._connectionState;
  }

  private updateConnectionState(state: signalR.HubConnectionState) {
    this._connectionState = state;
    this.connectionStateCallbacks.forEach(cb => cb(state));
  }

  async startStockHub(): Promise<void> {
    if (this.stockHubConnection?.state === signalR.HubConnectionState.Connected) return;

    this.stockHubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/api/hubs/stockprice')
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: (retryContext) => {
          // Exponential backoff: 0s, 2s, 4s, 8s, 16s, max 30s
          const delay = Math.min(Math.pow(2, retryContext.previousRetryCount) * 1000, 30000);
          return delay;
        }
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.stockHubConnection.on('ReceiveStockPrices', (data: StockPriceData[]) => {
      this.stockPriceCallbacks.forEach(cb => cb(data));
    });

    this.stockHubConnection.onreconnecting(() => {
      this.updateConnectionState(signalR.HubConnectionState.Reconnecting);
      console.log('[SignalR] Stock hub reconnecting...');
    });

    this.stockHubConnection.onreconnected(() => {
      this.updateConnectionState(signalR.HubConnectionState.Connected);
      console.log('[SignalR] Stock hub reconnected');
    });

    this.stockHubConnection.onclose(() => {
      this.updateConnectionState(signalR.HubConnectionState.Disconnected);
      console.log('[SignalR] Stock hub disconnected');
    });

    try {
      await this.stockHubConnection.start();
      this.updateConnectionState(signalR.HubConnectionState.Connected);
      console.log('[SignalR] Stock hub connected');
    } catch (err) {
      this.updateConnectionState(signalR.HubConnectionState.Disconnected);
      console.error('[SignalR] Stock hub connection failed:', err);
      // Retry after 5s
      setTimeout(() => this.startStockHub(), 5000);
    }
  }

  async startNotificationHub(token: string): Promise<void> {
    if (this.notifHubConnection?.state === signalR.HubConnectionState.Connected) return;

    this.notifHubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/api/hubs/notification', {
        accessTokenFactory: () => token
      })
      .withAutomaticReconnect()
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.notifHubConnection.on('ReceiveNotification', (data: NotificationData) => {
      this.notificationCallbacks.forEach(cb => cb(data));
    });

    try {
      await this.notifHubConnection.start();
      console.log('[SignalR] Notification hub connected');
    } catch (err) {
      console.error('[SignalR] Notification hub connection failed:', err);
      setTimeout(() => this.startNotificationHub(token), 5000);
    }
  }

  // ── Subscriptions ──────────────────────────────────────────
  onStockPrices(callback: StockPriceCallback): () => void {
    this.stockPriceCallbacks.add(callback);
    return () => this.stockPriceCallbacks.delete(callback);
  }

  onNotification(callback: NotificationCallback): () => void {
    this.notificationCallbacks.add(callback);
    return () => this.notificationCallbacks.delete(callback);
  }

  onConnectionStateChange(callback: ConnectionStateCallback): () => void {
    this.connectionStateCallbacks.add(callback);
    return () => this.connectionStateCallbacks.delete(callback);
  }

  // ── Cleanup ────────────────────────────────────────────────
  async stopAll(): Promise<void> {
    await this.stockHubConnection?.stop();
    await this.notifHubConnection?.stop();
    this.stockPriceCallbacks.clear();
    this.notificationCallbacks.clear();
    this.connectionStateCallbacks.clear();
  }
}

// Singleton
export const signalRService = new SignalRService();
