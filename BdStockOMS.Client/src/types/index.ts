// Matches your backend User model
export interface User {
  id: number;
  fullName: string;
  email: string;
  role: string;
  brokerageHouseId: number;
  isActive: boolean;
  cashBalance: number;
}

// Returned by /api/auth/login
export interface LoginResponse {
  token: string;
  expiration: string;
  user: User;
}

// Sent to /api/auth/login
export interface LoginRequest {
  email: string;
  password: string;
}

// Auth context shape — stored in memory
export interface AuthContextType {
  user: User | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

// Generic API error response
export interface ApiError {
  message: string;
  errors?: string[];
}
