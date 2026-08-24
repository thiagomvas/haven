import { apiClient } from './client';

export interface TraefikDashboardAuthDto {
  enabled: boolean;
  username?: string;
}

export interface UpdateTraefikDashboardAuthInput {
  enabled: boolean;
  username: string;
  password?: string;
}

const base = '/sidecars/traefik/dashboard-auth';

export const traefikDashboardAuthApi = {
  get: () => apiClient.get<TraefikDashboardAuthDto>(base),

  update: (body: UpdateTraefikDashboardAuthInput) =>
    apiClient.patch<TraefikDashboardAuthDto>(base, body),
};
