import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { githubAppApi, UpdateGitHubAppSettingsInput } from '@/api/githubApp';

import { usePermission } from './usePermission';

export function useGitHubAppSettings() {
  const canView = usePermission('system.manage_git_credentials');
  return useQuery({
    queryKey: ['githubApp'],
    queryFn: githubAppApi.get,
    enabled: canView,
  });
}

export function useUpdateGitHubAppSettings() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: UpdateGitHubAppSettingsInput) => githubAppApi.update(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['githubApp'] });
    },
  });
}
