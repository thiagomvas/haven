import { render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { EnvironmentVariableDto } from '@/api/types';

import { EnvironmentVariablesCard } from './EnvironmentVariablesCard';

const vars: EnvironmentVariableDto[] = [
  { key: 'NODE_ENV', value: 'production', scope: 'Service' },
  { key: 'API_URL', value: 'https://api.example.com', scope: 'Environment' },
];

describe('EnvironmentVariablesCard', () => {
  it('renders variables in a table with key, value and scope columns', () => {
    render(<EnvironmentVariablesCard variables={vars} totalEnvVars={vars.length} />);

    expect(screen.getByText('NODE_ENV')).toBeInTheDocument();
    expect(screen.getByText('production')).toBeInTheDocument();
    expect(screen.getByText('Service')).toBeInTheDocument();
  });

  it('shows the "view all" button only when there are more variables than shown', () => {
    const onViewAll = vi.fn();
    render(<EnvironmentVariablesCard variables={vars} totalEnvVars={10} onViewAll={onViewAll} />);

    expect(screen.getByRole('button', { name: /view all/i })).toBeInTheDocument();
  });

  it('hides the "view all" button when everything is already shown', () => {
    const onViewAll = vi.fn();
    render(
      <EnvironmentVariablesCard variables={vars} totalEnvVars={vars.length} onViewAll={onViewAll} />
    );

    expect(screen.queryByRole('button', { name: /view all/i })).not.toBeInTheDocument();
  });

  it('renders an empty state when there are no variables', () => {
    render(<EnvironmentVariablesCard variables={[]} totalEnvVars={0} />);

    expect(screen.getByText('No items found')).toBeInTheDocument();
  });
});
