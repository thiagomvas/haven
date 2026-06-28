import { apiClient } from './client';
import { PagedResult } from './types';
import { UpdateProjectInput } from './types/project.types.ts';
import { CreateProjectInput } from './types/project.types.ts';
import { GetProjectsParams } from './types/project.types.ts';
import { ProjectDashboardDto } from './types/project.types.ts';
import { ProjectDto } from './types/project.types.ts';

export interface CloneProjectInput {
  newName: string;
  newAlias?: string;
}

export const projectsApi = {
  getAll: (params?: GetProjectsParams) =>
    apiClient.get<PagedResult<ProjectDto>>('/projects', params),

  getDashboard: (params?: GetProjectsParams) =>
    apiClient.get<PagedResult<ProjectDashboardDto>>('/projects/dashboard', params),

  getDashboardById: (id: string) => apiClient.get<ProjectDashboardDto>(`/projects/${id}/dashboard`),

  getById: (id: string) => apiClient.get<ProjectDto>(`/projects/${id}`),

  create: (body: CreateProjectInput) => apiClient.post<string>('/projects', body),

  update: (id: string, body: UpdateProjectInput) =>
    apiClient.patch<string>(`/projects/${id}`, body),

  delete: (id: string) => apiClient.delete(`/projects/${id}`),

  clone: (id: string, body: CloneProjectInput) =>
    apiClient.post<string>(`/projects/${id}/clone`, body),

  getEnvironmentVariables: (projectId: string) =>
    apiClient.get<string>(`/projects/${projectId}/env`),

  setEnvironmentVariables: (projectId: string, envFile: string) =>
    apiClient.post(`/projects/${projectId}/env`, { envFile }),
};
