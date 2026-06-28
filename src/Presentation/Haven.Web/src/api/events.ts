import { apiClient, Params } from './client';
import { PagedResult } from './types';
import { GetEventsParams } from "./types/event.types";
import { DomainEventTypeDto } from "./types/event.types";
import { EventDto } from "./types/event.types";

export const eventsApi = {
  getAll: (params?: GetEventsParams) =>
    apiClient.get<PagedResult<EventDto>>('/events', params as Params | undefined),

  getTypes: () => apiClient.get<DomainEventTypeDto[]>('/events/types'),
};
