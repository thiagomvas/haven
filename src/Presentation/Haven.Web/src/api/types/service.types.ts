import { EnvironmentVariableDto } from './environmentVariables.types';
import { FeatureFlagDto } from './featureflags.types';

export type ServiceType = 'DockerImage' | 'Dockerfile' | 'Compose' | 'Process';
/** Computed server-side from desired state, observed runtime state, health and any in-flight deployment. */
export type ServiceDisplayStatus =
  | 'NotDeployed'
  | 'Queued'
  | 'Deploying'
  | 'Starting'
  | 'Running'
  | 'Degraded'
  | 'Stopped'
  | 'Crashed'
  | 'Unknown';
export type DesiredState = 'Running' | 'Stopped';
export type ServiceRuntimeState =
  | 'NotDeployed'
  | 'Running'
  | 'Restarting'
  | 'Stopped'
  | 'Crashed'
  | 'Unknown';
export type HealthStatus =
  | 'Running'
  | 'Healthy'
  | 'Degraded'
  | 'Stopped'
  | 'Died'
  | 'Unknown'
  | 'Deploying'
  | 'DeploymentPending';
export type ServiceHealth = 'Unknown' | 'Healthy' | 'Unhealthy';
export type RestartPolicy = 'No' | 'Always' | 'UnlessStopped' | 'OnFailure';
export interface ServiceStatisticsDto {
  total: number;
  running: number;
  /** Includes crashed. */
  stopped: number;
  /** Subset of `stopped` that stopped abnormally. */
  crashed: number;
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
  desiredState: DesiredState;
  runtimeState: ServiceRuntimeState;
  status: ServiceDisplayStatus;
  health: ServiceHealth;
  createdAt: string;
  updatedAt: string;
  lastDeployedAt?: string;
  sourceConfig?: ServiceSourceConfig | DockerConfig;
  repositoryBinding?: RepositoryBindingDto;
  webhookUrl: string;
  environmentVariables: EnvironmentVariableDto[];
  featureFlags: FeatureFlagDto[];
  registry?: ServiceRegistryEntryDto;
  computedOutputs: ComputedOutputDto[];
}
export interface ComputedOutputDto {
  key: string;
  label: string;
  isSecret: boolean;
  isAvailable: boolean;
  unavailableReason?: string;
  preview?: string;
}
export interface ComputedOutputValueDto {
  key: string;
  value: string;
}
export interface DockerConfig {
  image: string;
  ports: string[];
  commandArgs: string[];
  restartPolicy: RestartPolicy;
  /** Hard memory limit in bytes. 0 means no limit. */
  memoryLimit?: number;
  /** Total memory + swap limit in bytes. 0 means Docker's default, -1 means unlimited swap. */
  memorySwap?: number;
  /** Soft memory limit in bytes. 0 means no reservation. */
  memoryReservation?: number;
  /** CPU limit in billionths of a core (1_000_000_000 = 1 core). 0 means no limit. */
  nanoCpus?: number;
}
export interface DockerfileConfig {
  source: DockerfileSource;
  repository?: string;
  branch?: string;
  filePath?: string;
  buildContext?: string;
  gitCredentialId?: string;
  content?: string;
  buildArgs?: Record<string, string>;
  ports?: string[];
  commandArgs?: string[];
  restartPolicy: RestartPolicy;
  /** Hard memory limit in bytes. 0 means no limit. */
  memoryLimit?: number;
  /** Total memory + swap limit in bytes. 0 means Docker's default, -1 means unlimited swap. */
  memorySwap?: number;
  /** Soft memory limit in bytes. 0 means no reservation. */
  memoryReservation?: number;
  /** CPU limit in billionths of a core (1_000_000_000 = 1 core). 0 means no limit. */
  nanoCpus?: number;
}
export interface ServiceSourceConfig {
  $type?: string;
}
export interface RepositoryBindingDto {
  gitCredentialId?: string;
  repository?: string;
  branch?: string;
  filePath?: string;
}

export interface ServiceDto {
  id: string;
  environmentId: string;
  name: string;
  alias?: string;
  type: ServiceType;
  exposureMode: ExposureMode;
  desiredState: DesiredState;
  runtimeState: ServiceRuntimeState;
  status: ServiceDisplayStatus;
  health: ServiceHealth;
  sourceConfig?: ServiceSourceConfig | DockerConfig;
  repositoryBinding?: RepositoryBindingDto;
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

export interface BulkServiceActionResult {
  serviceId: string;
  success: boolean;
  errorMessage?: string;
}
export interface BulkServiceActionResponse {
  results: BulkServiceActionResult[];
  succeededCount: number;
  failedCount: number;
}

export type DockerfileSource = 'Git' | 'Raw';
export type ExposureMode = 'None' | 'Internal' | 'External' | 'Custom';
export interface PortMappingDto {
  hostPort?: number;
  containerPort: number;
  ipAddress?: string;
}
export type TlsMode = 'None' | 'Acme' | 'Custom';
export interface ServiceRegistryDomainDto {
  id: string;
  hostname: string;
  containerPort: number;
  tlsMode: TlsMode;
  internalBasePath?: string;
  hasCertificate: boolean;
  certificateId?: string;
  certificateName?: string;
  createdAt: string;
  updatedAt: string;
}
export interface ServiceRegistryEntryDto {
  id: string;
  serviceId?: string;
  serviceName?: string;
  sidecarId?: string;
  sidecarName?: string;
  environmentId?: string;
  environmentName?: string;
  projectId?: string;
  projectName?: string;
  containerName?: string;
  ipAddress?: string;
  ports: PortMappingDto[];
  runtimeState: ServiceRuntimeState;
  health: ServiceHealth;
  stateChangedAt: string;
  registeredAt: string;
  updatedAt: string;
  startedAt?: string;
  serviceType: ServiceType;
  exposureMode: ExposureMode;
  domains: ServiceRegistryDomainDto[];
}
export type ServiceRegistrySortBy = 'name' | 'project' | 'status' | 'type' | 'exposure' | 'uptime';
export type RegistryStatusFilter = 'Running' | 'Stopped' | 'Starting' | 'Unknown';
export interface GetServiceRegistryParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  status?: RegistryStatusFilter;
  exposure?: ExposureMode;
  type?: ServiceType;
  projectId?: string;
  environmentId?: string;
  sortBy?: ServiceRegistrySortBy;
  sortDescending?: boolean;
}
export interface ServiceRegistrySummaryDto {
  total: number;
  running: number;
  stopped: number;
  starting: number;
  unknown: number;
  domainCount: number;
  exposedExternally: number;
}
export type ServiceRegistryDomainSortBy = 'hostname' | 'service' | 'tls' | 'created';
export interface GetServiceRegistryDomainsParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  sortBy?: ServiceRegistryDomainSortBy;
  sortDescending?: boolean;
}
export interface ServiceRegistryDomainListItemDto {
  id: string;
  hostname: string;
  containerPort: number;
  tlsMode: TlsMode;
  hasCertificate: boolean;
  certificateName?: string;
  createdAt: string;
  containerName?: string;
  runtimeState: ServiceRuntimeState;
  health: ServiceHealth;
  serviceId?: string;
  serviceName?: string;
  sidecarName?: string;
  environmentName?: string;
  projectName?: string;
}
export interface DeployServiceInput {
  projectId: string;
  environmentId: string;
  serviceId: string;
}
