import { apiClient } from './client'
import { AuthResponse } from './auth'

export interface RegisterInput {
  name: string
  email: string
  password: string
}

export const setupApi = {
  register: (input: RegisterInput) =>
    apiClient.post<AuthResponse>('/setup/register', input),
}
