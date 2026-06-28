import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { telemetryApi, TelemetryOptions } from '@/api/telemetry';

export function useTelemetry() {
  return useQuery({
    queryKey: ['telemetry'],
    queryFn: telemetryApi.get,
  });
}

export function useUpdateTelemetry() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (options: TelemetryOptions) => telemetryApi.update(options),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['telemetry'] });
    },
  });
}
