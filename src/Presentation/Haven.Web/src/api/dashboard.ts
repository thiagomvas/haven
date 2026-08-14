import { apiClient } from './client';
import { DashboardOverviewDto } from './types/dashboard.types';

export const dashboardApi = {
  getOverview: () => apiClient.get<DashboardOverviewDto>('/dashboard/overview'),
};
