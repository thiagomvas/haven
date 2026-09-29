import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { servicesApi } from '@/api/services';
import { ComputedOutputDto } from '@/api/types';
import { usePermission } from '@/hooks/usePermission';

import { ComputedOutputsCard } from './ComputedOutputsCard';

vi.mock('@/hooks/usePermission');
vi.mock('@/api/services');

const outputs: ComputedOutputDto[] = [
  {
    key: 'url',
    label: 'Connection URL',
    isSecret: false,
    isAvailable: true,
    preview: 'postgres://db:5432',
  },
  { key: 'apiKey', label: 'API Key', isSecret: true, isAvailable: true, preview: 'sk_***' },
  {
    key: 'pending',
    label: 'Pending Value',
    isSecret: false,
    isAvailable: false,
    unavailableReason: 'NotRunning',
  },
];

describe('ComputedOutputsCard', () => {
  it('renders label/value lines for available outputs and a message for unavailable ones', () => {
    vi.mocked(usePermission).mockReturnValue(true);

    render(
      <ComputedOutputsCard
        projectId="proj-1"
        environmentId="env-1"
        serviceId="svc-1"
        outputs={outputs}
      />
    );

    expect(screen.getByText('Connection URL')).toBeInTheDocument();
    expect(screen.getByText('postgres://db:5432')).toBeInTheDocument();
    expect(screen.getByText('sk_***')).toBeInTheDocument();
    expect(screen.getByText('Pending Value')).toBeInTheDocument();
  });

  it('reveals the real value when the reveal button is clicked', async () => {
    vi.mocked(usePermission).mockReturnValue(true);
    vi.mocked(servicesApi.getComputedOutputValue).mockResolvedValue({
      key: 'apiKey',
      value: 'sk_live_realvalue',
    });
    const user = userEvent.setup();

    render(
      <ComputedOutputsCard
        projectId="proj-1"
        environmentId="env-1"
        serviceId="svc-1"
        outputs={[outputs[1]]}
      />
    );

    expect(screen.getByText('sk_***')).toBeInTheDocument();
    await user.click(screen.getByRole('button', { name: /reveal/i }));

    await waitFor(() => expect(screen.getByText('sk_live_realvalue')).toBeInTheDocument());
  });

  it('does not show reveal/copy actions for a secret when the user lacks permission', () => {
    vi.mocked(usePermission).mockReturnValue(false);

    render(
      <ComputedOutputsCard
        projectId="proj-1"
        environmentId="env-1"
        serviceId="svc-1"
        outputs={[outputs[1]]}
      />
    );

    expect(screen.queryByRole('button', { name: /reveal/i })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: /copy/i })).not.toBeInTheDocument();
  });
});
