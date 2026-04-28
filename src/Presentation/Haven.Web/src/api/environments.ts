import { apiClient } from './client'
import {
  CreateEnvironmentInput,
  EnvironmentDto,
  UpdateEnvironmentInput,
} from './types'

export const environmentsApi = {
  getByProjectId: (projectId: string) =>
    apiClient.get<EnvironmentDto[]>(
      `/projects/${projectId}/environments`,
    ),

  create: (projectId: string, body: CreateEnvironmentInput) =>
    apiClient.post<string>(
      `/projects/${projectId}/environments`,
      body,
    ),

  update: (
    projectId: string,
    environmentId: string,
    body: UpdateEnvironmentInput,
  ) =>
    apiClient.patch<string>(
      `/projects/${projectId}/environments/${environmentId}`,
      body,
    ),

  delete: (projectId: string, environmentId: string) =>
    apiClient.delete(
      `/projects/${projectId}/environments/${environmentId}`,
    ),
}
