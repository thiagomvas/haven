import { apiClient } from './client';
import { GitRepositorySummaryDto } from './types/git.types';

export const gitApi = {
  getRemoteBranches: (repositoryUrl: string, gitCredentialId?: string) =>
    apiClient.get<string[]>('/git/branches', {
      repositoryUrl,
      gitCredentialId,
    }),
  getAccessibleRepositories: (gitCredentialId: string) =>
    apiClient.get<GitRepositorySummaryDto[]>('/git/repositories', {
      gitCredentialId,
    }),
};
