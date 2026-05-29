import { apiClient } from './client'

export interface AuthResponse {
  accessToken: string
  refreshToken: string
  expiresAt: string
}

export interface LoginInput {
  email: string
  password: string
}

export const authApi = {
  login: (input: LoginInput) =>
    apiClient.post<AuthResponse>('/auth/login', input),
}
