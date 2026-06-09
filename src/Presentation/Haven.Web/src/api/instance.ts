import { apiClient } from './client';
import { TimeFormat } from './setup';

export interface InstanceDto {
  instanceName: string;
  timezone: string;
  timeFormat: TimeFormat;
}

export interface UpdateInstanceInput {
  instanceName: string;
  timezone: string;
  timeFormat: TimeFormat;
}

export const instanceApi = {
  get: () => apiClient.get<InstanceDto>('/configuration/instance'),
  update: (data: UpdateInstanceInput) =>
    apiClient.put<InstanceDto>('/configuration/instance', data),
};
