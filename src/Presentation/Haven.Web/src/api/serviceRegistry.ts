import { apiClient, Params } from './client';
import { PagedResult } from './types';
import { GetServiceRegistryParams } from "./types/service.types";
import { ServiceRegistryEntryDto } from "./types/service.types";

export const serviceRegistryApi = {
  getAll: (params?: GetServiceRegistryParams) =>
    apiClient.get<PagedResult<ServiceRegistryEntryDto>>(
      '/service-registry',
      params as Params | undefined
    ),
};
