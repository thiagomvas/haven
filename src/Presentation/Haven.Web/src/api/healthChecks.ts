import { apiClient } from './client';
import { CreateHealthCheckInput } from './types/healthCheck.types';
import { HealthCheckDto } from './types/healthCheck.types';
import { UpdateHealthCheckInput } from './types/healthCheck.types';

const base = (projectId: string, environmentId: string, serviceId: string) =>
  `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/health-checks`;

export const healthChecksApi = {
  list: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<HealthCheckDto[]>(base(projectId, environmentId, serviceId)),

  create: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    body: CreateHealthCheckInput
  ) => apiClient.post<string>(base(projectId, environmentId, serviceId), body),

  update: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    healthCheckId: string,
    body: UpdateHealthCheckInput
  ) => apiClient.patch<void>(`${base(projectId, environmentId, serviceId)}/${healthCheckId}`, body),

  delete: (projectId: string, environmentId: string, serviceId: string, healthCheckId: string) =>
    apiClient.delete<void>(`${base(projectId, environmentId, serviceId)}/${healthCheckId}`),

  runNow: (projectId: string, environmentId: string, serviceId: string, healthCheckId: string) =>
    apiClient.post<void>(
      `${base(projectId, environmentId, serviceId)}/${healthCheckId}/run`,
      undefined
    ),
};
