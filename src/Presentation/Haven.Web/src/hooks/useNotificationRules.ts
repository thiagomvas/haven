import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { notificationRulesApi } from '@/api/notificationRules';
import { SetNotificationRulesInput } from '@/api/types/notification.types';
import { NotificationRuleContext } from '@/api/types/notification.types';
import { usePermission } from './usePermission';

function ruleQueryKey(key: string, ctx?: NotificationRuleContext) {
  return ctx ? [key, ctx.scope, ctx.scopeId] : [key];
}

export function useNotificationRuleSummary(ctx?: NotificationRuleContext) {
  const canView = usePermission('system.read_notifications');
  return useQuery({
    queryKey: ruleQueryKey('notificationRuleSummary', ctx),
    queryFn: () => notificationRulesApi.getSummary(ctx),
    enabled: canView,
  });
}

export function useAllNotificationRules(ctx?: NotificationRuleContext) {
  const canView = usePermission('system.read_notifications');
  return useQuery({
    queryKey: ruleQueryKey('notificationRules:all', ctx),
    queryFn: () => notificationRulesApi.getAll(ctx),
    enabled: canView,
  });
}

export function useNotificationRulesForEvent(
  eventType: string | null,
  ctx?: NotificationRuleContext
) {
  const canView = usePermission('system.read_notifications');
  return useQuery({
    queryKey: [...ruleQueryKey('notificationRules', ctx), eventType],
    queryFn: () => notificationRulesApi.getForEvent(eventType!, ctx),
    enabled: canView && eventType !== null,
  });
}

export function useSetNotificationRules(ctx?: NotificationRuleContext) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ eventType, data }: { eventType: string; data: SetNotificationRulesInput }) =>
      notificationRulesApi.setForEvent(eventType, data, ctx),
    onSuccess: (_, { eventType }) => {
      queryClient.invalidateQueries({ queryKey: ruleQueryKey('notificationRuleSummary', ctx) });
      queryClient.invalidateQueries({ queryKey: ruleQueryKey('notificationRules:all', ctx) });
      queryClient.invalidateQueries({
        queryKey: [...ruleQueryKey('notificationRules', ctx), eventType],
      });
    },
  });
}

export function useClearNotificationRuleOverride(ctx: NotificationRuleContext) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (eventType: string) => notificationRulesApi.clearForEvent(eventType, ctx),
    onSuccess: (_, eventType) => {
      queryClient.invalidateQueries({ queryKey: ruleQueryKey('notificationRuleSummary', ctx) });
      queryClient.invalidateQueries({ queryKey: ruleQueryKey('notificationRules:all', ctx) });
      queryClient.invalidateQueries({
        queryKey: [...ruleQueryKey('notificationRules', ctx), eventType],
      });
    },
  });
}
