import { useState } from 'react';
import type { Stock, OrderType, OrderCategory, PlaceOrderRequest } from '../../types/trading';
import { placeOrder } from '../../services/tradingApi';

interface Props {
  stock: Stock | null;
  onOrderPlaced: () => void;
}

export default function PlaceOrderForm({ stock, onOrderPlaced }: Props) {
  const [orderType, setOrderType]         = useState<OrderType>('Buy');
  const [orderCategory, setOrderCategory] = useState<OrderCategory>('Market');
  const [quantity, setQuantity]           = useState('');
  const [limitPrice, setLimitPrice]       = useState('');
  const [loading, setLoading]             = useState(false);
  const [error, setError]                 = useState('');
  const [success, setSuccess]             = useState('');

  if (!stock) {
    return (
      <div className="bg-white rounded-xl shadow p-6 flex flex-col items-center justify-center h-48 text-gray-400">
        <div className="text-4xl mb-3">📈</div>
        <p className="text-sm">Select a stock to place an order</p>
      </div>
    );
  }

  const price      = orderCategory === 'Market' ? stock.lastTradePrice : (parseFloat(limitPrice) || 0);
  const qty        = parseInt(quantity) || 0;
  const totalValue = price * qty;
  const isUp       = stock.change >= 0;

  const handleSubmit = async () => {
    setError(''); setSuccess('');
    if (!quantity || parseInt(quantity) <= 0) { setError('Enter a valid quantity.'); return; }
    if (orderCategory === 'Limit' && (!limitPrice || parseFloat(limitPrice) <= 0)) {
      setError('Enter a valid limit price.'); return;
    }
    const dto: PlaceOrderRequest = {
      stockId: stock.id,
      orderType,
      orderCategory,
      quantity: parseInt(quantity),
      ...(orderCategory === 'Limit' && { limitPrice: parseFloat(limitPrice) }),
    };
    try {
      setLoading(true);
      await placeOrder(dto);
      setSuccess(`${orderType} order placed — ${quantity} × ${stock.tradingCode}`);
      setQuantity(''); setLimitPrice('');
      onOrderPlaced();
    } catch (err: any) {
      setError(err.response?.data?.message ?? 'Failed to place order.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="bg-white rounded-xl shadow p-5 flex flex-col gap-4">
      {/* Stock header */}
      <div className="flex items-center justify-between pb-3 border-b border-gray-100">
        <div>
          <div className="flex items-center gap-2">
            <span className="text-lg font-bold text-gray-800">{stock.tradingCode}</span>
            <span className={`text-xs px-2 py-0.5 rounded font-medium
              ${stock.exchange === 'DSE' ? 'bg-blue-100 text-blue-700' : 'bg-purple-100 text-purple-700'}`}>
              {stock.exchange}
            </span>
          </div>
          <div className="text-xs text-gray-400">{stock.companyName}</div>
        </div>
        <div className="text-right">
          <div className="text-2xl font-bold text-gray-800">৳{stock.lastTradePrice.toFixed(2)}</div>
          <div className={`text-sm font-medium ${isUp ? 'text-green-600' : 'text-red-600'}`}>
            {isUp ? '▲' : '▼'} {Math.abs(stock.change).toFixed(2)} ({Math.abs(stock.changePercent).toFixed(2)}%)
          </div>
        </div>
      </div>

      {/* Circuit breaker */}
      <div className="flex justify-between text-xs text-gray-400 bg-gray-50 rounded-lg px-3 py-2">
        <span>Low: <span className="text-red-500 font-medium">৳{stock.circuitBreakerLow.toFixed(2)}</span></span>
        <span>High: <span className="text-green-600 font-medium">৳{stock.circuitBreakerHigh.toFixed(2)}</span></span>
      </div>

      {/* Buy / Sell */}
      <div className="grid grid-cols-2 gap-2">
        {(['Buy', 'Sell'] as OrderType[]).map(t => (
          <button key={t} onClick={() => setOrderType(t)}
            className={`py-2 rounded-lg text-sm font-bold transition
              ${orderType === t
                ? t === 'Buy' ? 'bg-green-600 text-white' : 'bg-red-600 text-white'
                : 'bg-gray-100 text-gray-500 hover:bg-gray-200'}`}>
            {t}
          </button>
        ))}
      </div>

      {/* Market / Limit */}
      <div className="grid grid-cols-2 gap-2">
        {(['Market', 'Limit'] as OrderCategory[]).map(c => (
          <button key={c} onClick={() => setOrderCategory(c)}
            className={`py-1.5 rounded-lg text-xs font-semibold transition
              ${orderCategory === c ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-500 hover:bg-gray-200'}`}>
            {c}
          </button>
        ))}
      </div>

      {/* Limit price */}
      {orderCategory === 'Limit' && (
        <div>
          <label className="text-xs text-gray-500 mb-1 block">Limit Price (৳)</label>
          <input type="number" value={limitPrice} onChange={e => setLimitPrice(e.target.value)}
            placeholder={stock.lastTradePrice.toFixed(2)}
            className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
        </div>
      )}

      {/* Quantity */}
      <div>
        <label className="text-xs text-gray-500 mb-1 block">Quantity (min: {stock.boardLotSize})</label>
        <input type="number" value={quantity} onChange={e => setQuantity(e.target.value)}
          placeholder="0" min={stock.boardLotSize}
          className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500" />
      </div>

      {/* Estimated value */}
      {qty > 0 && (
        <div className={`rounded-lg px-3 py-2 text-sm font-medium
          ${orderType === 'Buy' ? 'bg-green-50 text-green-800' : 'bg-red-50 text-red-800'}`}>
          Estimated {orderType}: <span className="font-bold">৳{totalValue.toLocaleString('en-BD', { minimumFractionDigits: 2 })}</span>
        </div>
      )}

      {error   && <div className="text-xs text-red-600 bg-red-50 rounded-lg px-3 py-2">{error}</div>}
      {success && <div className="text-xs text-green-700 bg-green-50 rounded-lg px-3 py-2">{success}</div>}

      <button onClick={handleSubmit} disabled={loading}
        className={`w-full py-2.5 rounded-lg text-sm font-bold transition
          ${orderType === 'Buy' ? 'bg-green-600 hover:bg-green-700 text-white' : 'bg-red-600 hover:bg-red-700 text-white'}
          disabled:opacity-50 disabled:cursor-not-allowed`}>
        {loading ? 'Placing...' : `Place ${orderType} Order`}
      </button>
    </div>
  );
}
