import { apiClient } from './client';
import { AddDomainInput, UpdateDomainInput } from './types/registryDomain.types';
import { ServiceRegistryDomainDto } from './types/service.types';

const base = (sidecarId: string) => `/sidecars/${sidecarId}/domains`;

export const sidecarDomainsApi = {
  list: (sidecarId: string) => apiClient.get<ServiceRegistryDomainDto[]>(base(sidecarId)),

  add: (sidecarId: string, body: AddDomainInput) => apiClient.post<string>(base(sidecarId), body),

  update: (sidecarId: string, domainId: string, body: UpdateDomainInput) =>
    apiClient.patch<void>(`${base(sidecarId)}/${domainId}`, body),

  delete: (sidecarId: string, domainId: string) =>
    apiClient.delete<void>(`${base(sidecarId)}/${domainId}`),
};
