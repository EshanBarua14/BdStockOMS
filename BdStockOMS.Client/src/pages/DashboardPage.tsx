import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function DashboardPage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Top navigation bar */}
      <nav className="bg-blue-700 text-white px-6 py-4 flex justify-between items-center shadow">
        <h1 className="text-xl font-bold">BD Stock OMS</h1>
        <div className="flex items-center gap-4">
          <button
            onClick={() => navigate('/profile')}
            className="text-blue-200 hover:text-white text-sm transition"
          >
            {user?.fullName} — <span className="font-semibold">{user?.role}</span>
          </button>
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

          {/* Brokerage House card */}
          <div className="bg-white rounded-xl shadow p-6">
            <p className="text-sm text-gray-500 mb-1">Brokerage House</p>
            <p className="text-xl font-bold text-blue-600">
              {user?.brokerageHouseName ?? '—'}
            </p>
          </div>

          {/* Role card */}
          <div className="bg-white rounded-xl shadow p-6">
            <p className="text-sm text-gray-500 mb-1">Your Role</p>
            <p className="text-xl font-bold text-blue-600">{user?.role}</p>
          </div>

          {/* Email card */}
          <div className="bg-white rounded-xl shadow p-6">
            <p className="text-sm text-gray-500 mb-1">Email</p>
            <p className="text-xl font-bold text-gray-700">{user?.email}</p>
          </div>
        </div>

        {/* Welcome card */}
        <div className="bg-white rounded-xl shadow p-6">
          <h2 className="text-lg font-semibold text-gray-700 mb-2">
            Welcome, {user?.fullName}!
          </h2>
          <p className="text-gray-500 text-sm mb-4">
            Your BD Stock OMS dashboard. Full trading widgets coming in Days 31–35.
          </p>
          <div className="flex gap-3">
            <button
              onClick={() => navigate('/profile')}
              className="bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium px-4 py-2 rounded-lg transition"
            >
              View Profile
            </button>
            <button
              onClick={() => navigate('/change-password')}
              className="border border-gray-300 text-gray-700 text-sm font-medium px-4 py-2 rounded-lg hover:bg-gray-50 transition"
            >
              Change Password
            </button>
          </div>
        </div>
      </main>
    </div>
  );
}
