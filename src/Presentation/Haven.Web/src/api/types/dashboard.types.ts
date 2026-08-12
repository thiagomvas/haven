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
