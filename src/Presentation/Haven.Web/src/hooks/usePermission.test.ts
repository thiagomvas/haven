import { renderHook } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import type { MeResponse } from '@/api/auth';

import { useCurrentUser } from './useCurrentUser';
import { usePermission } from './usePermission';

vi.mock('./useCurrentUser');

const user = (overrides: Partial<MeResponse> = {}): MeResponse => ({
  id: 'user-1',
  name: 'Test User',
  email: 'user@example.com',
  requirePasswordChange: false,
  isAdmin: false,
  permissions: [],
  ...overrides,
});

describe('usePermission', () => {
  it('returns false when there is no current user', () => {
    vi.mocked(useCurrentUser).mockReturnValue(null);

    const { result } = renderHook(() => usePermission('projects.read'));

    expect(result.current).toBe(false);
  });

  it('returns true for any permission when the user is an admin', () => {
    vi.mocked(useCurrentUser).mockReturnValue(user({ isAdmin: true, permissions: [] }));

    const { result } = renderHook(() => usePermission('projects.delete'));

    expect(result.current).toBe(true);
  });

  it('returns true when the permission is in the user permission list', () => {
    vi.mocked(useCurrentUser).mockReturnValue(user({ permissions: ['projects.read'] }));

    const { result } = renderHook(() => usePermission('projects.read'));

    expect(result.current).toBe(true);
  });

  it('returns false when the permission is not in the user permission list', () => {
    vi.mocked(useCurrentUser).mockReturnValue(user({ permissions: ['projects.read'] }));

    const { result } = renderHook(() => usePermission('projects.delete'));

    expect(result.current).toBe(false);
  });
});
