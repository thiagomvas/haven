import { apiClient } from './client';
import {
  NotificationChannelConfigDto,
  CreateNotificationChannelConfigInput,
  UpdateNotificationChannelConfigInput,
  GetNotificationChannelConfigsParams,
  PagedResult,
} from './types';

export const notificationChannelsApi = {
  getAll: (params?: GetNotificationChannelConfigsParams) =>
    apiClient.get<PagedResult<NotificationChannelConfigDto>>('/notification-channels', params),

  getById: (id: string) =>
    apiClient.get<NotificationChannelConfigDto>(`/notification-channels/${id}`),

  create: (data: CreateNotificationChannelConfigInput) =>
    apiClient.post<string>('/notification-channels', data),

  update: (id: string, data: UpdateNotificationChannelConfigInput) =>
    apiClient.put<string>(`/notification-channels/${id}`, data),
};
