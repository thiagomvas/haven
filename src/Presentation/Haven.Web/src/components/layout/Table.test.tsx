import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from './Table';

describe('Table', () => {
  it('renders headers, rows, and cells', () => {
    render(
      <Table>
        <TableHead>
          <TableRow isHeader>
            <TableHeader>Name</TableHeader>
          </TableRow>
        </TableHead>
        <TableBody>
          <TableRow>
            <TableCell>Acme API</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    );

    expect(screen.getByRole('columnheader', { name: 'Name' })).toBeInTheDocument();
    expect(screen.getByRole('cell', { name: 'Acme API' })).toBeInTheDocument();
  });

  it('calls onRowClick when a row is clicked', async () => {
    const user = userEvent.setup();
    const onRowClick = vi.fn();
    render(
      <Table>
        <TableBody>
          <TableRow onRowClick={onRowClick}>
            <TableCell>Acme API</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    );

    await user.click(screen.getByRole('cell', { name: 'Acme API' }));

    expect(onRowClick).toHaveBeenCalledOnce();
  });

  it('renders the actions cell when actions are provided', () => {
    render(
      <Table>
        <TableBody>
          <TableRow actions={<button>Delete</button>}>
            <TableCell>Acme API</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    );

    expect(screen.getByRole('button', { name: 'Delete' })).toBeInTheDocument();
  });
});
