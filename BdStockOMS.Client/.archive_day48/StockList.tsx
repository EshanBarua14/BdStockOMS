// @ts-nocheck
import { useState, useEffect, useCallback } from 'react';
import type { Stock, PriceUpdate } from '../../types/trading';
import { fetchStocks, searchStocks } from '../../services/tradingApi';
import { useSignalR } from '../../hooks/useSignalR';

interface Props {
  onSelectStock: (stock: Stock) => void;
  selectedStockId?: number;
}

export default function StockList({ onSelectStock, selectedStockId }: Props) {
  const [stocks, setStocks]     = useState<Stock[]>([]);
  const [query, setQuery]       = useState('');
  const [loading, setLoading]   = useState(true);
  const [flashMap, setFlashMap] = useState<Record<number, 'up' | 'down'>>({});

  useEffect(() => {
    fetchStocks()
      .then(setStocks)
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!query.trim()) {
      fetchStocks().then(setStocks);
      return;
    }
    const t = setTimeout(() => {
      searchStocks(query).then(setStocks);
    }, 300);
    return () => clearTimeout(t);
  }, [query]);

  const handlePriceUpdate = useCallback((update: PriceUpdate) => {
    setStocks(prev => prev.map(s =>
      s.id === update.stockId
        ? { ...s,
            lastTradePrice: update.lastTradePrice,
            change: update.change,
            changePercent: update.changePercent }
        : s
    ));
    setFlashMap(prev => ({ ...prev, [update.stockId]: update.change >= 0 ? 'up' : 'down' }));
    setTimeout(() => {
      setFlashMap(prev => { const n = { ...prev }; delete n[update.stockId]; return n; });
    }, 800);
  }, []);

  useSignalR({ onPriceUpdate: handlePriceUpdate });

  const filtered = stocks.filter(s =>
    s.tradingCode.toLowerCase().includes(query.toLowerCase()) ||
    s.companyName.toLowerCase().includes(query.toLowerCase())
  );

  return (
    <div className="flex flex-col h-full bg-white rounded-xl shadow overflow-hidden">
      <div className="px-4 py-3 border-b border-gray-100">
        <h2 className="text-sm font-bold text-gray-700 mb-2">Market Watch</h2>
        <input
          type="text"
          value={query}
          onChange={e => setQuery(e.target.value)}
          placeholder="Search stocks..."
          className="w-full text-sm border border-gray-200 rounded-lg px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-blue-500"
        />
      </div>
      <div className="flex-1 overflow-y-auto">
        {loading ? (
          <div className="flex items-center justify-center h-32 text-gray-400 text-sm">Loading stocks...</div>
        ) : filtered.length === 0 ? (
          <div className="flex items-center justify-center h-32 text-gray-400 text-sm">No stocks found</div>
        ) : (
          filtered.map(stock => {
            const flash = flashMap[stock.id];
            const isUp  = stock.change >= 0;
            const isSelected = stock.id === selectedStockId;
            return (
              <button
                key={stock.id}
                onClick={() => onSelectStock(stock)}
                className={[
                  'w-full px-4 py-2.5 flex items-center justify-between text-left',
                  'border-b border-gray-50 transition-all duration-150',
                  isSelected ? 'bg-blue-50 border-l-4 border-l-blue-500' : 'hover:bg-gray-50',
                  flash === 'up'   ? 'bg-green-50' : '',
                  flash === 'down' ? 'bg-red-50'   : '',
                ].join(' ')}
              >
                <div>
                  <div className="flex items-center gap-1.5">
                    <span className="text-sm font-bold text-gray-800">{stock.tradingCode}</span>
                    <span className={`text-xs px-1.5 py-0.5 rounded font-medium
                      ${stock.exchange === 'DSE' ? 'bg-blue-100 text-blue-700' : 'bg-purple-100 text-purple-700'}`}>
                      {stock.exchange}
                    </span>
                    <span className="text-xs px-1.5 py-0.5 rounded bg-gray-100 text-gray-500">
                      {stock.category}
                    </span>
                  </div>
                  <div className="text-xs text-gray-400 truncate max-w-[140px]">{stock.companyName}</div>
                </div>
                <div className="text-right">
                  <div className={`text-sm font-bold tabular-nums transition-colors
                    ${flash === 'up' ? 'text-green-600' : flash === 'down' ? 'text-red-600' : 'text-gray-800'}`}>
                    ৳{stock.lastTradePrice.toFixed(2)}
                  </div>
                  <div className={`text-xs font-medium tabular-nums
                    ${isUp ? 'text-green-600' : 'text-red-600'}`}>
                    {isUp ? '▲' : '▼'} {Math.abs(stock.changePercent).toFixed(2)}%
                  </div>
                </div>
              </button>
            );
          })
        )}
      </div>
    </div>
  );
}
