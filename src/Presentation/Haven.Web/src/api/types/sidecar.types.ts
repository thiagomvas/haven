import { RestartPolicy, ServiceHealth, ServiceStatus } from './service.types';

export type SidecarKind = 'Traefik' | 'Whoami' | 'Custom';

export interface SidecarDto {
  id: string;
  name: string;
  alias?: string;
  kind: SidecarKind;
  status: ServiceStatus;
  health: ServiceHealth;
  enabled: boolean;
  createdAt: string;
  updatedAt: string;
  lastDeployedAt?: string;
  image?: string;
  ports: string[];
  commandArgs: string[];
  restartPolicy?: RestartPolicy;
  isAcmeConfigured?: boolean;
}

export interface UpdateSidecarDockerConfig {
  image: string;
  ports: string[];
  commandArgs: string[];
  restartPolicy: RestartPolicy;
}

export interface UpdateSidecarPayload {
  dockerConfig?: UpdateSidecarDockerConfig;
}
