import { apiClient } from './client';
import {
  NotificationRuleSummaryItemDto,
  NotificationRuleEventConfigDto,
  SetNotificationRulesInput,
} from './types';

export const notificationRulesApi = {
  getSummary: () =>
    apiClient.get<NotificationRuleSummaryItemDto[]>('/notifications/rules/summary'),

  getForEvent: (eventType: string) =>
    apiClient.get<NotificationRuleEventConfigDto>(`/notifications/rules/${encodeURIComponent(eventType)}`),

  setForEvent: (eventType: string, data: SetNotificationRulesInput) =>
    apiClient.put<void>(`/notifications/rules/${encodeURIComponent(eventType)}`, data),
};
