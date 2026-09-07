import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { SelectInput } from './SelectInput';

const options = [
  { value: 'admin', label: 'Admin' },
  { value: 'member', label: 'Member' },
];

describe('SelectInput', () => {
  it('shows the placeholder when no value is selected', () => {
    render(
      <SelectInput options={options} value="" onChange={vi.fn()} placeholder="Select a role…" />
    );

    expect(screen.getByRole('button')).toHaveTextContent('Select a role…');
  });

  it("shows the selected option's label", () => {
    render(<SelectInput options={options} value="member" onChange={vi.fn()} />);

    expect(screen.getByRole('button')).toHaveTextContent('Member');
  });

  it('opens the dropdown and lists the options on click', async () => {
    const user = userEvent.setup();
    render(<SelectInput options={options} value="" onChange={vi.fn()} />);

    await user.click(screen.getByRole('button'));

    expect(screen.getByRole('button', { name: 'Admin' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Member' })).toBeInTheDocument();
  });

  it('calls onChange with the picked option', async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<SelectInput options={options} value="" onChange={onChange} />);

    await user.click(screen.getByRole('button', { name: /select/i }));
    await user.click(screen.getByRole('button', { name: 'Admin' }));

    expect(onChange).toHaveBeenCalledWith('admin');
  });

  it('does not open when disabled', async () => {
    const user = userEvent.setup();
    render(<SelectInput options={options} value="" onChange={vi.fn()} disabled />);

    const trigger = screen.getByRole('button');
    expect(trigger).toBeDisabled();

    await user.click(trigger);

    expect(screen.queryByRole('button', { name: 'Admin' })).not.toBeInTheDocument();
  });
});
