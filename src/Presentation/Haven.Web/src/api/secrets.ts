import { apiClient } from './client';
import { PagedResult } from './types';
import {
  CreateSecretInput,
  GetSecretsParams,
  SecretsScope,
  SecretVariableDto,
  UpdateSecretInput,
} from './types/secrets.types';

function scopePath(scope: SecretsScope): string {
  switch (scope.parentType) {
    case 'Project':
      return `/projects/${scope.projectId}/secrets`;
    case 'Environment':
      return `/projects/${scope.projectId}/environments/${scope.environmentId}/secrets`;
    case 'Service':
      return `/projects/${scope.projectId}/environments/${scope.environmentId}/services/${scope.serviceId}/secrets`;
  }
}

export const secretsApi = {
  list: (scope: SecretsScope, params?: GetSecretsParams) =>
    apiClient.get<PagedResult<SecretVariableDto>>(scopePath(scope), params),

  create: (scope: SecretsScope, data: CreateSecretInput) =>
    apiClient.post<string>(scopePath(scope), data),

  update: (id: string, data: UpdateSecretInput) => apiClient.patch<string>(`/secrets/${id}`, data),

  delete: (id: string) => apiClient.delete(`/secrets/${id}`),
};
