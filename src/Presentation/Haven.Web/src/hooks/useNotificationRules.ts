import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationRulesApi } from '@/api/notificationRules';
import { SetNotificationRulesInput } from '@/api/types';
import { usePermission } from './usePermission';

export function useNotificationRuleSummary() {
  const canView = usePermission('system.read_notifications');
  return useQuery({
    queryKey: ['notificationRuleSummary'],
    queryFn: () => notificationRulesApi.getSummary(),
    enabled: canView,
  });
}

export function useNotificationRulesForEvent(eventType: string | null) {
  const canView = usePermission('system.read_notifications');
  return useQuery({
    queryKey: ['notificationRules', eventType],
    queryFn: () => notificationRulesApi.getForEvent(eventType!),
    enabled: canView && eventType !== null,
  });
}

export function useSetNotificationRules() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventType, data }: { eventType: string; data: SetNotificationRulesInput }) =>
      notificationRulesApi.setForEvent(eventType, data),
    onSuccess: (_, { eventType }) => {
      queryClient.invalidateQueries({ queryKey: ['notificationRuleSummary'] });
      queryClient.invalidateQueries({ queryKey: ['notificationRules', eventType] });
    },
  });
}
