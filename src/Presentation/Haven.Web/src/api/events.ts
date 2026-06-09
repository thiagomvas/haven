import { apiClient, Params } from './client'
import {
  EventDto,
  GetEventsParams,
  PagedResult,
} from './types'

export const eventsApi = {
  getAll: (params?: GetEventsParams) =>
    apiClient.get<PagedResult<EventDto>>('/events', params as Params | undefined),
}
