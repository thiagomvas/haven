import * as signalR from "@microsoft/signalr";

export function createHubConnection(path: string): signalR.HubConnection {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl(`${import.meta.env.VITE_API_URL}${path}`, {
            withCredentials: true,
        })
        .withAutomaticReconnect()
        .build();

    connection.onclose(() => {
        console.error(`SignalR connection closed: ${path}`);
    });

    return connection;
}