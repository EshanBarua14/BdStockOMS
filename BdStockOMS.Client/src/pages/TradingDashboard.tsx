import { useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import type { Stock } from '../types/trading';
import StockList from '../components/trading/StockList';
import PlaceOrderForm from '../components/trading/PlaceOrderForm';
import OrderHistory from '../components/trading/OrderHistory';
import PortfolioPanel from '../components/trading/PortfolioPanel';

export default function TradingDashboard() {
  const { user, logout }                    = useAuth();
  const navigate                            = useNavigate();
  const [selectedStock, setSelectedStock]   = useState<Stock | null>(null);
  const [refreshTrigger, setRefreshTrigger] = useState(0);
  const [activeTab, setActiveTab]           = useState<'orders' | 'portfolio'>('orders');

  const handleOrderPlaced = useCallback(() => {
    setRefreshTrigger(t => t + 1);
  }, []);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  const canTrade = user?.role === 'Investor' || user?.role === 'Trader';

  return (
    <div className="min-h-screen bg-gray-100 flex flex-col">

      {/* Nav */}
      <nav className="bg-blue-700 text-white px-4 py-3 flex items-center justify-between shadow-lg">
        <div className="flex items-center gap-3">
          <span className="text-lg font-bold tracking-tight">BD Stock OMS</span>
          <span className="text-xs bg-blue-600 px-2 py-0.5 rounded-full">Live</span>
        </div>
        <div className="flex items-center gap-3">
          <button onClick={() => navigate('/profile')}
            className="text-sm text-blue-200 hover:text-white transition">
            {user?.fullName}
          </button>
          <span className="text-xs bg-blue-800 px-2 py-1 rounded-full font-medium">{user?.role}</span>
          <span className="text-xs text-blue-300 hidden md:block">{user?.brokerageHouseName}</span>
          <button onClick={() => navigate('/change-password')}
            className="text-xs text-blue-300 hover:text-white transition">⚙</button>
          <button onClick={handleLogout}
            className="bg-white text-blue-700 text-xs font-bold px-3 py-1.5 rounded-lg hover:bg-blue-50 transition">
            Logout
          </button>
        </div>
      </nav>

      {/* 3-column layout */}
      <div className="flex flex-1 gap-3 p-3 overflow-hidden" style={{ height: 'calc(100vh - 52px)' }}>

        {/* Col 1 — Stock list */}
        <div className="w-56 flex-shrink-0 overflow-hidden">
          <StockList
            onSelectStock={setSelectedStock}
            selectedStockId={selectedStock?.id}
          />
        </div>

        {/* Col 2 — Order form */}
        <div className="w-64 flex-shrink-0 overflow-y-auto">
          {canTrade ? (
            <PlaceOrderForm stock={selectedStock} onOrderPlaced={handleOrderPlaced} />
          ) : (
            <div className="bg-white rounded-xl shadow p-5 text-sm text-gray-500">
              <p className="font-semibold text-gray-700 mb-1">View Only</p>
              <p>Order placement is available for Investor and Trader roles.</p>
            </div>
          )}
        </div>

        {/* Col 3 — Orders + Portfolio */}
        <div className="flex-1 flex flex-col gap-3 overflow-hidden min-w-0">
          <div className="flex gap-2">
            {(['orders', 'portfolio'] as const).map(tab => (
              <button key={tab} onClick={() => setActiveTab(tab)}
                className={`px-4 py-1.5 rounded-lg text-sm font-semibold transition capitalize
                  ${activeTab === tab
                    ? 'bg-blue-600 text-white'
                    : 'bg-white text-gray-500 hover:bg-gray-50 shadow-sm'}`}>
                {tab === 'orders' ? '📋 Orders' : '💼 Portfolio'}
              </button>
            ))}
          </div>
          <div className="flex-1 overflow-y-auto">
            {activeTab === 'orders'
              ? <OrderHistory refreshTrigger={refreshTrigger} />
              : <PortfolioPanel refreshTrigger={refreshTrigger} />
            }
          </div>
        </div>
      </div>
    </div>
  );
}
