import { useQuery } from '@tanstack/react-query';

import { networksApi } from '@/api/networks';
import { GetNetworksParams } from '@/api/types';

import { usePermission } from './usePermission';

const NETWORKS_KEY = 'networks';

export function useNetworks(params?: GetNetworksParams) {
  const canView = usePermission('dns.read');
  return useQuery({
    queryKey: [NETWORKS_KEY, params],
    queryFn: () => networksApi.getAll(params),
    enabled: canView,
  });
}
