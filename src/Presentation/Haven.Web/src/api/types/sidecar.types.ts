import { ServiceHealth, ServiceStatus } from './service.types';

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
}
