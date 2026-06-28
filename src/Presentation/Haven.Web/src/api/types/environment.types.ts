import { EnvironmentVariableDto } from './environmentVariables.types';
import {
  ServiceStatisticsDto,
  HealthStatus,
  ServiceDashboardDto,
  ServiceStatus,
} from './service.types';

export interface EnvironmentDto {
  id: string;
  projectId: string;
  name: string;
  alias?: string;
  description?: string;
  networkName: string;
  serviceCount: number;
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
