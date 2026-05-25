import * as signalR from "@microsoft/signalr";

export function createHubConnection(path: string): signalR.HubConnection {
    return new signalR.HubConnectionBuilder()
        .withUrl(`${import.meta.env.VITE_API_URL}${path}`, {
            withCredentials: true,
        })
        .withAutomaticReconnect()
        .build();
}