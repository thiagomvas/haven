import { apiClient } from './client';
import {
  GitCredentialDto,
  CreateGitCredentialInput,
  GetGitCredentialsParams,
  PagedResult,
} from './types';

export const gitCredentialsApi = {
  getAll: (params?: GetGitCredentialsParams) =>
    apiClient.get<PagedResult<GitCredentialDto>>('/credentials', params),

  create: (data: CreateGitCredentialInput) => apiClient.post<string>('/credentials', data),
};
