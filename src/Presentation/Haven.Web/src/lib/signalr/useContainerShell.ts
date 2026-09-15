import * as signalR from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';

import { containerShellConnection } from './hubs';

export type ShellType = 'Bash' | 'Sh';
export type ShellStatus = 'connecting' | 'connected' | 'closed' | 'error';

// The JSON hub protocol has no special handling for byte[]/Uint8Array: JSON.stringify(Uint8Array)
// serializes to {"0":72,"1":105,...} instead of an array or string, which fails to bind to a C#
// byte[] parameter. System.Text.Json's own default byte[] <-> base64-string convention is the one
// thing both sides already agree on, so shell I/O is base64-encoded by hand at this boundary.
function bytesToBase64(bytes: Uint8Array): string {
  let binary = '';
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary);
}

function base64ToBytes(base64: string): Uint8Array {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
  return bytes;
}

interface UseContainerShellOptions {
  projectId: string;
  environmentId: string;
  serviceId: string;
  shellType: ShellType;
  /** Nothing connects until this is true — opening a shell execs a process in the container. */
  enabled: boolean;
  onOutput: (data: Uint8Array) => void;
}

interface UseContainerShellResult {
  status: ShellStatus;
  error: string | null;
  sendInput: (data: Uint8Array) => void;
  stop: () => void;
}

export function useContainerShell({
  projectId,
  environmentId,
  serviceId,
  shellType,
  enabled,
  onOutput,
}: UseContainerShellOptions): UseContainerShellResult {
  const [status, setStatus] = useState<ShellStatus>('connecting');
  const [error, setError] = useState<string | null>(null);
  const sessionIdRef = useRef<string | null>(null);
  const onOutputRef = useRef(onOutput);

  useEffect(() => {
    onOutputRef.current = onOutput;
  }, [onOutput]);

  useEffect(() => {
    if (!enabled) return;

    // 'connecting'/null are already the initial state values, so nothing to reset here — this
    // effect only re-runs (on this hook's current usage) once, on mount.
    let cancelled = false;

    const handleOutput = (sessionId: string, data: string) => {
      if (sessionId === sessionIdRef.current) onOutputRef.current(base64ToBytes(data));
    };
    const handleClosed = (sessionId: string, reason: string | null) => {
      if (sessionId !== sessionIdRef.current) return;
      sessionIdRef.current = null;
      setStatus('closed');
      if (reason) setError(reason);
    };

    containerShellConnection.on('ShellOutput', handleOutput);
    containerShellConnection.on('ShellClosed', handleClosed);

    (async () => {
      try {
        if (containerShellConnection.state === signalR.HubConnectionState.Disconnected) {
          await containerShellConnection.start();
        }
        const sessionId: string = await containerShellConnection.invoke(
          'StartShell',
          projectId,
          environmentId,
          serviceId,
          shellType
        );
        if (cancelled) {
          await containerShellConnection.invoke('StopShell', sessionId).catch(() => {});
          return;
        }
        sessionIdRef.current = sessionId;
        setStatus('connected');
      } catch (err) {
        if (!cancelled) {
          setStatus('error');
          setError(err instanceof Error ? err.message : 'Failed to open shell session');
        }
      }
    })();

    return () => {
      cancelled = true;
      containerShellConnection.off('ShellOutput', handleOutput);
      containerShellConnection.off('ShellClosed', handleClosed);
      const sessionId = sessionIdRef.current;
      sessionIdRef.current = null;
      if (sessionId) {
        containerShellConnection.invoke('StopShell', sessionId).catch(() => {});
      }
    };
  }, [enabled, projectId, environmentId, serviceId, shellType]);

  return {
    status,
    error,
    sendInput: data => {
      const sessionId = sessionIdRef.current;
      if (!sessionId) return;
      containerShellConnection
        .invoke('SendInput', sessionId, bytesToBase64(data))
        .catch(err => console.error('Failed to send shell input', err));
    },
    stop: () => {
      const sessionId = sessionIdRef.current;
      sessionIdRef.current = null;
      setStatus('closed');
      if (sessionId) {
        containerShellConnection.invoke('StopShell', sessionId).catch(() => {});
      }
    },
  };
}
