import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { Modal } from './Modal';

describe('Modal', () => {
  it('renders nothing when closed', () => {
    render(
      <Modal isOpen={false} onClose={vi.fn()} title="Title">
        Content
      </Modal>
    );

    expect(screen.queryByText('Title')).not.toBeInTheDocument();
  });

  it('renders title, description, content, and footer when open', () => {
    render(
      <Modal
        isOpen
        onClose={vi.fn()}
        title="Delete project"
        description="This cannot be undone"
        footer={<button>Confirm</button>}
      >
        Are you sure?
      </Modal>
    );

    expect(screen.getByText('Delete project')).toBeInTheDocument();
    expect(screen.getByText('This cannot be undone')).toBeInTheDocument();
    expect(screen.getByText('Are you sure?')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Confirm' })).toBeInTheDocument();
  });

  it('renders the error alert when an error is provided', () => {
    render(
      <Modal isOpen onClose={vi.fn()} error="Something went wrong">
        Content
      </Modal>
    );

    expect(screen.getByText('Something went wrong')).toBeInTheDocument();
  });

  it('calls onClose when the backdrop is clicked', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <Modal isOpen onClose={onClose} title="Title">
        Content
      </Modal>
    );

    // header -> modal -> backdrop
    const backdrop = screen.getByText('Title').parentElement!.parentElement!.parentElement!;
    await user.click(backdrop);

    expect(onClose).toHaveBeenCalled();
  });

  it('does not call onClose when clicking inside the modal content', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <Modal isOpen onClose={onClose} title="Title">
        Content
      </Modal>
    );

    await user.click(screen.getByText('Content'));

    expect(onClose).not.toHaveBeenCalled();
  });

  it('calls onClose on Escape by default', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <Modal isOpen onClose={onClose} title="Title">
        Content
      </Modal>
    );

    await user.keyboard('{Escape}');

    expect(onClose).toHaveBeenCalledOnce();
  });

  it('does not call onClose on Escape when closeOnEscape is false', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <Modal isOpen onClose={onClose} title="Title" closeOnEscape={false}>
        Content
      </Modal>
    );

    await user.keyboard('{Escape}');

    expect(onClose).not.toHaveBeenCalled();
  });

  it('closes via the close button', async () => {
    const user = userEvent.setup();
    const onClose = vi.fn();
    render(
      <Modal isOpen onClose={onClose} title="Title">
        Content
      </Modal>
    );

    await user.click(screen.getByRole('button', { name: 'Close modal' }));

    expect(onClose).toHaveBeenCalledOnce();
  });
});
