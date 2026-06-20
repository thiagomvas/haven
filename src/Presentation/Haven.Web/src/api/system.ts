import { apiClient } from './client';
import { BuildInfoDto } from './types';

export const systemApi = {
  getBuildInfo: () => apiClient.get<BuildInfoDto>('/system/build-info'),
  getAllPermissions: () => apiClient.get<string[]>('/system/permissions'),
};
