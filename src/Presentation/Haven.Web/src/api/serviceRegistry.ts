import { apiClient, Params } from './client';
import { GetServiceRegistryParams, PagedResult, ServiceRegistryEntryDto } from './types';

export const serviceRegistryApi = {
  getAll: (params?: GetServiceRegistryParams) =>
    apiClient.get<PagedResult<ServiceRegistryEntryDto>>(
      '/service-registry',
      params as Params | undefined
    ),
};
