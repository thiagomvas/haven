import { useQuery } from '@tanstack/react-query'
import { EventDto, GetEventsParams, PagedResult } from '@/api/types'
import { eventsApi } from '@/api/events'
import { usePermission } from './usePermission'

const EVENTS_KEY = 'events'

export function useEvents(params?: GetEventsParams) {
  const canView = usePermission('projects.read')
  return useQuery({
    queryKey: [EVENTS_KEY, params],
    queryFn: () => eventsApi.getAll(params),
    enabled: canView,
  })
}
