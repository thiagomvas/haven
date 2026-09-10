import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { ThemeContext } from '@/context/ThemeContext';
import { useContainerShell } from '@/lib/signalr/useContainerShell';
import { render, screen } from '@/test/render';

import { ShellTab } from './ShellTab';

vi.mock('@/lib/signalr/useContainerShell', () => ({ useContainerShell: vi.fn() }));

class MockResizeObserver {
  observe = vi.fn();
  unobserve = vi.fn();
  disconnect = vi.fn();
}
vi.stubGlobal('ResizeObserver', MockResizeObserver);

vi.mock('@xterm/xterm', () => ({
  Terminal: class {
    loadAddon = vi.fn();
    open = vi.fn();
    onData = vi.fn().mockReturnValue({ dispose: vi.fn() });
    write = vi.fn();
    focus = vi.fn();
    dispose = vi.fn();
    options = {};
  },
}));

vi.mock('@xterm/addon-fit', () => ({
  FitAddon: class {
    fit = vi.fn();
  },
}));

function renderShellTab() {
  return render(
    <ThemeContext.Provider value={{ theme: 'light', toggleTheme: vi.fn() }}>
      <ShellTab projectId="p1" environmentId="e1" serviceId="s1" />
    </ThemeContext.Provider>
  );
}

function mockShell(overrides: Partial<ReturnType<typeof useContainerShell>> = {}) {
  vi.mocked(useContainerShell).mockReturnValue({
    status: 'connecting',
    error: null,
    sendInput: vi.fn(),
    stop: vi.fn(),
    ...overrides,
  });
}

describe('ShellTab', () => {
  it('shows the start screen with a default shell type before a session is started', () => {
    mockShell();

    renderShellTab();

    expect(screen.getByRole('button', { name: 'Start shell' })).toBeInTheDocument();
    expect(useContainerShell).not.toHaveBeenCalled();
  });

  it('starts a shell session with the default shell type when clicking start', async () => {
    mockShell();
    const user = userEvent.setup();

    renderShellTab();
    await user.click(screen.getByRole('button', { name: 'Start shell' }));

    expect(useContainerShell).toHaveBeenCalledWith(
      expect.objectContaining({
        projectId: 'p1',
        environmentId: 'e1',
        serviceId: 's1',
        shellType: 'Bash',
        enabled: true,
      })
    );
  });

  it('shows a connecting indicator while the session is starting', async () => {
    mockShell({ status: 'connecting' });
    const user = userEvent.setup();

    renderShellTab();
    await user.click(screen.getByRole('button', { name: 'Start shell' }));

    expect(screen.getByText('Connecting…')).toBeInTheDocument();
  });

  it('shows a connected indicator naming the shell once connected', async () => {
    mockShell({ status: 'connected' });
    const user = userEvent.setup();

    renderShellTab();
    await user.click(screen.getByRole('button', { name: 'Start shell' }));

    expect(screen.getByText('Connected (bash)')).toBeInTheDocument();
  });

  it('shows the error message when the session fails to connect', async () => {
    mockShell({ status: 'error', error: 'Failed to open shell session' });
    const user = userEvent.setup();

    renderShellTab();
    await user.click(screen.getByRole('button', { name: 'Start shell' }));

    expect(screen.getByText('Failed to open shell session')).toBeInTheDocument();
  });

  it('returns to the start screen when closing an active session', async () => {
    mockShell({ status: 'connected' });
    const user = userEvent.setup();

    renderShellTab();
    await user.click(screen.getByRole('button', { name: 'Start shell' }));
    await user.click(screen.getByRole('button', { name: 'Close shell' }));

    expect(screen.getByRole('button', { name: 'Start shell' })).toBeInTheDocument();
  });
});
