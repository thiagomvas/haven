import { useQuery } from '@tanstack/react-query';
import { PagedResult } from '@/api/types';
import { GetEventsParams } from "@/api/types/event.types";
import { DomainEventTypeDto } from "@/api/types/event.types";
import { EventDto } from "@/api/types/event.types";
import { eventsApi } from '@/api/events';
import { usePermission } from './usePermission';

const EVENTS_KEY = 'events';
const DOMAIN_EVENT_TYPES_KEY = 'domainEventTypes';

export function useEvents(params?: GetEventsParams) {
  const canView = usePermission('projects.read');
  return useQuery({
    queryKey: [EVENTS_KEY, params],
    queryFn: () => eventsApi.getAll(params),
    enabled: canView,
    staleTime: 0,
    gcTime: 0,
  });
}

export function useDomainEventTypes() {
  return useQuery({
    queryKey: [DOMAIN_EVENT_TYPES_KEY],
    queryFn: () => eventsApi.getTypes(),
  });
}
