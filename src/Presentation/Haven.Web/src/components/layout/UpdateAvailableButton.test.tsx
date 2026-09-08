import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import type { MeResponse } from '@/api/auth';
import type { LatestVersionDto } from '@/api/types/system.types';
import { useCurrentUser } from '@/hooks/useCurrentUser';
import { useLatestVersion } from '@/hooks/useLatestVersion';
import { render, screen } from '@/test/render';

import { UpdateAvailableButton } from './UpdateAvailableButton';

vi.mock('@/hooks/useCurrentUser');
vi.mock('@/hooks/useLatestVersion');

const user = (overrides: Partial<MeResponse> = {}): MeResponse => ({
  id: 'user-1',
  name: 'Test User',
  email: 'user@example.com',
  requirePasswordChange: false,
  isAdmin: true,
  permissions: [],
  ...overrides,
});

const latestVersion = (overrides: Partial<LatestVersionDto> = {}): LatestVersionDto => ({
  currentVersion: '1.0.0',
  latestVersion: '1.1.0',
  isUpdateAvailable: true,
  name: 'Haven 1.1.0',
  htmlUrl: 'https://github.com/example/haven/releases/tag/v1.1.0',
  body: '## Changelog\n- Added things',
  prerelease: false,
  publishedAt: '2026-01-15T00:00:00Z',
  ...overrides,
});

function mockLatestVersion(data: LatestVersionDto | undefined) {
  vi.mocked(useLatestVersion).mockReturnValue({ data } as ReturnType<typeof useLatestVersion>);
}

describe('UpdateAvailableButton', () => {
  it('renders nothing when the user is not an admin', () => {
    vi.mocked(useCurrentUser).mockReturnValue(user({ isAdmin: false }));
    mockLatestVersion(latestVersion());

    render(<UpdateAvailableButton />);

    expect(screen.queryByRole('button', { name: 'Update Available' })).not.toBeInTheDocument();
  });

  it('renders nothing when there is no update available', () => {
    vi.mocked(useCurrentUser).mockReturnValue(user());
    mockLatestVersion(latestVersion({ isUpdateAvailable: false }));

    render(<UpdateAvailableButton />);

    expect(screen.queryByRole('button', { name: 'Update Available' })).not.toBeInTheDocument();
  });

  it('renders nothing while the latest version has not loaded yet', () => {
    vi.mocked(useCurrentUser).mockReturnValue(user());
    mockLatestVersion(undefined);

    render(<UpdateAvailableButton />);

    expect(screen.queryByRole('button', { name: 'Update Available' })).not.toBeInTheDocument();
  });

  it('shows the update button for admins when an update is available', () => {
    vi.mocked(useCurrentUser).mockReturnValue(user());
    mockLatestVersion(latestVersion());

    render(<UpdateAvailableButton />);

    expect(screen.getByRole('button', { name: 'Update Available' })).toBeInTheDocument();
  });

  it('opens a modal with the changelog and a GitHub link when clicked', async () => {
    const clickUser = userEvent.setup();
    vi.mocked(useCurrentUser).mockReturnValue(user());
    mockLatestVersion(latestVersion());

    render(<UpdateAvailableButton />);

    expect(screen.queryByText(/Changelog/)).not.toBeInTheDocument();

    await clickUser.click(screen.getByRole('button', { name: 'Update Available' }));

    expect(screen.getByText(/Added things/)).toBeInTheDocument();
    expect(screen.getByText('1.0.0')).toBeInTheDocument();
    expect(screen.getByText('Haven 1.1.0')).toBeInTheDocument();

    const githubLink = screen.getByRole('link', { name: 'View on GitHub' });
    expect(githubLink).toHaveAttribute(
      'href',
      'https://github.com/example/haven/releases/tag/v1.1.0'
    );
  });
});
