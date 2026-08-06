import { apiClient } from './client';
import { GetNetworksParams } from './types/network.types.ts';
import { NetworkDto } from './types/network.types.ts';

export const networksApi = {
  getAll: (params?: GetNetworksParams) => apiClient.get<NetworkDto[]>('/networks', params),
};
