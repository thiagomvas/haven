import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { usePermission } from '@/hooks/usePermission';

import { PermissionGuard } from './PermissionGuard';

vi.mock('@/hooks/usePermission');

describe('PermissionGuard', () => {
  it('renders children when the user has the permission', () => {
    vi.mocked(usePermission).mockReturnValue(true);

    render(
      <PermissionGuard permission="projects.create">
        <button>Create project</button>
      </PermissionGuard>
    );

    expect(screen.getByRole('button', { name: 'Create project' })).toBeInTheDocument();
  });

  it('renders nothing when the user lacks the permission', () => {
    vi.mocked(usePermission).mockReturnValue(false);

    render(
      <PermissionGuard permission="projects.create">
        <button>Create project</button>
      </PermissionGuard>
    );

    expect(screen.queryByRole('button', { name: 'Create project' })).not.toBeInTheDocument();
  });

  it('checks the permission passed in props', () => {
    vi.mocked(usePermission).mockReturnValue(true);

    render(
      <PermissionGuard permission="projects.delete">
        <span>Delete</span>
      </PermissionGuard>
    );

    expect(usePermission).toHaveBeenCalledWith('projects.delete');
  });
});
