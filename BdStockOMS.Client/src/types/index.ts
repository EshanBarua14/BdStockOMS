// Matches AuthResponseDto from backend
export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiresAt: string;
  userId: number;
  fullName: string;
  email: string;
  role: string;
  brokerageHouseId: number;
  brokerageHouseName: string;
}

// Logged-in user stored in memory
export interface AuthUser {
  userId: number;
  fullName: string;
  email: string;
  role: string;
  brokerageHouseId: number;
  brokerageHouseName: string;
  expiresAt: string;
}

// Auth context shape
export interface AuthContextType {
  user: AuthUser | null;
  token: string | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

// Generic API error
export interface ApiError {
  message: string;
  errorCode?: string;
  errors?: string[];
}

// Change password DTO
export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}
