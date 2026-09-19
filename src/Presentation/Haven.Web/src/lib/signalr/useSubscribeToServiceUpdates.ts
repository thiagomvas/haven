import { useEffect } from 'react';

import { ServiceHealthChangedData } from '@/api/types';

import { HubManager } from './createHubManager';

interface ServiceStatusData {
  serviceId: string;
  serviceName: string;
  newStatus: string;
}

export function useSubscribeToServiceUpdates(
  hub: HubManager,
  serviceId: string | undefined,
  onStatusChange: (data: ServiceStatusData) => void,
  onHealthChange?: (data: ServiceHealthChangedData) => void
) {
  useEffect(() => {
    if (!serviceId) return;

    const subscribeToUpdates = async () => {
      try {
        await hub.subscribe(serviceId);

        hub.on<ServiceStatusData>('ServiceStatusChanged', onStatusChange);
        if (onHealthChange)
          hub.on<ServiceHealthChangedData>('ServiceHealthChanged', onHealthChange);

        return () => {
          hub.off('ServiceStatusChanged', onStatusChange);
          if (onHealthChange) hub.off('ServiceHealthChanged', onHealthChange);
        };
      } catch (err) {
        console.error('Failed to subscribe to service status updates', err);
      }
    };

    const cleanup = subscribeToUpdates();

    return () => {
      cleanup.then(unsubscribe => {
        if (unsubscribe) {
          unsubscribe();
        }
      });
      hub.unsubscribe(serviceId).catch(console.error);
    };
  }, [hub, serviceId, onStatusChange, onHealthChange]);
}
