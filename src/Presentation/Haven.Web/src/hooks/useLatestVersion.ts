import { useQuery } from '@tanstack/react-query';

import { systemApi } from '@/api/system';

export function useLatestVersion() {
  return useQuery({
    queryKey: ['latest-version'],
    queryFn: systemApi.getLatestVersion,
    staleTime: 1000 * 60 * 60,
    retry: false,
  });
}
