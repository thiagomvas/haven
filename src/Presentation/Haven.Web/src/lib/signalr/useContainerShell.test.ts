import * as signalR from '@microsoft/signalr';
import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { useContainerShell } from './useContainerShell';

const handlers: Record<string, (...args: unknown[]) => void> = {};

const mockConnection = {
  state: signalR.HubConnectionState.Disconnected as signalR.HubConnectionState,
  start: vi.fn().mockResolvedValue(undefined),
  invoke: vi.fn(),
  on: vi.fn((event: string, handler: (...args: unknown[]) => void) => {
    handlers[event] = handler;
  }),
  off: vi.fn((event: string) => {
    delete handlers[event];
  }),
};

vi.mock('./hubs', () => ({
  get containerShellConnection() {
    return mockConnection;
  },
}));

function bytesToBase64(bytes: Uint8Array): string {
  let binary = '';
  for (const byte of bytes) binary += String.fromCharCode(byte);
  return btoa(binary);
}

function defaultOptions(overrides: Partial<Parameters<typeof useContainerShell>[0]> = {}) {
  return {
    projectId: 'p1',
    environmentId: 'e1',
    serviceId: 's1',
    shellType: 'Bash' as const,
    enabled: true,
    onOutput: vi.fn(),
    ...overrides,
  };
}

describe('useContainerShell', () => {
  beforeEach(() => {
    mockConnection.state = signalR.HubConnectionState.Disconnected;
    mockConnection.invoke.mockReset().mockResolvedValue(undefined);
    mockConnection.start.mockClear();
    mockConnection.on.mockClear();
    mockConnection.off.mockClear();
  });

  it('does nothing when disabled', () => {
    renderHook(() => useContainerShell(defaultOptions({ enabled: false })));

    expect(mockConnection.start).not.toHaveBeenCalled();
    expect(mockConnection.invoke).not.toHaveBeenCalled();
  });

  it('starts the connection and shell session when enabled', async () => {
    mockConnection.invoke.mockResolvedValueOnce('session-1');

    const { result } = renderHook(() => useContainerShell(defaultOptions()));

    await waitFor(() => expect(result.current.status).toBe('connected'));

    expect(mockConnection.start).toHaveBeenCalledTimes(1);
    expect(mockConnection.invoke).toHaveBeenCalledWith('StartShell', 'p1', 'e1', 's1', 'Bash');
  });

  it('does not call start again when the connection is already connected', async () => {
    mockConnection.state = signalR.HubConnectionState.Connected;
    mockConnection.invoke.mockResolvedValueOnce('session-1');

    renderHook(() => useContainerShell(defaultOptions()));

    await waitFor(() => expect(mockConnection.invoke).toHaveBeenCalled());

    expect(mockConnection.start).not.toHaveBeenCalled();
  });

  it('sets status to error when starting the session fails', async () => {
    mockConnection.invoke.mockRejectedValueOnce(new Error('boom'));

    const { result } = renderHook(() => useContainerShell(defaultOptions()));

    await waitFor(() => expect(result.current.status).toBe('error'));
    expect(result.current.error).toBe('boom');
  });

  it('decodes and forwards output for the active session', async () => {
    mockConnection.invoke.mockResolvedValueOnce('session-1');
    const onOutput = vi.fn();

    renderHook(() => useContainerShell(defaultOptions({ onOutput })));

    await waitFor(() => expect(handlers.ShellOutput).toBeDefined());

    const payload = bytesToBase64(new Uint8Array([104, 105]));
    handlers.ShellOutput('session-1', payload);

    expect(onOutput).toHaveBeenCalledWith(new Uint8Array([104, 105]));
  });

  it('ignores output for a session id that is not the active one', async () => {
    mockConnection.invoke.mockResolvedValueOnce('session-1');
    const onOutput = vi.fn();

    renderHook(() => useContainerShell(defaultOptions({ onOutput })));

    await waitFor(() => expect(handlers.ShellOutput).toBeDefined());

    handlers.ShellOutput('some-other-session', bytesToBase64(new Uint8Array([1])));

    expect(onOutput).not.toHaveBeenCalled();
  });

  it('transitions to closed when the server reports the session closed', async () => {
    mockConnection.invoke.mockResolvedValueOnce('session-1');

    const { result } = renderHook(() => useContainerShell(defaultOptions()));

    await waitFor(() => expect(result.current.status).toBe('connected'));

    handlers.ShellClosed('session-1', 'shell exited');

    await waitFor(() => expect(result.current.status).toBe('closed'));
    expect(result.current.error).toBe('shell exited');
  });

  it('sends input as base64 for the active session', async () => {
    mockConnection.invoke.mockResolvedValueOnce('session-1');

    const { result } = renderHook(() => useContainerShell(defaultOptions()));

    await waitFor(() => expect(result.current.status).toBe('connected'));

    result.current.sendInput(new Uint8Array([104, 105]));

    expect(mockConnection.invoke).toHaveBeenCalledWith('SendInput', 'session-1', bytesToBase64(new Uint8Array([104, 105])));
  });

  it('does not send input when there is no active session', () => {
    const { result } = renderHook(() => useContainerShell(defaultOptions({ enabled: false })));

    result.current.sendInput(new Uint8Array([1]));

    expect(mockConnection.invoke).not.toHaveBeenCalled();
  });

  it('stops the session and sets status to closed', async () => {
    mockConnection.invoke.mockResolvedValueOnce('session-1');

    const { result } = renderHook(() => useContainerShell(defaultOptions()));

    await waitFor(() => expect(result.current.status).toBe('connected'));

    act(() => result.current.stop());

    expect(result.current.status).toBe('closed');
    expect(mockConnection.invoke).toHaveBeenCalledWith('StopShell', 'session-1');
  });

  it('stops the session on unmount', async () => {
    mockConnection.invoke.mockResolvedValueOnce('session-1');

    const { result, unmount } = renderHook(() => useContainerShell(defaultOptions()));

    await waitFor(() => expect(result.current.status).toBe('connected'));

    unmount();

    expect(mockConnection.invoke).toHaveBeenCalledWith('StopShell', 'session-1');
    expect(mockConnection.off).toHaveBeenCalledWith('ShellOutput', expect.any(Function));
    expect(mockConnection.off).toHaveBeenCalledWith('ShellClosed', expect.any(Function));
  });
});
