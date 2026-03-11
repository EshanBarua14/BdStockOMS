import { useState, useEffect } from 'react';
import type { PortfolioSummary } from '../../types/trading';
import { fetchPortfolioSummary } from '../../services/tradingApi';
import { useAuth } from '../../context/AuthContext';

interface Props { refreshTrigger: number; }

export default function PortfolioPanel({ refreshTrigger }: Props) {
  const { user }              = useAuth();
  const [summary, setSummary] = useState<PortfolioSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  useEffect(() => {
    if (!user?.userId) return;
    setLoading(true);
    fetchPortfolioSummary(user.userId)
      .then(setSummary)
      .catch(() => setError('Could not load portfolio.'))
      .finally(() => setLoading(false));
  }, [user?.userId, refreshTrigger]);

  if (loading) return (
    <div className="bg-white rounded-xl shadow p-6 flex items-center justify-center h-32 text-gray-400 text-sm">
      Loading portfolio...
    </div>
  );
  if (error) return <div className="bg-white rounded-xl shadow p-6 text-red-500 text-sm">{error}</div>;
  if (!summary) return null;

  const pnlPos = summary.totalUnrealizedPnL >= 0;

  return (
    <div className="bg-white rounded-xl shadow overflow-hidden">
      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 border-b border-gray-100">
        {[
          { label: 'Cash Balance',    value: `৳${summary.cashBalance.toLocaleString()}`,         color: 'text-blue-600' },
          { label: 'Portfolio Value', value: `৳${summary.totalCurrentValue.toLocaleString()}`,   color: 'text-gray-800' },
          { label: 'Unrealized P&L',  value: `৳${summary.totalUnrealizedPnL.toLocaleString()}`,  color: pnlPos ? 'text-green-600' : 'text-red-600' },
          { label: 'Total Assets',    value: `৳${summary.totalPortfolioValue.toLocaleString()}`, color: 'text-gray-800' },
        ].map(card => (
          <div key={card.label} className="px-4 py-3 border-r border-gray-100 last:border-r-0">
            <div className="text-xs text-gray-400 mb-0.5">{card.label}</div>
            <div className={`text-sm font-bold ${card.color}`}>{card.value}</div>
          </div>
        ))}
      </div>

      {/* Holdings */}
      <div className="px-4 py-2 border-b border-gray-50">
        <h3 className="text-xs font-bold text-gray-600">Holdings ({summary.holdings.length})</h3>
      </div>

      {summary.holdings.length === 0 ? (
        <div className="flex items-center justify-center h-16 text-gray-400 text-xs">
          No holdings yet — place a buy order to get started
        </div>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-xs">
            <thead className="bg-gray-50">
              <tr>
                {['Stock','Exch','Qty','Avg Buy','Current','Cost','Value','P&L','P&L%'].map(h => (
                  <th key={h} className="px-3 py-2 text-left font-semibold text-gray-500 whitespace-nowrap">{h}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {summary.holdings.map((h, i) => {
                const pos = h.unrealizedPnL >= 0;
                return (
                  <tr key={h.portfolioId} className={i % 2 === 0 ? 'bg-white' : 'bg-gray-50'}>
                    <td className="px-3 py-2 font-bold text-gray-800">{h.tradingCode}</td>
                    <td className="px-3 py-2 text-gray-500">{h.exchange}</td>
                    <td className="px-3 py-2 tabular-nums">{h.quantity}</td>
                    <td className="px-3 py-2 tabular-nums">৳{h.averageBuyPrice.toFixed(2)}</td>
                    <td className="px-3 py-2 tabular-nums">৳{h.currentPrice.toFixed(2)}</td>
                    <td className="px-3 py-2 tabular-nums">৳{h.costBasis.toLocaleString()}</td>
                    <td className="px-3 py-2 tabular-nums">৳{h.currentValue.toLocaleString()}</td>
                    <td className={`px-3 py-2 tabular-nums font-medium ${pos ? 'text-green-600' : 'text-red-600'}`}>
                      {pos ? '+' : ''}৳{h.unrealizedPnL.toFixed(2)}
                    </td>
                    <td className={`px-3 py-2 tabular-nums font-medium ${pos ? 'text-green-600' : 'text-red-600'}`}>
                      {pos ? '+' : ''}{h.pnLPercent.toFixed(2)}%
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
