import { useMemo } from 'react';

import type { DashboardInsightsDto } from '@/api/types';
import { getDashboardInsights } from '@/mocks/dashboardInsights';

/**
 * Monitoring data for the home page.
 * TODO(api): replace the mock with `dashboardApi.getInsights()` once the endpoint exists.
 */
export function useDashboardInsights(): { data: DashboardInsightsDto; isMock: true } {
  const data = useMemo(() => getDashboardInsights(), []);
  return { data, isMock: true };
}
