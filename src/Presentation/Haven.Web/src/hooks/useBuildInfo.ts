import { useQuery } from '@tanstack/react-query';
import { systemApi } from '@/api/system';

export function useBuildInfo() {
  return useQuery({
    queryKey: ['build-info'],
    queryFn: systemApi.getBuildInfo,
    staleTime: Infinity,
  });
}
