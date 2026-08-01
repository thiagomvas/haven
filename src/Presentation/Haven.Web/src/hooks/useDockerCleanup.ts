import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { dockerCleanupApi, DockerCleanupOptions } from '@/api/dockerCleanup';

export function useDockerCleanupOptions() {
  return useQuery({
    queryKey: ['dockerCleanupOptions'],
    queryFn: dockerCleanupApi.getOptions,
  });
}

export function useUpdateDockerCleanupOptions() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (options: DockerCleanupOptions) => dockerCleanupApi.updateOptions(options),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['dockerCleanupOptions'] });
    },
  });
}
