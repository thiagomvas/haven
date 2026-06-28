
export interface EventDto {
  id: string;
  eventType: string;
  message: string;
  payload?: string;
  triggeredAt: string;
}export interface DomainEventTypeDto {
  name: string;
  i18NKey: string;
}
export interface GetEventsParams {
  pageNumber?: number;
  pageSize?: number;
  eventType?: string;
  from?: string;
  to?: string;
  ascending?: boolean;
}

