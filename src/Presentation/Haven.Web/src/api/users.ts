import { apiClient } from './client';
import type { UserDto, CreateUserInput } from './types';

export const usersApi = {
  getAll: () => apiClient.get<UserDto[]>('/users'),
  create: (input: CreateUserInput) => apiClient.post<UserDto>('/users', input),
  delete: (id: string) => apiClient.delete<void>(`/users/${id}`),
  getPermissions: (id: string) => apiClient.get<string[]>(`/users/${id}/permissions`),
  setPermissions: (id: string, permissions: string[]) =>
    apiClient.put<void>(`/users/${id}/permissions`, { permissions }),
};
