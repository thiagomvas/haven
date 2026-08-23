export type { ServiceRegistryDomainDto } from './service.types';

export interface AddDomainInput {
  hostname: string;
  containerPort: number;
  enableTls?: boolean;
}

export interface UpdateDomainInput {
  hostname?: string;
  containerPort?: number;
  enableTls?: boolean;
}
