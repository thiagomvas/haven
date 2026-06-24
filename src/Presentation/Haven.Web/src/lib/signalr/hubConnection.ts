import * as signalR from '@microsoft/signalr';

export function createHubConnection(path: string): signalR.HubConnection {
  const baseUrl = import.meta.env.VITE_API_URL ?? '';
  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}${path}`, {
      withCredentials: true,
    })
    .withAutomaticReconnect()
    .build();

  connection.onclose(() => {
    console.error(`SignalR connection closed: ${path}`);
  });

  return connection;
}
