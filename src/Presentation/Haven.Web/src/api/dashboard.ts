import { apiClient } from './client';
import type {
  AttentionServiceDto,
  DashboardOverviewDto,
  RecentDeploymentDto,
} from './types/dashboard.types';

export const dashboardApi = {
  getOverview: () => apiClient.get<DashboardOverviewDto>('/dashboard/overview'),
  getAttention: (limit?: number) =>
    apiClient.get<AttentionServiceDto[]>('/dashboard/attention', { limit }),
  getRecentDeployments: (limit?: number) =>
    apiClient.get<RecentDeploymentDto[]>('/dashboard/deployments', { limit }),
};
