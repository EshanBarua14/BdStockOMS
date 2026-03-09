import { useState, useEffect, useCallback } from 'react';
import type { OrderResponse, OrderStatus } from '../../types/trading';
import { fetchOrders, cancelOrder, executeOrder } from '../../services/tradingApi';
import { useAuth } from '../../context/AuthContext';

interface Props { refreshTrigger: number; }

const STATUS_COLORS: Record<OrderStatus, string> = {
  Pending:   'bg-yellow-100 text-yellow-700',
  Executed:  'bg-blue-100 text-blue-700',
  Completed: 'bg-green-100 text-green-700',
  Cancelled: 'bg-gray-100 text-gray-500',
  Rejected:  'bg-red-100 text-red-700',
};

export default function OrderHistory({ refreshTrigger }: Props) {
  const { user }              = useAuth();
  const [orders, setOrders]   = useState<OrderResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [actionId, setActionId] = useState<number | null>(null);
  const [page, setPage]       = useState(1);
  const PAGE_SIZE = 8;

  const load = useCallback(() => {
    setLoading(true);
    fetchOrders().then(setOrders).finally(() => setLoading(false));
  }, []);

  useEffect(() => { load(); }, [load, refreshTrigger]);

  const handleCancel = async (id: number) => {
    setActionId(id);
    try { await cancelOrder(id, 'Cancelled by user'); load(); }
    catch (err: any) { alert(err.response?.data?.message ?? 'Cancel failed.'); }
    finally { setActionId(null); }
  };

  const handleExecute = async (id: number) => {
    setActionId(id);
    try { await executeOrder(id); load(); }
    catch (err: any) { alert(err.response?.data?.message ?? 'Execute failed.'); }
    finally { setActionId(null); }
  };

  const paginated  = orders.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);
  const totalPages = Math.ceil(orders.length / PAGE_SIZE);

  return (
    <div className="bg-white rounded-xl shadow overflow-hidden">
      <div className="px-5 py-3 border-b border-gray-100 flex items-center justify-between">
        <h2 className="text-sm font-bold text-gray-700">Order History</h2>
        <span className="text-xs text-gray-400">{orders.length} orders</span>
      </div>

      {loading ? (
        <div className="flex items-center justify-center h-32 text-gray-400 text-sm">Loading orders...</div>
      ) : orders.length === 0 ? (
        <div className="flex items-center justify-center h-32 text-gray-400 text-sm">No orders yet</div>
      ) : (
        <>
          <div className="overflow-x-auto">
            <table className="w-full text-xs">
              <thead className="bg-gray-50 border-b border-gray-100">
                <tr>
                  {['#','Stock','Type','Cat','Qty','Price','Value','Status','Time','Action'].map(h => (
                    <th key={h} className="px-3 py-2 text-left font-semibold text-gray-500 whitespace-nowrap">{h}</th>
                  ))}
                </tr>
              </thead>
              <tbody>
                {paginated.map((o, i) => (
                  <tr key={o.id} className={i % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                    <td className="px-3 py-2 text-gray-400">{o.id}</td>
                    <td className="px-3 py-2">
                      <div className="font-bold text-gray-800">{o.tradingCode}</div>
                      <div className="text-gray-400">{o.exchange}</div>
                    </td>
                    <td className="px-3 py-2">
                      <span className={`font-bold ${o.orderType === 'Buy' ? 'text-green-600' : 'text-red-600'}`}>
                        {o.orderType}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-gray-600">{o.orderCategory}</td>
                    <td className="px-3 py-2 tabular-nums">{o.quantity}</td>
                    <td className="px-3 py-2 tabular-nums">৳{(o.executionPrice ?? o.priceAtOrder).toFixed(2)}</td>
                    <td className="px-3 py-2 tabular-nums">৳{o.totalValue.toLocaleString()}</td>
                    <td className="px-3 py-2">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${STATUS_COLORS[o.status]}`}>
                        {o.status}
                      </span>
                    </td>
                    <td className="px-3 py-2 text-gray-400 whitespace-nowrap">
                      {new Date(o.createdAt).toLocaleTimeString('en-BD', { hour: '2-digit', minute: '2-digit' })}
                    </td>
                    <td className="px-3 py-2">
                      {o.status === 'Pending' && (
                        <div className="flex gap-1">
                          {user?.role === 'Trader' && (
                            <button onClick={() => handleExecute(o.id)} disabled={actionId === o.id}
                              className="text-xs bg-blue-600 text-white px-2 py-0.5 rounded hover:bg-blue-700 disabled:opacity-50">
                              {actionId === o.id ? '...' : 'Execute'}
                            </button>
                          )}
                          {['Investor','Trader','Admin','SuperAdmin'].includes(user?.role ?? '') && (
                            <button onClick={() => handleCancel(o.id)} disabled={actionId === o.id}
                              className="text-xs bg-red-100 text-red-600 px-2 py-0.5 rounded hover:bg-red-200 disabled:opacity-50">
                              {actionId === o.id ? '...' : 'Cancel'}
                            </button>
                          )}
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          {totalPages > 1 && (
            <div className="px-4 py-2 border-t border-gray-100 flex items-center justify-between">
              <span className="text-xs text-gray-400">Page {page} of {totalPages}</span>
              <div className="flex gap-1">
                <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
                  className="text-xs px-2 py-1 rounded border border-gray-200 disabled:opacity-40 hover:bg-gray-50">← Prev</button>
                <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page === totalPages}
                  className="text-xs px-2 py-1 rounded border border-gray-200 disabled:opacity-40 hover:bg-gray-50">Next →</button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
