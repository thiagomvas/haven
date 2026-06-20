import { apiClient } from './client';
import {
  NotificationRuleSummaryItemDto,
  NotificationRuleEventConfigDto,
  NotificationRuleContext,
  SetNotificationRulesInput,
} from './types';

function scopeParams(ctx?: NotificationRuleContext): string {
  if (!ctx) return '';
  return `?scope=${ctx.scope}&scopeId=${ctx.scopeId}`;
}

export const notificationRulesApi = {
  getSummary: (ctx?: NotificationRuleContext) =>
    apiClient.get<NotificationRuleSummaryItemDto[]>(
      `/notifications/rules/summary${scopeParams(ctx)}`
    ),

  getAll: (ctx?: NotificationRuleContext) =>
    apiClient.get<NotificationRuleEventConfigDto[]>(`/notifications/rules${scopeParams(ctx)}`),

  getForEvent: (eventType: string, ctx?: NotificationRuleContext) =>
    apiClient.get<NotificationRuleEventConfigDto>(
      `/notifications/rules/${encodeURIComponent(eventType)}${scopeParams(ctx)}`
    ),

  setForEvent: (
    eventType: string,
    data: SetNotificationRulesInput,
    ctx?: NotificationRuleContext
  ) =>
    apiClient.put<void>(
      `/notifications/rules/${encodeURIComponent(eventType)}${scopeParams(ctx)}`,
      data
    ),

  clearForEvent: (eventType: string, ctx: NotificationRuleContext) =>
    apiClient.delete<void>(
      `/notifications/rules/${encodeURIComponent(eventType)}${scopeParams(ctx)}`
    ),
};
