import { apiClient } from './client'
import {
  CreateServiceInput,
  ServiceDto,
} from './types'

export interface UpdateServiceInput {
  name?: string
  type?: string
  exposureMode?: string
  dockerConfig?: DockerConfig
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
}
