import { apiClient } from './client';
import { PagedResult } from './types';
import { UpdateFeatureFlagInput } from './types/featureflags.types';
import { CreateFeatureFlagInput } from './types/featureflags.types';
import { FeatureFlagDto } from './types/featureflags.types';

export const featureFlagsApi = {
  list: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    pageNumber: number = 1,
    pageSize: number = 100
  ) =>
    apiClient.get<PagedResult<FeatureFlagDto>>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/feature-flags?pageNumber=${pageNumber}&pageSize=${pageSize}`
    ),

  create: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    body: CreateFeatureFlagInput
  ) =>
    apiClient.post<string>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/feature-flags`,
      body
    ),

  batchCreate: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    creates: CreateFeatureFlagInput[]
  ) =>
    apiClient.post<string[]>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/feature-flags/batch`,
      { creates }
    ),

  update: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    flagId: string,
    body: UpdateFeatureFlagInput
  ) =>
    apiClient.patch<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/feature-flags/${flagId}`,
      body
    ),

  batchUpdate: (
    projectId: string,
    environmentId: string,
    serviceId: string,
    updates: Array<{ flagId: string } & UpdateFeatureFlagInput>
  ) =>
    apiClient.patch<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/feature-flags/batch`,
      { updates }
    ),

  delete: (projectId: string, environmentId: string, serviceId: string, flagId: string) =>
    apiClient.delete<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/feature-flags/${flagId}`
    ),

  batchDelete: (projectId: string, environmentId: string, serviceId: string, flagIds: string[]) =>
    apiClient.delete<void>(
      `/projects/${projectId}/environments/${environmentId}/services/${serviceId}/feature-flags/batch?flagIds=${encodeURIComponent(flagIds.join(','))}`
    ),
};
