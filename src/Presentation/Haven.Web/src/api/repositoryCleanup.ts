import { apiClient } from './client';

export interface RepositoryCleanupOptions {
  enabled: boolean;
  cronExpression: string;
  gracePeriodHours: number;
  dryRun: boolean;
}

export const repositoryCleanupApi = {
  getOptions: () => apiClient.get<RepositoryCleanupOptions>('/repository-cleanup/options'),
  updateOptions: (options: RepositoryCleanupOptions) =>
    apiClient.put<RepositoryCleanupOptions>('/repository-cleanup/options', { options }),
};
