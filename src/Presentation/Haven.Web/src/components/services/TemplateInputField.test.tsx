import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { TemplateInputFieldDto } from '@/api/types';

import { TemplateInputField } from './TemplateInputField';

function makeField(overrides: Partial<TemplateInputFieldDto> = {}): TemplateInputFieldDto {
  return {
    key: 'postgres_password',
    type: 'Text',
    immutable: false,
    ...overrides,
  };
}

describe('TemplateInputField', () => {
  it('renders a text input and reports changes', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TemplateInputField field={makeField({ label: 'Password' })} onChange={onChange} />);

    const input = screen.getByLabelText(/Password/);
    await user.type(input, 'a');

    expect(onChange).toHaveBeenCalledWith('a');
  });

  it('renders a password input for Secret fields', () => {
    render(
      <TemplateInputField
        field={makeField({ type: 'Secret', label: 'Password' })}
        onChange={vi.fn()}
      />
    );

    expect(screen.getByLabelText(/Password/)).toHaveAttribute('type', 'password');
  });

  it('renders a select with the provided options for Select fields', () => {
    render(
      <TemplateInputField
        field={makeField({
          type: 'Select',
          label: 'Version',
          options: ['15', '16', '17'],
          defaultValue: '17',
        })}
        onChange={vi.fn()}
      />
    );

    expect(screen.getByText('17')).toBeInTheDocument();
  });

  it('marks the field as required when there is no default value', () => {
    render(<TemplateInputField field={makeField({ label: 'Password' })} onChange={vi.fn()} />);

    expect(screen.getByText('*')).toBeInTheDocument();
  });

  it('does not mark the field as required when a default value is present', () => {
    render(
      <TemplateInputField
        field={makeField({ label: 'User', defaultValue: 'postgres' })}
        onChange={vi.fn()}
      />
    );

    expect(screen.queryByText('*')).not.toBeInTheDocument();
  });

  it('pre-fills the visible value with the default', () => {
    render(
      <TemplateInputField
        field={makeField({ label: 'User', defaultValue: 'postgres' })}
        onChange={vi.fn()}
      />
    );

    expect(screen.getByLabelText(/User/)).toHaveValue('postgres');
  });
});
