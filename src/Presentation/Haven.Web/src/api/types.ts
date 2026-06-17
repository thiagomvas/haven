/* Search */
export interface FuzzySearchResult {
  entityType: string;
  id: string;
  label: string;
  similarity: number;
  metadata?: Record<string, string>;
}

/* Enums */
export type ServiceType = 'DockerImage' | 'Dockerfile' | 'Compose' | 'Process';
export type DockerfileSource = 'Git' | 'Raw';
export type ServiceStatus = 'Running' | 'Stopped' | 'Degraded' | 'DeploymentPending' | 'Unknown';
export type HealthStatus = 'Running' | 'Healthy' | 'Degraded' | 'Stopped' | 'Died' | 'Unknown';
export type ExposureMode = 'None' | 'Internal' | 'External';
export type RestartPolicy = 'No' | 'Always' | 'UnlessStopped' | 'OnFailure';
export type NetworkType = 'ProjectEnvironment' | 'Shared' | 'External';
export type GitProviderType = 'Generic' | 'GitHub' | 'GitLab' | 'Bitbucket' | 'Gitea';
export type GitAuthMethod = 'Token' | 'Ssh';
export type DeploymentStatus = 'InProgress' | 'Succeeded' | 'Failed';

/* Response Wrappers */
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

/* DTOs */
export interface ProjectDto {
  id: string;
  name: string;
  alias?: string;
  description?: string;
  environmentCount: number;
  serviceCount: number;
}

export interface EnvironmentDto {
  id: string;
  projectId: string;
  name: string;
  alias?: string;
  description?: string;
  networkName: string;
  serviceCount: number;
}

export interface EnvironmentVariableDto {
  key: string;
  value: string;
  scope: string;
}

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
  createdAt: string;
  updatedAt: string;
  lastDeployedAt?: string;
  sourceConfig?: ServiceSourceConfig | DockerConfig;
  webhookUrl: string;
  environmentVariables: EnvironmentVariableDto[];
  featureFlags: FeatureFlagDto[];
}

export interface EnvironmentDashboardDto {
  id: string;
  name: string;
  alias?: string;
  description?: string;
  projectId: string;
  projectName: string;
  networkName: string;
  serviceStatistics: ServiceStatisticsDto;
  status: HealthStatus;
  totalEnvVars: number;
  environmentVariables: EnvironmentVariableDto[];
  services: ServiceDashboardDto[];
  serviceStatusMap: Record<string, ServiceStatus>;
}

export interface ProjectDashboardDto {
  id: string;
  name: string;
  alias?: string;
  description?: string;
  environments: EnvironmentDashboardDto[];
  serviceStatistics: ServiceStatisticsDto;
  lastDeployedAt?: string;
  totalEnvVars: number;
  environmentVariables: EnvironmentVariableDto[];
}

export interface DockerConfig {
  image: string;
  ports: string[];
  volumes: string[];
  environmentVariables: string[];
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
  sourceConfig?: ServiceSourceConfig | DockerConfig;
  createdAt: string;
  updatedAt: string;
  webhookUrl?: string;
}

export interface DeploymentDto {
  id: string;
  serviceId: string;
  startedAt: string;
  finishedAt?: string;
  status: DeploymentStatus;
  triggeredBy?: string;
}

export interface EventDto {
  id: string;
  eventType: string;
  message: string;
  payload?: string;
  triggeredAt: string;
}

export interface DomainEventTypeDto {
  name: string;
  i18NKey: string;
}

export interface NetworkDto {
  name: string;
  type: NetworkType;
  metadata?: Record<string, unknown>;
}

/* Request Types */
export interface GetProjectsParams {
  pageNumber?: number;
  pageSize?: number;
}

export interface CreateProjectInput {
  name: string;
  alias?: string;
  description?: string;
}

export interface UpdateProjectInput {
  name?: string;
  alias?: string;
  description?: string | null;
}

export interface CreateEnvironmentInput {
  name: string;
  alias?: string;
  description?: string;
}

export interface UpdateEnvironmentInput {
  name?: string;
  alias?: string;
  description?: string | null;
}

export interface CreateServiceInput {
  name: string;
  alias?: string;
  type: ServiceType;
  exposureMode: ExposureMode;
  dockerConfig?: DockerConfig;
  dockerfileConfig?: DockerfileConfig;
}

