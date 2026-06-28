import { EnvironmentDashboardDto } from "./environment.types";
import { EnvironmentVariableDto } from "./environmentVariables.types";
import { ServiceStatisticsDto } from "./service.types";

export interface ProjectDto {
  id: string;
  name: string;
  alias?: string;
  description?: string;
  environmentCount: number;
  serviceCount: number;
}export interface ProjectDashboardDto {
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

