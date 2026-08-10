import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { repositoryCleanupApi, RepositoryCleanupOptions } from '@/api/repositoryCleanup';

export function useRepositoryCleanupOptions() {
  return useQuery({
    queryKey: ['repositoryCleanupOptions'],
    queryFn: repositoryCleanupApi.getOptions,
  });
}

export function useUpdateRepositoryCleanupOptions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (options: RepositoryCleanupOptions) => repositoryCleanupApi.updateOptions(options),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['repositoryCleanupOptions'] });
    },
  });
}
