import { useAuth } from '../context/AuthContext';
import { useNavigate } from 'react-router-dom';

export default function DashboardPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Top navigation bar */}
      <nav className="bg-blue-700 text-white px-6 py-4 flex justify-between items-center shadow">
        <h1 className="text-xl font-bold">BD Stock OMS</h1>
        <div className="flex items-center gap-4">
          <span className="text-sm">
            {user?.fullName} — <span className="font-semibold">{user?.role}</span>
          </span>
          <button
            onClick={handleLogout}
            className="bg-white text-blue-700 text-sm font-semibold px-4 py-1 rounded-lg hover:bg-blue-50 transition"
          >
            Logout
          </button>
        </div>
      </nav>

      {/* Main content */}
      <main className="max-w-6xl mx-auto p-6">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mb-6">

          {/* Cash Balance card */}
          <div className="bg-white rounded-xl shadow p-6">
            <p className="text-sm text-gray-500 mb-1">Cash Balance</p>
            <p className="text-2xl font-bold text-green-600">
              ৳{user?.cashBalance?.toLocaleString() ?? '0'}
            </p>
          </div>

          {/* Role card */}
          <div className="bg-white rounded-xl shadow p-6">
            <p className="text-sm text-gray-500 mb-1">Your Role</p>
            <p className="text-2xl font-bold text-blue-600">{user?.role}</p>
          </div>

          {/* Status card */}
          <div className="bg-white rounded-xl shadow p-6">
            <p className="text-sm text-gray-500 mb-1">Account Status</p>
            <p className="text-2xl font-bold text-blue-600">
              {user?.isActive ? '✓ Active' : '✗ Inactive'}
            </p>
          </div>
        </div>

        {/* Placeholder for future widgets */}
        <div className="bg-white rounded-xl shadow p-6">
          <h2 className="text-lg font-semibold text-gray-700 mb-2">Welcome, {user?.fullName}!</h2>
          <p className="text-gray-500 text-sm">
            Dashboard widgets will be added in Days 31–35.
            Your backend API is fully operational with 281 passing tests.
          </p>
        </div>
      </main>
    </div>
  );
}
