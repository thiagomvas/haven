import { useEffect, useState } from 'react';
import { Search } from 'lucide-react';
import { serviceRegistryApi } from '@/api/serviceRegistry';
import { PagedResult, PagedServiceRegistryEntryDto } from '@/api/types';
import { Badge } from '@/components/ui/Badge';
import { Card, CardContent, CardHeader } from '@/components/ui/Card';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { Input } from '@/components/ui/Input';
import { Spinner } from '@/components/ui/Spinner';
import {
  Row,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/layout';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import styles from './ServiceRegistryPage.module.css';
import { HealthIndicator } from '@/components/ui/HealthIndicator';

const PAGE_SIZE = 20;

function statusVariant(status: string): 'success' | 'danger' | 'warning' | 'default' {
  switch (status) {
    case 'Running': return 'success';
    case 'Stopped': return 'danger';
    case 'Degraded': return 'warning';
    default: return 'default';
  }
}

function formatPorts(ports: PagedServiceRegistryEntryDto['ports']): string {
  if (!ports.length) return '—';
  return ports.map(p => (p.hostPort ? `${p.hostPort}:${p.containerPort}` : `${p.containerPort}`)).join(', ');
}

export function ServiceRegistryPage() {
  const [data, setData] = useState<PagedResult<PagedServiceRegistryEntryDto> | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useSetBreadcrumbs([{ label: 'Service Registry' }]);

  useEffect(() => {
    const id = setTimeout(() => {
      setDebouncedSearch(search);
      setCurrentPage(1);
    }, 300);
    return () => clearTimeout(id);
  }, [search]);

  useEffect(() => {
    const load = async () => {
      try {
        setLoading(true);
        setError(null);
        const result = await serviceRegistryApi.getAll({
          pageNumber: currentPage,
          pageSize: PAGE_SIZE,
          search: debouncedSearch || undefined,
        });
        setData(result);
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load service registry.');
      } finally {
        setLoading(false);
      }
    };
    load();
  }, [currentPage, debouncedSearch]);

  return (
    <Stack gap="6" className={styles.container}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Service Registry</h1>
          <p className={styles.subtitle}>Live runtime state for all registered services.</p>
        </div>
      </div>

      <div className={styles.toolbar}>
        <div className={styles.searchWrapper}>
          <Search size={16} className={styles.searchIcon} />
          <Input
            placeholder="Search..."
            value={search}
            onChange={e => setSearch(e.target.value)}
            className={styles.searchInput}
          />
        </div>
      </div>

      {error && <ErrorAlert message={error} variant="block" />}

      {!error && loading && (
        <div className={styles.spinner}>
          <Spinner />
          <p>Loading...</p>
        </div>
      )}

      {!error && !loading && (
        <Card>
          <CardHeader>
            <h2 className={styles.sectionTitle}>Entries</h2>
          </CardHeader>
          <CardContent className={styles.tableContent}>
            {!data?.items.length ? (
              <p className={styles.emptyState}>No registry entries found.</p>
            ) : (
              <Table hoverable striped>
                <TableHead>
                  <TableRow isHeader>
                    <TableHeader>Container</TableHeader>
                    <TableHeader>IP Address</TableHeader>
                    <TableHeader>Ports</TableHeader>
                    <TableHeader>Registered</TableHeader>
                    <TableHeader>Updated</TableHeader>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.items.map(entry => (
                    <TableRow key={entry.id}>
                      <TableCell>
                        <Row gap="5" align="center">
                        <HealthIndicator useTooltip health={entry.status} />
                        <Stack gap="1">
                          <span className={styles.containerName}>{entry.containerName ?? '—'}</span>
                          <CodeSpan className={styles.serviceId}>{entry.serviceId}</CodeSpan>
                        </Stack>
                        </Row>
                      </TableCell>
                      <TableCell variant="mono">{entry.ipAddress ?? '—'}</TableCell>
                      <TableCell variant="mono">{formatPorts(entry.ports)}</TableCell>
                      <TableCell variant="muted" nowrap>
                        {new Date(entry.registeredAt).toLocaleString()}
                      </TableCell>
                      <TableCell variant="muted" nowrap>
                        {new Date(entry.updatedAt).toLocaleString()}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </CardContent>
        </Card>
      )}

      {!error && data && data.totalPages > 1 && (
        <div className={styles.pagination}>
          <button
            className={styles.paginationButton}
            onClick={() => setCurrentPage(p => Math.max(1, p - 1))}
            disabled={!data.hasPreviousPage}
          >
            Previous
          </button>
          <span className={styles.paginationInfo}>
            Page {data.pageNumber} of {data.totalPages}
          </span>
          <button
            className={styles.paginationButton}
            onClick={() => setCurrentPage(p => p + 1)}
            disabled={!data.hasNextPage}
          >
            Next
          </button>
        </div>
      )}
    </Stack>
  );
}
