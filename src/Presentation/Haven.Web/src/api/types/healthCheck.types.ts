import { ServiceHealth } from './service.types';

export type HealthCheckKind = 'Http' | 'Container' | 'Bash' | 'Tcp';

export type HealthCheckFailureReason =
  | 'None'
  | 'Timeout'
  | 'ConnectionRefused'
  | 'DnsFailure'
  | 'TlsError'
  | 'UnexpectedStatusCode'
  | 'UnexpectedExitCode'
  | 'ContainerNotFound'
  | 'ContainerNotRunning'
  | 'ContainerUnhealthy'
  | 'InvalidConfig'
  | 'ProbeUnavailable'
  | 'Error';

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
  lastRunReason: HealthCheckFailureReason;
  lastRunMessage?: string;
  lastRunDurationMs?: number;
  retries: number;
  failureThreshold: number;
  successThreshold: number;
  consecutiveFailures: number;
}

export interface HealthCheckResultDto {
  id: string;
  ranAt: string;
  status: ServiceHealth;
  reason: HealthCheckFailureReason;
  message?: string;
  durationMs: number;
  httpStatusCode?: number;
  exitCode?: number;
  output?: string;
  attempts: number;
}

/** Direct: Haven makes the request itself. Probe: a short-lived container on the service's network makes it. */
export type HttpHealthCheckMode = 'Direct' | 'Probe';

export interface HttpHealthCheckConfig {
  url: string;
  method: string;
  expectedStatusCodes: number[];
  timeoutSeconds: number;
  mode: HttpHealthCheckMode;
}

export interface BashHealthCheckConfig {
  command: string;
  expectedExitCode: number;
  timeoutSeconds: number;
}

export interface TcpHealthCheckConfig {
  host: string;
  port: number;
  timeoutSeconds: number;
}

export interface CreateHealthCheckInput {
  name: string;
  kind: HealthCheckKind;
  enabled: boolean;
  cronExpression?: string;
  config: string;
  retries: number;
  failureThreshold: number;
  successThreshold: number;
}

export interface UpdateHealthCheckInput {
  name?: string;
  enabled?: boolean;
  cronExpression?: string;
  clearCronExpression?: boolean;
  config?: string;
  retries?: number;
  failureThreshold?: number;
  successThreshold?: number;
}

export interface TestHealthCheckInput {
  kind: HealthCheckKind;
  config: string;
}

/** Pushed over the service status hub when a service turns unhealthy or recovers. */
export interface ServiceHealthChangedData {
  serviceId: string;
  serviceName: string;
  health: ServiceHealth;
  issue?: {
    checkName: string;
    reason: HealthCheckFailureReason;
    message?: string;
  };
}
