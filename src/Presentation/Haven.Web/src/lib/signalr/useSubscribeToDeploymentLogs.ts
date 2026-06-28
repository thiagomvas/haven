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

    // Register synchronously so no messages are missed during async start/subscribe
    hub.on<DeploymentLogEntry>('ReceiveLogEntry', onLogEntry);

    hub
      .start()
      .then(() => hub.subscribe(deploymentId))
      .catch(err => console.error('Failed to subscribe to deployment logs', err));

    return () => {
      hub.off('ReceiveLogEntry', onLogEntry);
      hub.unsubscribe(deploymentId).catch(console.error);
    };
  }, [hub, deploymentId, onLogEntry]);
}
