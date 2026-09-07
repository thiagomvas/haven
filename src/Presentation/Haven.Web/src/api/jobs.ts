import { apiClient } from './client';
import { JobInfoDto } from './types/job.types';

export const jobsApi = {
  getAll: () => apiClient.get<JobInfoDto[]>('/jobs'),
};
