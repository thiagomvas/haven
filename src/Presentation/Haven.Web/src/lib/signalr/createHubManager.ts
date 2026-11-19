import * as signalR from '@microsoft/signalr';

import { createHubConnection } from './hubConnection';

export interface HubManager {
  start(): Promise<void>;
  stop(): Promise<void>;
  /** Reference-counted: the server group is joined on the first subscriber and left after the last one. */
  subscribe(group: string): Promise<void>;
  unsubscribe(group: string): Promise<void>;
  on<T>(event: string, callback: (data: T) => void): void;
  off<T>(event: string, callback?: (data: T) => void): void;
  /**
   * Called whenever messages may have been missed (reconnected, connection restored after a failure,
   * tab became visible again). Subscribers should refetch their state.
   */
  onResync(callback: () => void): () => void;
  readonly state: signalR.HubConnectionState;
}

interface HubManagerOptions {
  subscribeMethod?: string;
  unsubscribeMethod?: string;
}

const RETRY_DELAYS_MS = [1000, 2000, 5000, 10000, 30000];

export function createHubManager(path: string, options: HubManagerOptions = {}): HubManager {
  const subscribeMethod = options.subscribeMethod ?? 'SubscribeToService';
  const unsubscribeMethod = options.unsubscribeMethod ?? 'UnsubscribeFromService';

  const connection = createHubConnection(path);
  const groupRefCounts = new Map<string, number>();
  const resyncListeners = new Set<() => void>();

  let wantConnected = false;
  let hasConnectedBefore = false;
  let retryAttempt = 0;
  let retryTimer: ReturnType<typeof setTimeout> | undefined;
  let hiddenAt: number | null = null;

  const isConnected = () => connection.state === signalR.HubConnectionState.Connected;

  const emitResync = () => resyncListeners.forEach(cb => cb());

  const joinAllGroups = async () => {
    for (const group of groupRefCounts.keys()) {
      try {
        await connection.invoke(subscribeMethod, group);
      } catch (err) {
        console.error(`Failed to join group ${group}`, err);
      }
    }
  };

  const clearRetry = () => {
    if (retryTimer !== undefined) {
      clearTimeout(retryTimer);
      retryTimer = undefined;
    }
  };

  const scheduleRetry = () => {
    if (!wantConnected || retryTimer !== undefined) return;
    const delay = RETRY_DELAYS_MS[Math.min(retryAttempt, RETRY_DELAYS_MS.length - 1)];
    retryAttempt++;
    retryTimer = setTimeout(() => {
      retryTimer = undefined;
      void connect();
    }, delay);
  };

  const connect = async () => {
    if (!wantConnected || connection.state !== signalR.HubConnectionState.Disconnected) return;
    try {
      await connection.start();
      retryAttempt = 0;
      await joinAllGroups();
      // The very first connection has nothing stale to repair; any later one may have missed events.
      if (hasConnectedBefore) emitResync();
      hasConnectedBefore = true;
    } catch (err) {
      console.error('Failed to start SignalR connection', err);
      scheduleRetry();
    }
  };

  connection.onreconnected(async () => {
    await joinAllGroups();
    emitResync();
  });

  // Automatic reconnect gives up after a few attempts; keep trying instead of staying dead until a refresh.
  connection.onclose(() => {
    scheduleRetry();
  });

  if (typeof document !== 'undefined') {
    document.addEventListener('visibilitychange', () => {
      if (document.visibilityState === 'hidden') {
        hiddenAt = Date.now();
        return;
      }
      const wasHiddenLong = hiddenAt !== null && Date.now() - hiddenAt > 5000;
      hiddenAt = null;
      if (!wantConnected) return;
      if (connection.state === signalR.HubConnectionState.Disconnected) {
        clearRetry();
        retryAttempt = 0;
        void connect(); // emits resync on success
      } else if (wasHiddenLong && isConnected()) {
        emitResync(); // timers/sockets may have been throttled while hidden
      }
    });
  }

  return {
    get state() {
      return connection.state;
    },

    async start() {
      wantConnected = true;
      await connect();
    },

    async stop() {
      wantConnected = false;
      clearRetry();
      groupRefCounts.clear();
      hasConnectedBefore = false;
      await connection.stop();
    },

    async subscribe(group: string) {
      const count = groupRefCounts.get(group) ?? 0;
      groupRefCounts.set(group, count + 1);
      if (count > 0 || !isConnected()) return; // joined on connect otherwise
      try {
        await connection.invoke(subscribeMethod, group);
      } catch (err) {
        console.error(`Failed to subscribe to group ${group}`, err);
      }
    },

    async unsubscribe(group: string) {
      const count = groupRefCounts.get(group) ?? 0;
      if (count === 0) return;
      if (count > 1) {
        groupRefCounts.set(group, count - 1);
        return;
      }
      groupRefCounts.delete(group);
      if (!isConnected()) return;
      try {
        await connection.invoke(unsubscribeMethod, group);
      } catch (err) {
        console.error(`Failed to unsubscribe from group ${group}`, err);
      }
    },

    on<T>(event: string, callback: (data: T) => void) {
      connection.on(event, callback);
    },

    off<T>(event: string, callback?: (data: T) => void) {
      if (callback) {
        connection.off(event, callback as (...args: unknown[]) => void);
      } else {
        connection.off(event);
      }
    },

    onResync(callback: () => void) {
      resyncListeners.add(callback);
      return () => {
        resyncListeners.delete(callback);
      };
    },
  };
}
