import { apiClient } from './client';

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

export interface MeResponse {
  id: string;
  name: string;
  email: string;
  requirePasswordChange: boolean;
  isAdmin: boolean;
  permissions: string[];
}

export interface LoginInput {
  email: string;
  password: string;
}

export interface SetPasswordInput {
  newPassword: string;
  confirmPassword: string;
}

export const authApi = {
  login: (input: LoginInput) => apiClient.post<AuthResponse>('/auth/login', input),
  me: () => apiClient.get<MeResponse>('/auth/me'),
  logout: () => apiClient.post<void>('/auth/logout', {}),
  setPassword: (input: SetPasswordInput) => apiClient.post<void>('/auth/set-password', input),
};
