import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { secretsApi } from '@/api/secrets';
import { CreateSecretInput, GetSecretsParams, SecretsScope, UpdateSecretInput } from '@/api/types';

import { usePermission } from './usePermission';

function scopeKey(scope: SecretsScope) {
  return ['secrets', scope.parentType, scope.projectId, scope.environmentId, scope.serviceId];
}

export function useSecrets(scope: SecretsScope, params?: GetSecretsParams) {
  const canView = usePermission('projects.manage_secrets');
  return useQuery({
    queryKey: [...scopeKey(scope), params],
    queryFn: () => secretsApi.list(scope, params),
    enabled: canView,
  });
}

export function useCreateSecret(scope: SecretsScope) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateSecretInput) => secretsApi.create(scope, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: scopeKey(scope) });
    },
  });
}

export function useUpdateSecret(scope: SecretsScope) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateSecretInput }) =>
      secretsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: scopeKey(scope) });
    },
  });
}

export function useDeleteSecret(scope: SecretsScope) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => secretsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: scopeKey(scope) });
    },
  });
}
