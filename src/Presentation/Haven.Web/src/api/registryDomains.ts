import { apiClient } from './client';
import {
  AddDomainInput,
  DomainCertificateStatusDto,
  UpdateDomainInput,
  UploadDomainCertificateInput,
  UploadDomainCertificateResult,
} from './types/registryDomain.types';
import { ServiceRegistryEntryDto } from './types/service.types';

const base = (serviceId: string) => `/service-registry/${serviceId}/domains`;

export const registryDomainsApi = {
  getEntry: (serviceId: string) =>
    apiClient.get<ServiceRegistryEntryDto | null>(`/service-registry/services/${serviceId}`),

  add: (serviceId: string, body: AddDomainInput) => apiClient.post<string>(base(serviceId), body),

  update: (serviceId: string, domainId: string, body: UpdateDomainInput) =>
    apiClient.patch<void>(`${base(serviceId)}/${domainId}`, body),

  delete: (serviceId: string, domainId: string) =>
    apiClient.delete<void>(`${base(serviceId)}/${domainId}`),

  uploadCertificate: (serviceId: string, domainId: string, body: UploadDomainCertificateInput) =>
    apiClient.post<UploadDomainCertificateResult>(
      `${base(serviceId)}/${domainId}/certificate`,
      body
    ),

  removeCertificate: (serviceId: string, domainId: string) =>
    apiClient.delete<void>(`${base(serviceId)}/${domainId}/certificate`),
};

/** Owner-agnostic - domain ids are globally unique, so this works for service- and sidecar-owned domains alike. */
export const getDomainCertificateStatus = (domainId: string) =>
  apiClient.get<DomainCertificateStatusDto>(`/domains/${domainId}/certificate/status`);
