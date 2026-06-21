import { apiClient, Params } from './client';
import { GetServiceRegistryParams, PagedResult, PagedServiceRegistryEntryDto } from './types';

export const serviceRegistryApi = {
  getAll: (params?: GetServiceRegistryParams) =>
    apiClient.get<PagedResult<PagedServiceRegistryEntryDto>>(
      '/service-registry',
      params as Params | undefined,
    ),
};
