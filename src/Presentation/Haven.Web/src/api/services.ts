import { apiClient } from './client'
import {
  CreateServiceInput,
  ServiceDto,
  ServiceDashboardDto,
  DockerConfig,
  DockerfileConfig,
} from './types'

export interface UpdateServiceInput {
  name?: string
  type?: string
  exposureMode?: string
  dockerConfig?: DockerConfig
  dockerfileConfig?: DockerfileConfig
}

export const servicesApi = {
  getByEnvironmentId: (projectId: string, environmentId: string) =>
    apiClient.get<ServiceDto[]>(
      `/projects/${projectId}/environments/${environmentId}/services`,
    ),

  getById: (
    projectId: string,
    environmentId: string,
    serviceId: string,
  ) =>
    apiClient.get<ServiceDto>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}`,
    ),

  getDashboard: (
    projectId: string,
    environmentId: string,
    serviceId: string,
  ) =>
    apiClient.get<ServiceDashboardDto>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/dashboard`,
    ),

  create: (
    projectId: string,
    environmentId: string,
    body: CreateServiceInput,
  ) =>
    apiClient.post<string>(
      `/projects/${projectId}/environments/${environmentId}/services`,
      body,
    ),

  update: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    body: UpdateServiceInput,
  ) =>
    apiClient.patch<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}`,
      body,
    ),

  deploy: (
    projectId: string,
    environmentId: string,
    serviceId: string,
  ) =>
    apiClient.post<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/deploy`,
      null,
    ),

  restart: (
    projectId: string,
    environmentId: string,
    serviceId: string,
  ) =>
    apiClient.post<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/restart`,
      null,
    ),

  stop: (
    projectId: string,
    environmentId: string,
    serviceId: string,
  ) =>
    apiClient.post<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/stop`,
      null,
    ),

  getEnvironmentVariables: (
    projectId: string,
    environmentId: string,
    serviceId: string,
  ) =>
    apiClient.get<string>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/env`,
    ),

  setEnvironmentVariables: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    envFile: string,
  ) =>
    apiClient.post(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/env`,
      { envFile },
    ),

  regenerateToken: (
    projectId: string,
    environmentId: string,
    serviceId: string,
  ) =>
    apiClient.post<string>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/tokens/regenerate`,
      null,
    ),
}