export interface DeployServiceInput {
  projectId: string;
  environmentId: string;
  serviceId: string;
}

export interface GetEventsParams {
  pageNumber?: number;
  pageSize?: number;
  eventType?: string;
  from?: string;
  to?: string;
  ascending?: boolean;
}

/* Feature Flags */
export type FeatureFlagType = 'EnvironmentVariable';
export type FeatureFlagValueType = 'String' | 'Bool' | 'Number';

export interface FeatureFlagDto {
  id: string;
  serviceId: string;
  name: string;
  type: FeatureFlagType;
  key?: string;
  description?: string;
  value: string;
  valueType: FeatureFlagValueType;
}

export interface CreateFeatureFlagInput {
  name: string;
  type: FeatureFlagType;
  key?: string;
  description?: string;
  value: string;
  valueType: FeatureFlagValueType;
}

export interface UpdateFeatureFlagInput {
  name?: string;
  type?: FeatureFlagType;
  key?: string;
  description?: string;
  value?: string;
  valueType?: FeatureFlagValueType;
}

/* Git Credentials */
export interface GitCredentialDto {
  id: string;
  providerType: GitProviderType;
  hostUrl?: string;
  authMethod: GitAuthMethod;
  displayName: string;
  isActive: boolean;
  lastValidatedAt: string;
}

export interface CreateGitCredentialInput {
  providerType: GitProviderType;
  hostUrl?: string;
  authMethod: GitAuthMethod;
  primaryCredential: string;
  secondaryCredential?: string;
  webhookSecret?: string;
  displayName: string;
}

export interface GetGitCredentialsParams {
  pageNumber?: number;
  pageSize?: number;
}

/* Notification Rules */
export type NotificationScope = 'Global' | 'Project' | 'Environment' | 'Service';

export interface NotificationRuleContext {
  scope: NotificationScope;
  scopeId: string;
}

export interface NotificationRuleSummaryItemDto {
  name: string;
  i18NKey: string;
  ruleCount: number;
  isOverridden: boolean;
  globalRuleCount: number;
}

export interface NotificationRuleEventConfigDto {
  eventType: string;
  channelIds: string[];
}

export interface SetNotificationRulesInput {
  channelIds: string[];
}

/* Notification Channels */
export type NotificationChannel = 'Webhook' | 'Discord';

export interface WebhookNotificationConfig {
  url: string;
  headers: Record<string, string>;
}

export interface DiscordNotificationConfig {
  webhookUrl: string;
  embed: boolean;
}

export interface NotificationChannelConfigDto {
  id: string;
  name: string;
  channel: NotificationChannel;
  config: string;
  enabled: boolean;
  rulesCount: number;
}

export interface CreateNotificationChannelConfigInput {
  name: string;
  channel: NotificationChannel;
  configJson: string;
  enabled: boolean;
}

export interface UpdateNotificationChannelConfigInput {
  name: string;
  configJson: string;
  enabled: boolean;
}

export interface GetNotificationChannelConfigsParams {
  pageNumber?: number;
  pageSize?: number;
}

export type NotificationDeliveryStatus = 'Pending' | 'Delivered' | 'Failed';

export interface NotificationAttemptDto {
  id: string;
  eventType: string;
  status: NotificationDeliveryStatus;
  errorMessage: string | null;
  attemptedAt: string | null;
}

export interface GetNotificationAttemptsParams {
  pageNumber?: number;
  pageSize?: number;
}

/* Users */
export interface UserDto {
  id: string;
  name: string;
  email: string;
  isAdmin: boolean;
  requirePasswordChange: boolean;
}

export interface CreateUserInput {
  name: string;
  email: string;
  temporaryPassword: string;
  isAdmin: boolean;
  permissions: string[];
}

/* Build Info */
export interface DatabaseBuildInfoDto {
  provider: string;
  version: string;
  path: string;
}

export interface DockerEngineBuildInfoDto {
  isConnected: boolean;
  version: string | null;
}

export interface BuildInfoDto {
  version: string;
  commitSha: string;
  buildDate: string;
  buildSystem: string;
  dotNetVersion: string;
  database: DatabaseBuildInfoDto;
  dockerEngine: DockerEngineBuildInfoDto;
}
