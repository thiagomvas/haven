import { EnvironmentVariableDto } from './environmentVariables.types';
import { FeatureFlagDto } from './featureflags.types';

export type ServiceType = 'DockerImage' | 'Dockerfile' | 'Compose' | 'Process';
export type ServiceStatus = 'Running' | 'Stopped' | 'Degraded' | 'DeploymentPending' | 'Unknown';
export type HealthStatus = 'Running' | 'Healthy' | 'Degraded' | 'Stopped' | 'Died' | 'Unknown';
export type ServiceHealth = 'Healthy' | 'Unhealthy' | 'Unknown';
export type RestartPolicy = 'No' | 'Always' | 'UnlessStopped' | 'OnFailure';
export interface ServiceStatisticsDto {
  total: number;
  running: number;
  stopped: number;
  degraded: number;
  deploymentPending: number;
  deploying: number;
  unknown: number;
}
export interface ServiceDashboardDto {
  id: string;
  environmentId: string;
  name: string;
  alias?: string;
  type: ServiceType;
  exposureMode: ExposureMode;
  status: ServiceStatus;
  health: ServiceHealth;
  createdAt: string;
  updatedAt: string;
  lastDeployedAt?: string;
  sourceConfig?: ServiceSourceConfig | DockerConfig;
  webhookUrl: string;
  environmentVariables: EnvironmentVariableDto[];
  featureFlags: FeatureFlagDto[];
  registry?: ServiceRegistryEntryDto;
}
export interface DockerConfig {
  image: string;
  ports: string[];
  restartPolicy: RestartPolicy;
}
export interface DockerfileConfig {
  source: DockerfileSource;
  repository?: string;
  branch?: string;
  filePath?: string;
  gitCredentialId?: string;
  content?: string;
}
export interface ServiceSourceConfig {
  $type?: string;
}
export interface ServiceDto {
  id: string;
  environmentId: string;
  name: string;
  alias?: string;
  type: ServiceType;
  exposureMode: ExposureMode;
  status: ServiceStatus;
  health: ServiceHealth;
  sourceConfig?: ServiceSourceConfig | DockerConfig;
  createdAt: string;
  updatedAt: string;
  webhookUrl?: string;
}
export interface CreateServiceInput {
  name: string;
  alias?: string;
  type: ServiceType;
  exposureMode: ExposureMode;
  dockerConfig?: DockerConfig;
  dockerfileConfig?: DockerfileConfig;
}

export type DockerfileSource = 'Git' | 'Raw';
export type ExposureMode = 'None' | 'Internal' | 'External' | 'Custom';
export interface PortMappingDto {
  hostPort?: number;
  containerPort: number;
  ipAddress?: string;
}
export interface ServiceRegistryEntryDto {
  serviceId: string;
  containerName?: string;
  ipAddress?: string;
  ports: PortMappingDto[];
  status: ServiceStatus;
  registeredAt: string;
  updatedAt: string;
  startedAt?: string;
  serviceType: ServiceType;
  exposureMode: ExposureMode;
}
export interface GetServiceRegistryParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
}
export interface DeployServiceInput {
  projectId: string;
  environmentId: string;
  serviceId: string;
}
