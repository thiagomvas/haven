import { useEffect, useRef } from 'react';

import { HubManager } from './createHubManager';
import type { ServiceStatusData } from './serviceStatus';

/**
 * Subscribes to status updates for one service. Callbacks may change identity every render; they are
 * read through a ref so the subscription itself only depends on the hub and service id (no churn).
 */
export function useSubscribeToServiceUpdates(
  hub: HubManager,
  serviceId: string | undefined,
  onStatusChange: (data: ServiceStatusData) => void,
  onResync?: () => void
) {
  const statusRef = useRef(onStatusChange);
  const resyncRef = useRef(onResync);
  useEffect(() => {
    statusRef.current = onStatusChange;
    resyncRef.current = onResync;
  });

  useEffect(() => {
    if (!serviceId) return;

    const handler = (data: ServiceStatusData) => {
      if (data.serviceId.toLowerCase() === serviceId.toLowerCase()) statusRef.current(data);
    };

    // Register synchronously so no message is missed while the subscribe call is in flight.
    hub.on<ServiceStatusData>('ServiceStatusChanged', handler);
    const offResync = hub.onResync(() => resyncRef.current?.());
    hub.subscribe(serviceId).catch(console.error);

    return () => {
      hub.off('ServiceStatusChanged', handler);
      offResync();
      hub.unsubscribe(serviceId).catch(console.error);
    };
  }, [hub, serviceId]);
}
