import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { Input } from './Input';

describe('Input', () => {
  it('associates the label with the input via id', () => {
    render(<Input id="name" label="Name" />);

    expect(screen.getByLabelText('Name')).toBe(screen.getByRole('textbox'));
  });

  it('renders no label when none is provided', () => {
    render(<Input placeholder="Search" />);

    expect(screen.queryByRole('label')).not.toBeInTheDocument();
  });

  it('shows the error message when error is set', () => {
    render(<Input label="Name" error="Name is required" />);

    expect(screen.getByText('Name is required')).toBeInTheDocument();
  });

  it('renders no error message when error is absent', () => {
    render(<Input label="Name" />);

    expect(screen.queryByText(/required/)).not.toBeInTheDocument();
  });

  it('forwards native input props such as value and onChange', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<Input id="name" label="Name" value="" onChange={onChange} />);

    await user.type(screen.getByLabelText('Name'), 'a');

    expect(onChange).toHaveBeenCalled();
  });
});
