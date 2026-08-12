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
