import { apiClient } from './client';
import { UpdateEnvironmentInput } from './types/environment.types';
import { CreateEnvironmentInput } from './types/environment.types';
import { EnvironmentDashboardDto } from './types/environment.types';
import { EnvironmentDto } from './types/environment.types';

export interface CloneEnvironmentInput {
  newName: string;
  newAlias?: string;
  targetProjectId?: string;
}

export const environmentsApi = {
  getByProjectId: (projectId: string) =>
    apiClient.get<EnvironmentDto[]>(`/projects/${projectId}/environments`),

  getById: (projectId: string, environmentId: string) =>
    apiClient.get<EnvironmentDto>(`/projects/${projectId}/environments/${environmentId}`),

  getDashboard: (projectId: string, environmentId: string) =>
    apiClient.get<EnvironmentDashboardDto>(
      `/projects/${projectId}/environments/${environmentId}/dashboard`
    ),

  create: (projectId: string, body: CreateEnvironmentInput) =>
    apiClient.post<string>(`/projects/${projectId}/environments`, body),

  update: (projectId: string, environmentId: string, body: UpdateEnvironmentInput) =>
    apiClient.patch<string>(`/projects/${projectId}/environments/${environmentId}`, body),

  delete: (projectId: string, environmentId: string) =>
    apiClient.delete(`/projects/${projectId}/environments/${environmentId}`),

  clone: (projectId: string, environmentId: string, body: CloneEnvironmentInput) =>
    apiClient.post<string>(`/projects/${projectId}/environments/${environmentId}/clone`, body),

  getEnvironmentVariables: (projectId: string, environmentId: string) =>
    apiClient.get<string>(`/projects/${projectId}/environments/${environmentId}/env`),

  setEnvironmentVariables: (projectId: string, environmentId: string, envFile: string) =>
    apiClient.post(`/projects/${projectId}/environments/${environmentId}/env`, { envFile }),
};
