import { apiClient } from './client';
import {
  CreateHealthCheckInput,
  HealthCheckDto,
  HealthCheckResultDto,
  TestHealthCheckInput,
  UpdateHealthCheckInput,
} from './types/healthCheck.types';

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

  /** Runs the check immediately and returns its result. */
  runNow: (projectId: string, environmentId: string, serviceId: string, healthCheckId: string) =>
    apiClient.post<HealthCheckResultDto>(
      `${base(projectId, environmentId, serviceId)}/${healthCheckId}/run`,
      undefined
    ),

  results: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    healthCheckId: string,
    limit = 50
  ) =>
    apiClient.get<HealthCheckResultDto[]>(
      `${base(projectId, environmentId, serviceId)}/${healthCheckId}/results`,
      { limit }
    ),

  /** Runs an unsaved configuration once and returns the outcome; nothing is stored. */
  test: (projectId: string, environmentId: string, serviceId: string, body: TestHealthCheckInput) =>
    apiClient.post<HealthCheckResultDto>(`${base(projectId, environmentId, serviceId)}/test`, body),
};
