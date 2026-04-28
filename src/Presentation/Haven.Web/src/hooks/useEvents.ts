import { useQuery } from '@tanstack/react-query'
import { EventDto, GetEventsParams, PagedResult } from '@/api/types'
import { eventsApi } from '@/api/events'

const EVENTS_KEY = 'events'

export function useEvents(params?: GetEventsParams) {
  return useQuery({
    queryKey: [EVENTS_KEY, params],
    queryFn: () => eventsApi.getAll(params),
  })
}
