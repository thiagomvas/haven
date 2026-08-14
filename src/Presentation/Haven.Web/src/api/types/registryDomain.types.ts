export type { ServiceRegistryDomainDto } from './service.types';

export interface AddDomainInput {
  hostname: string;
  containerPort: number;
}

export interface UpdateDomainInput {
  hostname?: string;
  containerPort?: number;
}
