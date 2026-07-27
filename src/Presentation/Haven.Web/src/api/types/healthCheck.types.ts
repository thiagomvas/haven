import { ServiceHealth } from './service.types';

export type HealthCheckKind = 'Http' | 'Container' | 'Bash';

export interface HealthCheckDto {
  id: string;
  serviceId: string;
  name: string;
  enabled: boolean;
  cronExpression?: string;
  config: string;
  kind: HealthCheckKind;
  lastRunAt?: string;
  lastRunStatus: ServiceHealth;
}

export interface HttpHealthCheckConfig {
  url: string;
  method: string;
  expectedStatusCodes: number[];
  timeoutSeconds: number;
}

export interface BashHealthCheckConfig {
  command: string;
  expectedExitCode: number;
  timeoutSeconds: number;
}

export interface CreateHealthCheckInput {
  name: string;
  kind: HealthCheckKind;
  enabled: boolean;
  cronExpression?: string;
  config: string;
}

export interface UpdateHealthCheckInput {
  name?: string;
  enabled?: boolean;
  cronExpression?: string;
  clearCronExpression?: boolean;
  config?: string;
}
