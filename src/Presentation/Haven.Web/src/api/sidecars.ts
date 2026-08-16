import { apiClient } from './client';
import { SidecarDto } from './types/sidecar.types';

export const sidecarsApi = {
  getAll: () => apiClient.get<SidecarDto[]>('/sidecars'),

  enable: (sidecarId: string) => apiClient.post<void>(`/sidecars/${sidecarId}/enable`, null),

  disable: (sidecarId: string) => apiClient.post<void>(`/sidecars/${sidecarId}/disable`, null),
};
