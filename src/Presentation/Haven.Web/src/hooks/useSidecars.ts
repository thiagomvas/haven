import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { sidecarsApi } from '@/api/sidecars';

import { usePermission } from './usePermission';

const SIDECARS_KEY = 'sidecars';

export function useSidecars() {
  const canView = usePermission('sidecars.read');
  return useQuery({
    queryKey: [SIDECARS_KEY],
    queryFn: () => sidecarsApi.getAll(),
    enabled: canView,
  });
}

export function useEnableSidecar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (sidecarId: string) => sidecarsApi.enable(sidecarId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [SIDECARS_KEY] });
    },
  });
}

export function useDisableSidecar() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (sidecarId: string) => sidecarsApi.disable(sidecarId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [SIDECARS_KEY] });
    },
  });
}

export function useExportSidecarManifest() {
  return useMutation({
    mutationFn: (sidecarId: string) => sidecarsApi.exportManifest(sidecarId),
  });
}

export function useImportSidecarManifest() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ sidecarId, manifestYaml }: { sidecarId: string; manifestYaml?: string }) =>
      sidecarsApi.importManifest(sidecarId, manifestYaml),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [SIDECARS_KEY] });
    },
  });
}
