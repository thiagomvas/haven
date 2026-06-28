import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationChannelsApi } from '@/api/notificationChannels';
import { GetNotificationAttemptsParams } from "@/api/types/notification.types";
import { GetNotificationChannelConfigsParams } from "@/api/types/notification.types";
import { UpdateNotificationChannelConfigInput } from "@/api/types/notification.types";
import { CreateNotificationChannelConfigInput } from "@/api/types/notification.types";
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
    mutationFn: (data: CreateNotificationChannelConfigInput) =>
      notificationChannelsApi.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationChannels'] });
    },
  });
}

export function useUpdateNotificationChannel() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateNotificationChannelConfigInput }) =>
      notificationChannelsApi.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationChannels'] });
    },
  });
}

export function useSetNotificationChannelEnabled() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ id, enabled }: { id: string; enabled: boolean }) =>
      notificationChannelsApi.setEnabled(id, enabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationChannels'] });
    },
  });
}

export function useDeleteNotificationChannel() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (id: string) => notificationChannelsApi.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['notificationChannels'] });
    },
  });
}

export function useTestNotificationChannel() {
  return useMutation({
    mutationFn: (id: string) => notificationChannelsApi.test(id),
  });
}

export function useTestNotificationChannelInline() {
  return useMutation({
    mutationFn: ({ channel, configJson }: { channel: string; configJson: string }) =>
      notificationChannelsApi.testInline(channel, configJson),
  });
}

export function useNotificationAttempts(
  channelConfigId: string | null,
  params?: GetNotificationAttemptsParams
) {
  const canView = usePermission('system.read_notifications');
  return useQuery({
    queryKey: ['notificationAttempts', channelConfigId, params],
    queryFn: () => notificationChannelsApi.getAttempts(channelConfigId!, params),
    enabled: canView && channelConfigId !== null,
    staleTime: 0,
    gcTime: 0,
  });
}
