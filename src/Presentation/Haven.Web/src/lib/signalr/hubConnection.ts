import * as signalR from '@microsoft/signalr';

import { tokenStorage } from '@/lib/tokenStorage';

interface CreateHubConnectionOptions {
  /** Attaches the stored JWT via accessTokenFactory, required for hubs decorated with [Authorize]. */
  authenticated?: boolean;
}

export function createHubConnection(
  path: string,
  options: CreateHubConnectionOptions = {}
): signalR.HubConnection {
  const baseUrl = import.meta.env.VITE_API_URL ?? '';
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}${path}`, {
      withCredentials: true,
      ...(options.authenticated && {
        accessTokenFactory: () => tokenStorage.getAccessToken() ?? '',
      }),
    })
    .withAutomaticReconnect()
    .build();

  connection.onclose(() => {
    console.error(`SignalR connection closed: ${path}`);
  });

  return connection;
}
