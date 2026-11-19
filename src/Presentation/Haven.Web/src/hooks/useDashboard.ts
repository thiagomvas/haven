import { useQuery } from '@tanstack/react-query';

import { dashboardApi } from '@/api/dashboard';

import { usePermission } from './usePermission';

const DASHBOARD_OVERVIEW_KEY = 'dashboard-overview';

export function useDashboardOverview() {
  const canView = usePermission('projects.read');
  return useQuery({
    queryKey: [DASHBOARD_OVERVIEW_KEY],
    queryFn: () => dashboardApi.getOverview(),
    enabled: canView,
  });
}

const DASHBOARD_ATTENTION_KEY = 'dashboard-attention';
const DASHBOARD_DEPLOYMENTS_KEY = 'dashboard-deployments';

export function useDashboardAttention(limit = 5) {
  const canView = usePermission('projects.read');
  return useQuery({
    queryKey: [DASHBOARD_ATTENTION_KEY, limit],
    queryFn: () => dashboardApi.getAttention(limit),
    enabled: canView,
  });
}

export function useRecentDeployments(limit = 4) {
  const canView = usePermission('projects.read');
  return useQuery({
    queryKey: [DASHBOARD_DEPLOYMENTS_KEY, limit],
    queryFn: () => dashboardApi.getRecentDeployments(limit),
    enabled: canView,
  });
}
