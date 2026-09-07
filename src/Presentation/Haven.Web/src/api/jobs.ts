import { apiClient } from './client';
import { JobInfoDto } from './types/job.types';

export const jobsApi = {
  getAll: () => apiClient.get<JobInfoDto[]>('/jobs'),

  trigger: (jobKey: string) => apiClient.post<void>('/jobs/trigger', { jobKey }),
};
