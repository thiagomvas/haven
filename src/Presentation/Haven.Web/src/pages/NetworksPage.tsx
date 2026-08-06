import { ChevronDown, ChevronRight, ChevronsDownUp, ChevronsUpDown } from 'lucide-react';
import { Fragment, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';

import { NetworkDto, NetworkServiceDto, NetworkType } from '@/api/types';
import {
  Grid,
  Row,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/layout';
import { Button } from '@/components/ui/Button';
import { Card, CardContent, CardHeader } from '@/components/ui/Card';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { Spinner } from '@/components/ui/Spinner';
import { Tooltip } from '@/components/ui/Tooltip';
import { useNetworks } from '@/hooks/useNetworks';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import styles from '@/styles/pages/NetworksPage.module.css';

const PAGE_SIZE = 20;
const CONNECTIONS_PREVIEW_LIMIT = 8;

const NETWORK_TYPE_ORDER: NetworkType[] = ['ProjectEnvironment', 'Shared', 'External'];

function groupByType(items: NetworkDto[]): Map<NetworkType, NetworkDto[]> {
  const groups = new Map<NetworkType, NetworkDto[]>();
  for (const type of NETWORK_TYPE_ORDER) {
    groups.set(type, []);
  }
  for (const network of items) {
    groups.get(network.type)?.push(network);
  }
  return groups;
}

function ServiceDot({ service }: { service: NetworkServiceDto }) {
  return (
    <Tooltip content={`${service.name} · ${service.status}`} direction="above">
      <HealthIndicator health={service.status} />
    </Tooltip>
  );
}

function ConnectionsPreview({ services }: { services: NetworkServiceDto[] }) {
  if (!services.length) {
    return <span className={styles.noConnections}>—</span>;
  }

  const visible = services.slice(0, CONNECTIONS_PREVIEW_LIMIT);
  const overflow = services.slice(CONNECTIONS_PREVIEW_LIMIT);

  return (
    <Row gap="1" align="center" className={styles.connectionsPreview}>
      {visible.map(service => (
        <ServiceDot key={service.id} service={service} />
      ))}
      {overflow.length > 0 && (
        <Tooltip content={overflow.map(s => s.name).join(', ')} direction="above">
          <span className={styles.connectionsOverflow}>+{overflow.length}</span>
        </Tooltip>
      )}
      <span className={styles.connectionsCount}>{services.length}</span>
    </Row>
  );
}

export function NetworksPage() {
  const { t } = useTranslation('networks');
  const [currentPage, setCurrentPage] = useState(1);
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  useSetBreadcrumbs([{ label: t('title') }]);

  const { data, isLoading, isError } = useNetworks({
    pageNumber: currentPage,
    pageSize: PAGE_SIZE,
  });

  const items = useMemo(() => data?.items ?? [], [data]);
  const groups = useMemo(() => groupByType(items), [items]);

  const toggleExpanded = (id: string) => {
    setExpanded(prev => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const expandAll = () => setExpanded(new Set(items.filter(n => n.services.length).map(n => n.id)));
  const collapseAll = () => setExpanded(new Set());

  const networkScope = (network: NetworkDto): string => {
    if (network.projectName && network.environmentName) {
      return `${network.projectName} / ${network.environmentName}`;
    }
    if (network.type === 'Shared') return t('scope.shared');
    return t('scope.external');
  };

  return (
    <Stack gap="5" className={styles.container}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
        {!isLoading && items.length > 0 && (
          <Row gap="2">
            <Button
              variant="ghost"
              size="sm"
              icon={<ChevronsUpDown size={14} />}
              onClick={expandAll}
            >
              {t('actions.expandAll')}
            </Button>
            <Button
              variant="ghost"
              size="sm"
              icon={<ChevronsDownUp size={14} />}
              onClick={collapseAll}
            >
              {t('actions.collapseAll')}
            </Button>
          </Row>
        )}
      </div>

      {isError && <ErrorAlert message={t('error')} variant="block" />}

      {!isError && isLoading && (
        <div className={styles.spinner}>
          <Spinner />
          <p>{t('loading')}</p>
        </div>
      )}

      {!isError && !isLoading && !items.length && <p className={styles.emptyState}>{t('empty')}</p>}

      {!isError &&
        !isLoading &&
        items.length > 0 &&
        NETWORK_TYPE_ORDER.map(type => {
          const groupItems = groups.get(type) ?? [];
          if (!groupItems.length) return null;

          return (
            <Card key={type} className={styles.groupCard} padding={0}>
              <CardHeader className={`${styles.groupHeader} ${styles[`accent-${type}`]}`}>
                <span className={styles.groupTitle}>{t(`groups.${type}` as const)}</span>
                <span className={styles.groupCount}>{groupItems.length}</span>
              </CardHeader>
              <CardContent className={styles.tableContent} padding={0}>
                <Table hoverable padding="2" className={styles.table}>
                  <TableHead>
                    <TableRow isHeader>
                      <TableHeader className={styles.chevronHeader} />
                      <TableHeader>{t('table.name')}</TableHeader>
                      <TableHeader>{t('table.scope')}</TableHeader>
                      <TableHeader align="right">{t('table.connections')}</TableHeader>
                    </TableRow>
                  </TableHead>
                  <TableBody>
                    {groupItems.map(network => {
                      const isExpanded = expanded.has(network.id);
                      const hasServices = network.services.length > 0;
                      return (
                        <Fragment key={network.id}>
                          <TableRow
                            onRowClick={hasServices ? () => toggleExpanded(network.id) : undefined}
                            className={hasServices ? undefined : styles.rowInert}
                          >
                            <TableCell className={styles.chevronCell}>
                              {hasServices &&
                                (isExpanded ? (
                                  <ChevronDown size={14} className={styles.chevron} />
                                ) : (
                                  <ChevronRight size={14} className={styles.chevron} />
                                ))}
                            </TableCell>
                            <TableCell>
                              <CodeSpan copyable className={styles.nameSpan}>
                                {network.name}
                              </CodeSpan>
                            </TableCell>
                            <TableCell variant="muted">{networkScope(network)}</TableCell>
                            <TableCell align="right">
                              <ConnectionsPreview services={network.services} />
                            </TableCell>
                          </TableRow>
                          {isExpanded && hasServices && (
                            <TableRow className={styles.detailsRow}>
                              <TableCell colSpan={4}>
                                <Grid gap="2">
                                  {network.services.map(service => (
                                    <Tooltip
                                      key={service.id}
                                      content={service.status}
                                      direction="above"
                                    >
                                      <div className={styles.serviceEntry}>
                                        <HealthIndicator health={service.status} />
                                        <span className={styles.serviceName}>{service.name}</span>
                                      </div>
                                    </Tooltip>
                                  ))}
                                </Grid>
                              </TableCell>
                            </TableRow>
                          )}
                        </Fragment>
                      );
                    })}
                  </TableBody>
                </Table>
              </CardContent>
            </Card>
          );
        })}

      {!isError && data && data.totalPages > 1 && (
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
