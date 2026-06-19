import { createHubManager, HubManager } from './createHubManager';

export const serviceStatusHub: HubManager = createHubManager('/hubs/services/status');

export const deploymentLogsHub: HubManager = createHubManager('/hubs/deployments/logs', {
  subscribeMethod: 'SubscribeToDeployment',
  unsubscribeMethod: 'UnsubscribeFromDeployment',
});

export async function startHubs(): Promise<void> {
  await serviceStatusHub.start();
}

export async function stopHubs(): Promise<void> {
  await serviceStatusHub.stop();
}
