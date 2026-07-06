import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { gitCredentialsApi } from '@/api/gitCredentials';
import { GetGitCredentialsParams } from '@/api/types/git.types';
import { CreateGitCredentialInput } from '@/api/types/git.types';

import { usePermission } from './usePermission';

export function useGitCredentials(params?: GetGitCredentialsParams) {
  const canView = usePermission('system.read_git_credentials');
  return useQuery({
    queryKey: ['gitCredentials', params],
    queryFn: () => gitCredentialsApi.getAll(params),
    enabled: canView,
  });
}

export function useCreateGitCredential() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateGitCredentialInput) => gitCredentialsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gitCredentials'] });
    },
  });
}

export function useStartGitHubOAuth() {
  return useMutation({
    mutationFn: () => gitCredentialsApi.startGitHubOAuth(),
    onSuccess: authorizeUrl => {
      window.location.href = authorizeUrl;
    },
  });
}
