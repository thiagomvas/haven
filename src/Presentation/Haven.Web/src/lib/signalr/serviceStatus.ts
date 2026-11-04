import type {
  DesiredState,
  ServiceDisplayStatus,
  ServiceHealth,
  ServiceRuntimeState,
} from '@/api/types';

/** Payload of the `ServiceStatusChanged` hub event: the full observed state, recomputed server-side. */
export interface ServiceStatusData {
  serviceId: string;
  serviceName: string;
  newStatus: string;
  desiredState?: string;
  runtimeState?: string;
  health?: string;
}

interface StatusFields {
  status: ServiceDisplayStatus;
  desiredState?: DesiredState;
  runtimeState?: ServiceRuntimeState;
  health?: ServiceHealth;
}

/** Merges every state field of an update, not just `status`, so derived UI (e.g. action buttons) stays correct. */
export function applyServiceStatus<T extends StatusFields>(service: T, data: ServiceStatusData): T {
  return {
    ...service,
    status: data.newStatus as ServiceDisplayStatus,
    ...(data.desiredState && { desiredState: data.desiredState as DesiredState }),
    ...(data.runtimeState && { runtimeState: data.runtimeState as ServiceRuntimeState }),
    ...(data.health && { health: data.health as ServiceHealth }),
  };
}

/** Statuses that should be transient; if one lingers, the UI polls as a safety net against missed messages. */
export const TRANSITIONAL_STATUSES: readonly string[] = ['Queued', 'Deploying', 'Starting'];
