import { apiClient } from './client'
import {
  CreateProjectInput,
  GetProjectsParams,
  PagedResult,
  ProjectDto,
  ProjectDashboardDto,
  UpdateProjectInput,
} from './types'

export const projectsApi = {
  getAll: (params?: GetProjectsParams) =>
    apiClient.get<PagedResult<ProjectDto>>('/projects', params),

  getDashboard: (params?: GetProjectsParams) =>
    apiClient.get<PagedResult<ProjectDashboardDto>>('/projects/dashboard', params),

  getById: (id: string) =>
    apiClient.get<ProjectDto>(`/projects/${id}`),

  create: (body: CreateProjectInput) =>
    apiClient.post<string>('/projects', body),

  update: (id: string, body: UpdateProjectInput) =>
    apiClient.patch<string>(`/projects/${id}`, body),

  delete: (id: string) =>
    apiClient.delete(`/projects/${id}`),

  getEnvironmentVariables: (projectId: string) =>
    apiClient.get<string>(`/projects/${projectId}/env`),

  setEnvironmentVariables: (projectId: string, envFile: string) =>
    apiClient.post(`/projects/${projectId}/env`, { envFile }),
}
