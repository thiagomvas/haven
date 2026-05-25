import { createHubManager, HubManager } from "./createHubManager";

export const serviceStatusHub: HubManager = createHubManager("/hubs/services/status");

export async function startHubs(): Promise<void> {
    await serviceStatusHub.start();
}

export async function stopHubs(): Promise<void> {
    await serviceStatusHub.stop();
}