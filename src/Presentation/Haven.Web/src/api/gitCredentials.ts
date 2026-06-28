import { apiClient } from './client';
import {
  PagedResult,
} from './types';
import { GetGitCredentialsParams } from "./types/git.types";
import { CreateGitCredentialInput } from "./types/git.types";
import { GitCredentialDto } from "./types/git.types";

export const gitCredentialsApi = {
  getAll: (params?: GetGitCredentialsParams) =>
    apiClient.get<PagedResult<GitCredentialDto>>('/credentials', params),

  create: (data: CreateGitCredentialInput) => apiClient.post<string>('/credentials', data),
};
