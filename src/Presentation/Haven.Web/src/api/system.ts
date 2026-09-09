import { apiClient } from './client';
import { BuildInfoDto, LatestVersionDto } from './types/system.types';

export const systemApi = {
  getBuildInfo: () => apiClient.get<BuildInfoDto>('/system/build-info'),
  getAllPermissions: () => apiClient.get<string[]>('/system/permissions'),
  getLatestVersion: () => apiClient.get<LatestVersionDto>('/system/latest-version'),
};
