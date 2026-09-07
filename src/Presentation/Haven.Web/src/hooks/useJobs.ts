import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { jobsApi } from '@/api/jobs';

const JOBS_KEY = 'jobs';

export function useJobs() {
  return useQuery({
    queryKey: [JOBS_KEY],
    queryFn: () => jobsApi.getAll(),
    staleTime: 0,
    refetchInterval: 30_000,
  });
}

export function useTriggerJob() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (jobKey: string) => jobsApi.trigger(jobKey),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: [JOBS_KEY] });
    },
  });
}
