import { useEffect } from 'react';
import { HubManager } from './createHubManager';

interface ServiceStatusData {
  serviceId: string;
  serviceName: string;
  newStatus: string;
}

export function useSubscribeToMultipleServices(
  hub: HubManager,
  serviceIds: string[],
  onStatusChange: (data: ServiceStatusData) => void
) {
  useEffect(() => {
    const subscribeToServiceUpdates = async () => {
      try {
        for (const serviceId of serviceIds) {
          await hub.subscribe(serviceId);
        }

        hub.on<ServiceStatusData>('ServiceStatusChanged', onStatusChange);

        return () => {
          hub.off('ServiceStatusChanged', onStatusChange);
        };
      } catch (err) {
        console.error('Failed to subscribe to service status updates', err);
      }
    };

    if (serviceIds.length > 0) {
      const cleanup = subscribeToServiceUpdates();

      return () => {
        cleanup.then(unsubscribe => {
          if (unsubscribe) {
            unsubscribe();
          }
        });
        serviceIds.forEach(serviceId => {
          hub.unsubscribe(serviceId).catch(console.error);
        });
      };
    }
  }, [hub, serviceIds.join(','), onStatusChange]);
}
