import { apiClient } from './client';
import { DeploymentDto } from './types/deployment.types';
import { CreateServiceInput } from './types/service.types';
import { ServiceDto } from './types/service.types';
import { DockerfileConfig } from './types/service.types';
import { DockerConfig } from './types/service.types';
import { ServiceDashboardDto } from './types/service.types';

export interface CloneServiceInput {
  newName: string;
  newAlias?: string;
  targetProjectId?: string;
  targetEnvironmentId?: string;
}

export interface UpdateServiceInput {
  name?: string;
  type?: string;
  exposureMode?: string;
  dockerConfig?: DockerConfig;
  dockerfileConfig?: DockerfileConfig;
}

export interface ServiceLocationDto {
  serviceId: string;
  environmentId: string;
  projectId: string;
}

export const servicesApi = {
  resolve: (serviceId: string) => apiClient.get<ServiceLocationDto>(`/services/${serviceId}`),

  getByEnvironmentId: (projectId: string, environmentId: string) =>
    apiClient.get<ServiceDto[]>(`/projects/${projectId}/environments/${environmentId}/services`),

  getById: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<ServiceDto>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}`
    ),

  getDashboard: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<ServiceDashboardDto>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/dashboard`
    ),

  create: (projectId: string, environmentId: string, body: CreateServiceInput) =>
    apiClient.post<string>(`/projects/${projectId}/environments/${environmentId}/services`, body),

  update: (projectId: string, environmentId: string, serviceId: string, body: UpdateServiceInput) =>
    apiClient.patch<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}`,
      body
    ),

  delete: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.delete<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}`
    ),

  deploy: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.post<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/deploy`,
      null
    ),

  restart: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.post<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/restart`,
      null
    ),

  stop: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.post<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/stop`,
      null
    ),

  getEnvironmentVariables: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<string>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/env`
    ),

  setEnvironmentVariables: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    envFile: string
  ) =>
    apiClient.post(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/env`,
      { envFile }
    ),

  regenerateToken: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.post<string>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/tokens/regenerate`,
      null
    ),

  clone: (projectId: string, environmentId: string, serviceId: string, body: CloneServiceInput) =>
    apiClient.post<string>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/clone`,
      body
    ),

  getDeployments: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<DeploymentDto[]>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/deployments`
    ),

  getDeploymentLogs: (deploymentId: string) =>
    apiClient.get<string[]>(`/deployments/${deploymentId}/logs`),

  cancelDeployment: (deploymentId: string) =>
    apiClient.post<void>(`/deployments/${deploymentId}/cancel`, null),

  getManifest: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<string>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/manifest`
    ),

  applyManifest: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    manifestYaml: string
  ) =>
    apiClient.put<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/manifest`,
      { manifestYaml }
    ),
};
