import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import api from '../api/axios';

interface ProfileData {
  id: number;
  fullName: string;
  email: string;
  phone: string;
  role: string;
  brokerageHouseName: string;
  cashBalance: number;
  isActive: boolean;
  twoFactorEnabled: boolean;
  passwordChangedAt: string;
  createdAt: string;
}

export default function ProfilePage() {
  const { logout } = useAuth();
  const navigate = useNavigate();
  const [profile, setProfile] = useState<ProfileData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError]     = useState('');

  useEffect(() => {
    api.get('/auth/me')
      .then(res => setProfile(res.data))
      .catch(() => setError('Failed to load profile.'))
      .finally(() => setLoading(false));
  }, []);

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  if (loading) return (
    <div className="min-h-screen bg-gray-100 flex items-center justify-center">
      <div className="text-gray-500">Loading profile...</div>
    </div>
  );

  return (
    <div className="min-h-screen bg-gray-100">
      {/* Nav */}
      <nav className="bg-blue-700 text-white px-6 py-4 flex justify-between items-center">
        <button onClick={() => navigate(-1)} className="text-blue-200 hover:text-white text-sm">
          ← Back
        </button>
        <h1 className="font-bold">My Profile</h1>
        <button onClick={handleLogout} className="bg-white text-blue-700 text-sm font-semibold px-4 py-1 rounded-lg">
          Logout
        </button>
      </nav>

      <main className="max-w-2xl mx-auto p-6">
        {error && (
          <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-3 mb-4 text-sm">
            {error}
          </div>
        )}

        {profile && (
          <div className="bg-white rounded-xl shadow p-6 space-y-4">
            {/* Avatar */}
            <div className="flex items-center gap-4 pb-4 border-b">
              <div className="w-16 h-16 rounded-full bg-blue-100 flex items-center justify-center text-2xl font-bold text-blue-700">
                {profile.fullName.charAt(0)}
              </div>
              <div>
                <h2 className="text-xl font-bold text-gray-800">{profile.fullName}</h2>
                <p className="text-gray-500 text-sm">{profile.email}</p>
                <span className="inline-block mt-1 bg-blue-100 text-blue-700 text-xs font-semibold px-2 py-0.5 rounded-full">
                  {profile.role}
                </span>
              </div>
            </div>

            {/* Details grid */}
            <div className="grid grid-cols-2 gap-4">
              <div>
                <p className="text-xs text-gray-500">Brokerage House</p>
                <p className="font-medium text-gray-800">{profile.brokerageHouseName}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500">Cash Balance</p>
                <p className="font-medium text-green-600">৳{profile.cashBalance?.toLocaleString()}</p>
              </div>
              <div>
                <p className="text-xs text-gray-500">Account Status</p>
                <p className={`font-medium ${profile.isActive ? 'text-green-600' : 'text-red-600'}`}>
                  {profile.isActive ? '✓ Active' : '✗ Inactive'}
                </p>
              </div>
              <div>
                <p className="text-xs text-gray-500">2FA Enabled</p>
                <p className={`font-medium ${profile.twoFactorEnabled ? 'text-green-600' : 'text-gray-500'}`}>
                  {profile.twoFactorEnabled ? '✓ Enabled' : 'Disabled'}
                </p>
              </div>
            </div>

            {/* Actions */}
            <div className="pt-4 border-t">
              <button
                onClick={() => navigate('/change-password')}
                className="w-full border border-blue-300 text-blue-600 font-medium py-2.5 rounded-lg hover:bg-blue-50 transition text-sm"
              >
                Change Password
              </button>
            </div>
          </div>
        )}
      </main>
    </div>
  );
}
