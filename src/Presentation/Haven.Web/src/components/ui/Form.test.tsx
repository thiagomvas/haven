import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { Form, FormInput, FormLabel, FormSelect } from './Form';

describe('Form', () => {
  it('calls onSubmit when submitted', async () => {
    const user = userEvent.setup();
    const onSubmit = vi.fn(e => e.preventDefault());
    render(
      <Form onSubmit={onSubmit}>
        <button type="submit">Submit</button>
      </Form>
    );

    await user.click(screen.getByRole('button', { name: 'Submit' }));

    expect(onSubmit).toHaveBeenCalledOnce();
  });

  it('disables all fields while isLoading', () => {
    render(
      <Form onSubmit={vi.fn()} isLoading>
        <input aria-label="Name" />
      </Form>
    );

    expect(screen.getByLabelText('Name')).toBeDisabled();
  });
});

describe('FormLabel', () => {
  it('shows a required marker when required', () => {
    render(<FormLabel required>Name</FormLabel>);

    expect(screen.getByText('*')).toBeInTheDocument();
    expect(screen.queryByText('Optional')).not.toBeInTheDocument();
  });

  it('shows "Optional" when not required', () => {
    render(<FormLabel>Name</FormLabel>);

    expect(screen.getByText('Optional')).toBeInTheDocument();
  });

  it('hides both markers when readOnly', () => {
    render(<FormLabel readOnly>Name</FormLabel>);

    expect(screen.queryByText('*')).not.toBeInTheDocument();
    expect(screen.queryByText('Optional')).not.toBeInTheDocument();
  });
});

describe('FormInput', () => {
  it('shows the explicit error prop', () => {
    render(<FormInput aria-label="Name" error="Required" />);

    expect(screen.getByText('Required')).toBeInTheDocument();
  });

  it('shows the error looked up from fieldErrors by fieldName', () => {
    render(<FormInput aria-label="Name" fieldName="name" fieldErrors={{ name: 'Too short' }} />);

    expect(screen.getByText('Too short')).toBeInTheDocument();
  });

  it('renders no error when neither error nor a matching fieldError is present', () => {
    const { container } = render(<FormInput aria-label="Name" fieldName="name" fieldErrors={{}} />);

    expect(container.querySelectorAll('input')).toHaveLength(1);
    expect(container.querySelectorAll('div')).toHaveLength(0);
  });
});

describe('FormSelect', () => {
  it('renders its options and the looked-up field error', () => {
    render(
      <FormSelect aria-label="Role" fieldName="role" fieldErrors={{ role: 'Pick a role' }}>
        <option value="admin">Admin</option>
        <option value="member">Member</option>
      </FormSelect>
    );

    expect(screen.getByRole('option', { name: 'Admin' })).toBeInTheDocument();
    expect(screen.getByRole('option', { name: 'Member' })).toBeInTheDocument();
    expect(screen.getByText('Pick a role')).toBeInTheDocument();
  });
});
