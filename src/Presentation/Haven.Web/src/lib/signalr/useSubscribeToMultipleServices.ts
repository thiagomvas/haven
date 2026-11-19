import { useEffect, useRef } from 'react';

import { HubManager } from './createHubManager';
import type { ServiceStatusData } from './serviceStatus';

export function useSubscribeToMultipleServices(
  hub: HubManager,
  serviceIds: string[],
  onStatusChange: (data: ServiceStatusData) => void,
  onResync?: () => void
) {
  const statusRef = useRef(onStatusChange);
  const resyncRef = useRef(onResync);
  useEffect(() => {
    statusRef.current = onStatusChange;
    resyncRef.current = onResync;
  });

  const key = serviceIds.join(',');

  useEffect(() => {
    if (!key) return;
    const ids = key.split(',');

    const handler = (data: ServiceStatusData) => statusRef.current(data);

    hub.on<ServiceStatusData>('ServiceStatusChanged', handler);
    const offResync = hub.onResync(() => resyncRef.current?.());
    ids.forEach(id => hub.subscribe(id).catch(console.error));

    return () => {
      hub.off('ServiceStatusChanged', handler);
      offResync();
      ids.forEach(id => hub.unsubscribe(id).catch(console.error));
    };
  }, [hub, key]);
}
