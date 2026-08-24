export type { ServiceRegistryDomainDto, TlsMode } from './service.types';
import { TlsMode } from './service.types';

export interface AddDomainInput {
  hostname: string;
  containerPort: number;
  tlsMode?: TlsMode;
}

export interface UpdateDomainInput {
  hostname?: string;
  containerPort?: number;
  tlsMode?: TlsMode;
}

export interface AttachDomainCertificateInput {
  certificateId: string;
}

export interface AttachDomainCertificateResult {
  certificateId: string;
  notAfter: string;
  warnings: string[];
}

export interface DomainCertificateStatusDto {
  tlsMode: TlsMode;
  sourceOfTruth: string;
  notBefore?: string;
  notAfter?: string;
  subjectCommonName?: string;
  isExpired: boolean;
  daysUntilExpiry?: number;
  hostnameMismatch: boolean;
  traefikReachable: boolean;
  routerStatus?: string;
  errors: string[];
}
