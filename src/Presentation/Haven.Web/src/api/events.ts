import { apiClient, Params } from './client';
import { DomainEventTypeDto, EventDto, GetEventsParams, PagedResult } from './types';

export const eventsApi = {
  getAll: (params?: GetEventsParams) =>
    apiClient.get<PagedResult<EventDto>>('/events', params as Params | undefined),

  getTypes: () => apiClient.get<DomainEventTypeDto[]>('/events/types'),
};
