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

  getCertificateStatus: (serviceId: string, domainId: string) =>
    apiClient.get<DomainCertificateStatusDto>(`${base(serviceId)}/${domainId}/certificate/status`),
};
