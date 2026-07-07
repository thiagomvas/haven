import { apiClient } from './client';
import {
  AddVolumeInput,
  ManagedVolumeFileEntry,
  ServiceVolumeDto,
  UpdateVolumeInput,
} from './types/volume.types';

const base = (projectId: string, environmentId: string, serviceId: string) =>
  `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/volumes`;

export const volumesApi = {
  list: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<ServiceVolumeDto[]>(base(projectId, environmentId, serviceId)),

  add: (projectId: string, environmentId: string, serviceId: string, body: AddVolumeInput) =>
    apiClient.post<string>(base(projectId, environmentId, serviceId), body),

  update: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    volumeId: string,
    body: UpdateVolumeInput
  ) => apiClient.patch<void>(`${base(projectId, environmentId, serviceId)}/${volumeId}`, body),

  delete: (projectId: string, environmentId: string, serviceId: string, volumeId: string) =>
    apiClient.delete<void>(`${base(projectId, environmentId, serviceId)}/${volumeId}`),

  listFiles: (projectId: string, environmentId: string, serviceId: string, volumeId: string) =>
    apiClient.get<ManagedVolumeFileEntry[]>(
      `${base(projectId, environmentId, serviceId)}/${volumeId}/files`
    ),

  getFileContent: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    volumeId: string,
    path: string
  ) =>
    apiClient.get<string>(
      `${base(projectId, environmentId, serviceId)}/${volumeId}/files/content`,
      { path }
    ),

  writeFileContent: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    volumeId: string,
    path: string,
    content: string
  ) =>
    apiClient.put<void>(
      `${base(projectId, environmentId, serviceId)}/${volumeId}/files/content`,
      { path, content }
    ),

  deleteFile: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    volumeId: string,
    path: string
  ) =>
    apiClient.delete<void>(
      `${base(projectId, environmentId, serviceId)}/${volumeId}/files/content?path=${encodeURIComponent(path)}`
    ),
};
