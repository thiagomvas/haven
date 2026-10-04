import { apiClient } from './client';
import {
  CreateCustomActionInput,
  CustomActionDto,
  UpdateCustomActionInput,
} from './types/customAction.types';

const base = (projectId: string, environmentId: string, serviceId: string) =>
  `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/actions`;

export const customActionsApi = {
  list: (projectId: string, environmentId: string, serviceId: string) =>
    apiClient.get<CustomActionDto[]>(base(projectId, environmentId, serviceId)),

  create: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    body: CreateCustomActionInput
  ) => apiClient.post<string>(base(projectId, environmentId, serviceId), body),

  update: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    actionId: string,
    body: UpdateCustomActionInput
  ) => apiClient.patch<void>(`${base(projectId, environmentId, serviceId)}/${actionId}`, body),

  delete: (projectId: string, environmentId: string, serviceId: string, actionId: string) =>
    apiClient.delete<void>(`${base(projectId, environmentId, serviceId)}/${actionId}`),

  execute: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    actionId: string,
    inputs?: Record<string, string>
  ) =>
    apiClient.post<void>(`${base(projectId, environmentId, serviceId)}/${actionId}/execute`, {
      inputs,
    }),
};
