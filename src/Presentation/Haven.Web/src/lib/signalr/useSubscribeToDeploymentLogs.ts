import { useEffect } from 'react';
import { HubManager } from './createHubManager';

export interface DeploymentLogEntry {
  message: string;
  timestamp: string;
}

export function useSubscribeToDeploymentLogs(
  hub: HubManager,
  deploymentId: string | undefined,
  onLogEntry: (entry: DeploymentLogEntry) => void
) {
  useEffect(() => {
    if (!deploymentId) return;

    const subscribe = async () => {
      try {
        await hub.start();
        await hub.subscribe(deploymentId);
        hub.on<DeploymentLogEntry>('ReceiveLogEntry', onLogEntry);

        return () => {
          hub.off('ReceiveLogEntry', onLogEntry);
        };
      } catch (err) {
        console.error('Failed to subscribe to deployment logs', err);
      }
    };

    const cleanup = subscribe();

    return () => {
      cleanup.then(unsubscribe => unsubscribe?.());
      hub.unsubscribe(deploymentId).catch(console.error);
    };
  }, [hub, deploymentId, onLogEntry]);
}
