import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationChannelsApi } from '@/api/notificationChannels';
import { CreateNotificationChannelConfigInput, GetNotificationChannelConfigsParams } from '@/api/types';
import { usePermission } from './usePermission';

export function useNotificationChannels(params?: GetNotificationChannelConfigsParams) {
  const canView = usePermission('system.read_notifications');
  return useQuery({
    queryKey: ['notificationChannels', params],
    queryFn: () => notificationChannelsApi.getAll(params),
    enabled: canView,
  });
}

export function useCreateNotificationChannel() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: CreateNotificationChannelConfigInput) => notificationChannelsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationChannels'] });
    },
  });
}
