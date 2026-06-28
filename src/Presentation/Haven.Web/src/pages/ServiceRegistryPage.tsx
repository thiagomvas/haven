import { ArrowRight, ExternalLink, Search, SquareArrowOutUpRight } from 'lucide-react';
import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { serviceRegistryApi } from '@/api/serviceRegistry';
import { PagedResult } from '@/api/types';
import { ServiceRegistryEntryDto } from '@/api/types/service.types';
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
import { Badge } from '@/components/ui/Badge';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader } from '@/components/ui/Card';
import { ServiceExposureChip } from '@/components/ui/chips/serviceExposureChip';
import { ServiceTypeChip } from '@/components/ui/chips/serviceTypeChip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { Input } from '@/components/ui/Input';
import { Spinner } from '@/components/ui/Spinner';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';

import styles from './ServiceRegistryPage.module.css';

const PAGE_SIZE = 20;

function statusVariant(status: string): 'success' | 'danger' | 'warning' | 'default' {
  switch (status) {
    case 'Running':
      return 'success';
    case 'Stopped':
      return 'danger';
    case 'Degraded':
      return 'warning';
    default:
      return 'default';
  }
}

function formatUptime(startedAt: string): string {
  const seconds = Math.floor((Date.now() - new Date(startedAt).getTime()) / 1000);
  if (seconds < 60) return `${seconds}s`;
  const minutes = Math.floor(seconds / 60);
  if (minutes < 60) return `${minutes}m ${seconds % 60}s`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ${minutes % 60}m`;
  const days = Math.floor(hours / 24);
  return `${days}d ${hours % 24}h`;
}

function portHost(ipAddress?: string): string {
  if (!ipAddress || ipAddress === '0.0.0.0' || ipAddress === '::') return 'localhost';
  return ipAddress;
}

export function ServiceRegistryPage() {
  const { t } = useTranslation('serviceRegistry');
  const navigate = useNavigate();
  const [data, setData] = useState<PagedResult<ServiceRegistryEntryDto> | null>(null);
  const [currentPage, setCurrentPage] = useState(1);
  const [search, setSearch] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useSetBreadcrumbs([{ label: t('title') }]);

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
        setError(err instanceof Error ? err.message : t('error'));
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
          <h1 className={styles.title}>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
      </div>

      <div className={styles.toolbar}>
        <div className={styles.searchWrapper}>
          <Search size={16} className={styles.searchIcon} />
          <Input
            placeholder={t('search.placeholder')}
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
          <p>{t('loading')}</p>
        </div>
      )}

      {!error && !loading && (
        <Card>
          <CardHeader>
            <h2 className={styles.sectionTitle}>{t('sectionTitle')}</h2>
          </CardHeader>
          <CardContent className={styles.tableContent}>
            {!data?.items.length ? (
              <p className={styles.emptyState}>{t('empty')}</p>
            ) : (
              <Table hoverable striped>
                <TableHead>
                  <TableRow isHeader hasActionsColumn>
                    <TableHeader>{t('table.fqdn')}</TableHeader>
                    <TableHeader>{t('table.type')}</TableHeader>
                    <TableHeader>{t('table.addresses')}</TableHeader>
                    <TableHeader>{t('table.exposure')}</TableHeader>
                    <TableHeader>{t('table.uptime')}</TableHeader>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.items.map(entry => (
                    <TableRow
                      key={entry.containerName}
                      actions={
                        <Button
                          variant="text"
                          size="xs"
                          icon={<ArrowRight size={14} />}
                          onClick={() => navigate(`/services/${entry.serviceId}`)}
                        />
                      }
                    >
                      <TableCell>
                        <Row gap="5" align="center">
                          <HealthIndicator useTooltip health={entry.status} />
                          <Stack gap="1">
                            <span className={styles.containerName}>
                              {entry.containerName ?? '—'}
                            </span>
                          </Stack>
                        </Row>
                      </TableCell>
                      <TableCell>
                        <ServiceTypeChip serviceType={entry.serviceType} />
                      </TableCell>
                      <TableCell>
                        <Stack gap="1">
                          {entry.ports
                            .filter(p => p.hostPort)
                            .map((p, i) => (
                              <a
                                key={i}
                                href={`http://${portHost(p.ipAddress)}:${p.hostPort}`}
                                target="_blank"
                                rel="noreferrer"
                                className={styles.portLink}
                              >
                                <ExternalLink size={11} />
                                {portHost(p.ipAddress)}:{p.hostPort}
                                <span className={styles.portArrow}>
                                  {' '}
                                  <ArrowRight size={11} /> :{p.containerPort}
                                </span>
                              </a>
                            ))}
                          {entry.ipAddress &&
                            entry.ports.map((p, i) => (
                              <a
                                key={`cip-${i}`}
                                href={`http://${entry.ipAddress}:${p.containerPort}`}
                                target="_blank"
                                rel="noreferrer"
                                className={styles.portLink}
                              >
                                <ExternalLink size={11} />
                                {entry.ipAddress}:{p.containerPort}
                              </a>
                            ))}
                          {!entry.ports.length && !entry.ipAddress && '—'}
                        </Stack>
                      </TableCell>
                      <TableCell variant="muted" nowrap>
                        <ServiceExposureChip exposureMode={entry.exposureMode} />
                      </TableCell>
                      <TableCell variant="muted" nowrap>
                        {entry.startedAt ? formatUptime(entry.startedAt) : '—'}
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
            {t('pagination.previous')}
          </button>
          <span className={styles.paginationInfo}>
            {t('labels.pageOf', { ns: 'common', current: data.pageNumber, total: data.totalPages })}
          </span>
          <button
            className={styles.paginationButton}
            onClick={() => setCurrentPage(p => p + 1)}
            disabled={!data.hasNextPage}
          >
            {t('pagination.next')}
          </button>
        </div>
      )}
    </Stack>
  );
}
