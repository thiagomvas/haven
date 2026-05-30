import { apiClient } from './client'
import type { UserDto, CreateUserInput } from './types'

export const usersApi = {
  getAll: () => apiClient.get<UserDto[]>('/users'),
  create: (input: CreateUserInput) => apiClient.post<UserDto>('/users', input),
  delete: (id: string) => apiClient.delete<void>(`/users/${id}`),
}
