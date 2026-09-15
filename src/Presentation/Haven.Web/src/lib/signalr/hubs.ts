import * as signalR from '@microsoft/signalr';

import { createHubManager, HubManager } from './createHubManager';
import { createHubConnection } from './hubConnection';

export const serviceStatusHub: HubManager = createHubManager('/hubs/services/status');

export const deploymentLogsHub: HubManager = createHubManager('/hubs/deployments/logs', {
  subscribeMethod: 'SubscribeToDeployment',
  unsubscribeMethod: 'UnsubscribeFromDeployment',
});

// Request/response RPC (StartShell/SendInput/StopShell), not the group subscribe/unsubscribe
// model createHubManager wraps — used directly by useContainerShell instead.
export const containerShellConnection: signalR.HubConnection = createHubConnection(
  '/hubs/services/shell',
  { authenticated: true }
);

export async function startHubs(): Promise<void> {
  await serviceStatusHub.start();
}

export async function stopHubs(): Promise<void> {
  await serviceStatusHub.stop();
}
