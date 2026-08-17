import { apiClient } from './client';
import { SidecarDto } from './types/sidecar.types';

export const sidecarsApi = {
  getAll: () => apiClient.get<SidecarDto[]>('/sidecars'),

  enable: (sidecarId: string) => apiClient.post<void>(`/sidecars/${sidecarId}/enable`, null),

  disable: (sidecarId: string) => apiClient.post<void>(`/sidecars/${sidecarId}/disable`, null),

  exportManifest: (sidecarId: string) =>
    apiClient.post<void>(`/sidecars/${sidecarId}/export`, null),

  getManifest: (sidecarId: string) => apiClient.get<string>(`/sidecars/${sidecarId}/manifest`),

  importManifest: (sidecarId: string, manifestYaml?: string) =>
    apiClient.post<void>(`/sidecars/${sidecarId}/import`, { manifestYaml }),
};
