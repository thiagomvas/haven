import { apiClient } from './client';
import { PagedResult } from './types';
import { GetGitCredentialsParams } from './types/git.types';
import { CreateGitCredentialInput } from './types/git.types';
import { UpdateGitCredentialInput } from './types/git.types';
import { RotateGitCredentialInput } from './types/git.types';
import { GitCredentialDto } from './types/git.types';

export const gitCredentialsApi = {
  getAll: (params?: GetGitCredentialsParams) =>
    apiClient.get<PagedResult<GitCredentialDto>>('/credentials', params),

  create: (data: CreateGitCredentialInput) => apiClient.post<string>('/credentials', data),

  update: (id: string, data: UpdateGitCredentialInput) =>
    apiClient.patch<string>(`/credentials/${id}`, data),

  rotate: (id: string, data: RotateGitCredentialInput) =>
    apiClient.post<string>(`/credentials/${id}/rotate`, data),

  delete: (id: string) => apiClient.delete(`/credentials/${id}`),

  startGitHubOAuth: (credentialId?: string) =>
    apiClient.get<string>('/github/oauth/authorize', credentialId ? { credentialId } : undefined),
};
