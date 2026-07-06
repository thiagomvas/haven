import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { gitCredentialsApi } from '@/api/gitCredentials';
import { GetGitCredentialsParams } from '@/api/types/git.types';
import { CreateGitCredentialInput } from '@/api/types/git.types';
import { UpdateGitCredentialInput } from '@/api/types/git.types';
import { RotateGitCredentialInput } from '@/api/types/git.types';

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

export function useUpdateGitCredential() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateGitCredentialInput }) =>
      gitCredentialsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gitCredentials'] });
    },
  });
}

export function useDeleteGitCredential() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => gitCredentialsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gitCredentials'] });
    },
  });
}

export function useRotateGitCredential() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: RotateGitCredentialInput }) =>
      gitCredentialsApi.rotate(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['gitCredentials'] });
    },
  });
}

export function useStartGitHubOAuth() {
  return useMutation({
    mutationFn: (credentialId?: string) => gitCredentialsApi.startGitHubOAuth(credentialId),
    onSuccess: authorizeUrl => {
      window.location.href = authorizeUrl;
    },
  });
}
