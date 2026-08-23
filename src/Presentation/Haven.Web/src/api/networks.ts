import { apiClient } from './client';
import {
  AttachableServiceDto,
  CreateNetworkInput,
  GetNetworksParams,
  SearchAttachableServicesParams,
} from './types/network.types.ts';
import { NetworkDto } from './types/network.types.ts';

export const networksApi = {
  getAll: (params?: GetNetworksParams) => apiClient.get<NetworkDto[]>('/networks', params),

  getById: (networkId: string) => apiClient.get<NetworkDto>(`/networks/${networkId}`),

  create: (body: CreateNetworkInput) => apiClient.post<string>('/networks', body),

  delete: (networkId: string) => apiClient.delete(`/networks/${networkId}`),

  searchAttachableServices: (networkId: string, params?: SearchAttachableServicesParams) =>
    apiClient.get<AttachableServiceDto[]>(`/networks/${networkId}/attachable-services`, params),

  assignService: (networkId: string, serviceId: string) =>
    apiClient.post(`/networks/${networkId}/services/${serviceId}`, {}),

  unassignService: (networkId: string, serviceId: string) =>
    apiClient.delete(`/networks/${networkId}/services/${serviceId}`),
};
