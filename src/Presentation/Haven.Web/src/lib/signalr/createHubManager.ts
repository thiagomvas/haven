import * as signalR from "@microsoft/signalr";
import { createHubConnection } from "./hubConnection";

export interface HubManager {
    start(): Promise<void>;
    stop(): Promise<void>;
    subscribe(group: string): Promise<void>;
    unsubscribe(group: string): Promise<void>;
    on<T>(event: string, callback: (data: T) => void): void;
    off(event: string, callback?: (...args: unknown[]) => void): void;
    readonly state: signalR.HubConnectionState;
}

export function createHubManager(path: string): HubManager {
    const connection = createHubConnection(path);
    const activeGroups = new Set<string>();

    connection.onreconnected(async () => {
        for (const group of activeGroups) {
            try {
                await connection.invoke("SubscribeToService", group);
            } catch (err) {
                console.error("Failed to re-subscribe to service after reconnect", err);
            }
        }
    });

    return {
        get state() {
            return connection.state;
        },

        async start() {
            if (connection.state === signalR.HubConnectionState.Disconnected) {
                try {
                    await connection.start();
                } catch (err) {
                    console.error("Failed to start SignalR connection", err);
                }
            }
        },

        async stop() {
            activeGroups.clear();
            await connection.stop();
        },

        async subscribe(group: string) {
            activeGroups.add(group);
            if (connection.state === signalR.HubConnectionState.Connected) {
                try {
                    await connection.invoke("SubscribeToService", group);
                } catch (err) {
                    console.error(`Failed to subscribe to service group ${group}`, err);
                }
            }
        },

        async unsubscribe(group: string) {
            activeGroups.delete(group);
            if (connection.state === signalR.HubConnectionState.Connected) {
                try {
                    await connection.invoke("UnsubscribeFromService", group);
                } catch (err) {
                    console.error(`Failed to unsubscribe from service group ${group}`, err);
                }
            }
        },

        on<T>(event: string, callback: (data: T) => void) {
            connection.on(event, callback);
        },

        off(event: string, callback?: (...args: unknown[]) => void) {
            callback ? connection.off(event, callback) : connection.off(event);
        },
    };
}