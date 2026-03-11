import axios from 'axios';

// Create a configured axios instance
const api = axios.create({
  baseURL: '/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor — adds JWT token to every request automatically
api.interceptors.request.use((config) => {
  // Token is stored in memory via AuthContext
  // We get it from window.__authToken (set by AuthContext)
  const token = (window as any).__authToken;
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor — handle 401 Unauthorized globally
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Clear token and redirect to login
      (window as any).__authToken = null;
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
