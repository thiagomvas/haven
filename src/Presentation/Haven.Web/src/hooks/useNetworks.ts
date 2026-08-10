import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { networksApi } from '@/api/networks';
import { CreateNetworkInput, GetNetworksParams } from '@/api/types';

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

export function useCreateNetwork() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateNetworkInput) => networksApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [NETWORKS_KEY] });
    },
  });
}

export function useDeleteNetwork() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (networkId: string) => networksApi.delete(networkId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [NETWORKS_KEY] });
    },
  });
}

export function useAssignServiceToNetwork() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ networkId, serviceId }: { networkId: string; serviceId: string }) =>
      networksApi.assignService(networkId, serviceId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [NETWORKS_KEY] });
    },
  });
}

export function useUnassignServiceFromNetwork() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ networkId, serviceId }: { networkId: string; serviceId: string }) =>
      networksApi.unassignService(networkId, serviceId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [NETWORKS_KEY] });
    },
  });
}
