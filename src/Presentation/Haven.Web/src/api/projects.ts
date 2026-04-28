import { apiClient } from './client'
import {
  CreateProjectInput,
  GetProjectsParams,
  PagedResult,
  ProjectDto,
  UpdateProjectInput,
} from './types'

export const projectsApi = {
  getAll: (params?: GetProjectsParams) =>
    apiClient.get<PagedResult<ProjectDto>>('/projects', params),

  getById: (id: string) =>
    apiClient.get<ProjectDto>(`/projects/${id}`),

  create: (body: CreateProjectInput) =>
    apiClient.post<string>('/projects', body),

  update: (id: string, body: UpdateProjectInput) =>
    apiClient.patch<string>(`/projects/${id}`, body),

  delete: (id: string) =>
    apiClient.delete(`/projects/${id}`),
}
