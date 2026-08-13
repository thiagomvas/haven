import { apiClient } from './client';
import { AddDomainInput, UpdateDomainInput } from './types/registryDomain.types';
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
};
