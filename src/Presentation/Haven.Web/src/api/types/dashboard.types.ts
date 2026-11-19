import type { DeploymentIdentifierType, DeploymentStatus } from './deployment.types';
import { HealthStatus, ServiceStatisticsDto } from './service.types';

export interface AttentionEnvironmentDto {
  projectId: string;
  projectName: string;
  environmentId: string;
  environmentName: string;
  status: HealthStatus;
  affectedServiceCount: number;
}

export interface LastDeploymentDto {
  serviceName: string;
  projectName: string;
  environmentName: string;
  deployedAt: string;
}

export interface DashboardOverviewDto {
  totalProjects: number;
  totalEnvironments: number;
  serviceStatistics: ServiceStatisticsDto;
  attentionEnvironment?: AttentionEnvironmentDto;
  deploymentsLast24h: number;
  lastDeployment?: LastDeploymentDto;
}

/*
 * ---- Dashboard monitoring panels ----
 * Attention and deployments are live endpoints. The shapes below them (host, jobs) are a proposed contract served from
 * `mocks/dashboardInsights.ts` until the matching endpoints exist; keep the mock and
 * these types in sync, then swap `useDashboardInsights` to call the API.
 */

export type HostMetricKey = 'cpu' | 'memory' | 'disk' | 'network';

export interface HostMetricDto {
  key: HostMetricKey;
  /** Current reading, in `unit`. */
  value: number;
  unit: string;
  /** Capacity in the same unit (cores, GB, ...) when the metric has one. */
  capacity?: number;
  /** Utilisation 0-100 used for thresholds; omitted for metrics without a ceiling. */
  percent?: number;
  /** Evenly spaced readings covering the last 24 hours, oldest first. */
  series: number[];
}

export interface HostSnapshotDto {
  hostname: string;
  cores: number;
  uptimeSeconds: number;
  dockerVersion: string;
  runningContainers: number;
  totalContainers: number;
  metrics: HostMetricDto[];
}

export type AttentionReason = 'Unhealthy' | 'Crashed' | 'DeploymentFailed';

/** A service on the "needs attention" list. Served by `GET /dashboard/attention`. */
export interface AttentionServiceDto {
  serviceId: string;
  serviceName: string;
  projectId: string;
  projectName: string;
  environmentId: string;
  environmentName: string;
  reason: AttentionReason;
  /** When the problem started, when known. Unhealthy services carry no timestamp. */
  since?: string | null;
}

/** A deployment with its owning service, environment and project. Served by `GET /dashboard/deployments`. */
export interface RecentDeploymentDto {
  id: string;
  serviceId: string;
  serviceName: string;
  projectId: string;
  projectName: string;
  environmentId: string;
  environmentName: string;
  status: DeploymentStatus;
  triggeredBy?: string | null;
  identifierType?: DeploymentIdentifierType;
  identifier?: string | null;
  commitMessage?: string | null;
  startedAt: string;
  finishedAt?: string | null;
}

export type ScheduledJobKind = 'backup' | 'cleanup' | 'sync' | 'certificates';

export interface ScheduledJobDto {
  key: string;
  name: string;
  kind: ScheduledJobKind;
  lastRunTime?: string;
  lastStatus?: 'Succeeded' | 'Failed';
  nextRunTime?: string;
  /** Result detail, e.g. "3.2 GB written". */
  detail?: string;
}

export type SystemCheckState = 'ok' | 'warn' | 'fail';

export interface EnvironmentInsightDto {
  environmentId: string;
  availability24hPercent: number;
  cpuPercent: number;
  memoryMb: number;
}

export interface DashboardInsightsDto {
  host: HostSnapshotDto;
  jobs: ScheduledJobDto[];
}
