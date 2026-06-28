import { apiClient } from './client';
import {
  PagedResult,
} from './types';
import { GetNotificationAttemptsParams } from "./types/notification.types";
import { NotificationAttemptDto } from "./types/notification.types";
import { GetNotificationChannelConfigsParams } from "./types/notification.types";
import { UpdateNotificationChannelConfigInput } from "./types/notification.types";
import { CreateNotificationChannelConfigInput } from "./types/notification.types";
import { NotificationChannelConfigDto } from "./types/notification.types";

export const notificationChannelsApi = {
  getAll: (params?: GetNotificationChannelConfigsParams) =>
    apiClient.get<PagedResult<NotificationChannelConfigDto>>('/notifications/channels', params),

  getById: (id: string) =>
    apiClient.get<NotificationChannelConfigDto>(`/notifications/channels/${id}`),

  create: (data: CreateNotificationChannelConfigInput) =>
    apiClient.post<string>('/notifications/channels', data),

  update: (id: string, data: UpdateNotificationChannelConfigInput) =>
    apiClient.put<string>(`/notifications/channels/${id}`, data),

  setEnabled: (id: string, enabled: boolean) =>
    apiClient.patch<void>(`/notifications/channels/${id}/enabled`, { enabled }),

  delete: (id: string) => apiClient.delete<void>(`/notifications/channels/${id}`),

  test: (id: string) =>
    apiClient.post<{ success: boolean; response: string | null; errorMessage: string | null }>(
      `/notifications/channels/${id}/test`,
      {}
    ),

  testInline: (channel: string, configJson: string) =>
    apiClient.post<{ success: boolean; response: string | null; errorMessage: string | null }>(
      '/notifications/channels/test',
      { channel, configJson }
    ),

  getAttempts: (channelConfigId: string, params?: GetNotificationAttemptsParams) =>
    apiClient.get<PagedResult<NotificationAttemptDto>>(
      `/notifications/channels/${channelConfigId}/attempts`,
      params
    ),
};
