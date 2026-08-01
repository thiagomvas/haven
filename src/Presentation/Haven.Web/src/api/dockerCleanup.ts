import { apiClient } from './client';

export interface DockerCleanupOptions {
  enabled: boolean;
  cronExpression: string;
  gracePeriodHours: number;
  dryRun: boolean;
}

export const dockerCleanupApi = {
  getOptions: () => apiClient.get<DockerCleanupOptions>('/docker-cleanup/options'),
  updateOptions: (options: DockerCleanupOptions) =>
    apiClient.put<DockerCleanupOptions>('/docker-cleanup/options', { options }),
};
