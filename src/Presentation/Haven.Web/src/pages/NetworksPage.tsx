import {
  ChevronDown,
  ChevronRight,
  ChevronsDownUp,
  ChevronsUpDown,
  ExternalLink,
  Link2,
} from 'lucide-react';
import { Fragment, ReactNode, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';

import { NetworkDto, NetworkServiceDto, NetworkType } from '@/api/types';
import {
  Row,
  Spacer,
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
import { Chip } from '@/components/ui/Chip';
import { CodeSpan } from '@/components/ui/CodeSpan';
import { Divider } from '@/components/ui/Divider';
import { ErrorAlert } from '@/components/ui/ErrorAlert';
import { HealthIndicator } from '@/components/ui/HealthIndicator';
import { Label } from '@/components/ui/Label';
import { SelectInput } from '@/components/ui/SelectInput';
import { Spinner } from '@/components/ui/Spinner';
import { useNetworks } from '@/hooks/useNetworks';
import { useSetBreadcrumbs } from '@/hooks/useSetBreadcrumbs';
import styles from '@/styles/pages/NetworksPage.module.css';

const NETWORK_TYPE_ORDER: NetworkType[] = ['ProjectEnvironment', 'Shared', 'External'];

interface ProjectGroup {
  projectId: string;
  projectName: string;
  networks: NetworkDto[];
}

function groupProjectEnvironmentNetworks(items: NetworkDto[]): ProjectGroup[] {
  const map = new Map<string, ProjectGroup>();
  for (const network of items) {
    if (network.type !== 'ProjectEnvironment' || !network.projectId) continue;
    const key = network.projectId;
    if (!map.has(key)) {
      map.set(key, {
        projectId: key,
        projectName: network.projectName ?? '—',
        networks: [],
      });
    }
    map.get(key)!.networks.push(network);
  }
  return [...map.values()].sort((a, b) => a.projectName.localeCompare(b.projectName));
}

function formatDate(value: string): string {
  return new Date(value).toLocaleDateString(undefined, {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

function ServicesSubTable({
  services,
  showProject,
}: {
  services: NetworkServiceDto[];
  showProject: boolean;
}) {
  const { t } = useTranslation('networks');
  const navigate = useNavigate();

  return (
    <div className={styles.nestedTableWrapper}>
      <Table compact hoverable padding="2" className={styles.nestedTable}>
        <TableHead>
          <TableRow isHeader>
            <TableHeader>{t('table.serviceName')}</TableHeader>
            {showProject && <TableHeader>{t('table.project')}</TableHeader>}
            <TableHeader>{t('table.ipAddress')}</TableHeader>
          </TableRow>
        </TableHead>
        <TableBody>
          {services.map(service => (
            <TableRow key={service.id} onRowClick={() => navigate(`/services/${service.id}`)}>
              <TableCell>
                <Row gap="2" align="center">
                  <HealthIndicator health={service.status} />
                  <span className={styles.serviceName}>{service.name}</span>
                </Row>
              </TableCell>
              {showProject && <TableCell variant="muted">{service.projectName ?? '—'}</TableCell>}
              <TableCell variant="mono">{service.ipAddress ?? '—'}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </div>
  );
}

function Section({
  title,
  count,
  accent,
  children,
}: {
  title: string;
  count: number;
  accent: NetworkType;
  children: ReactNode;
}) {
  return (
    <Stack gap="3">
      <Row>
        <Label size="xl" weight="semibold" variant="primary">
          {title}
        </Label>
        <Chip content={count} size="sm" />
      </Row>
      <Stack gap="4">{children}</Stack>
    </Stack>
  );
}

function ProjectNetworksCard({
  group,
  expanded,
  onToggle,
}: {
  group: ProjectGroup;
  expanded: Set<string>;
  onToggle: (id: string) => void;
}) {
  const { t } = useTranslation('networks');
  const navigate = useNavigate();

  return (
    <Card padding={0}>
      <CardHeader>
        <Row>
          <Label variant="primary" size="lg" weight="semibold">
            {group.projectName}
          </Label>
          <Spacer expand direction="horizontal" />
          <Chip content={group.networks.length} size="sm" />
        </Row>
      </CardHeader>
      <CardContent>
        <Table compact striped hoverable padding="2" className={styles.networksTable}>
          <TableHead>
            <TableRow isHeader>
              <TableHeader>{t('table.name')}</TableHeader>
              <TableHeader>{t('table.environment')}</TableHeader>
              <TableHeader>{t('table.subnet')}</TableHeader>
              <TableHeader>{t('table.gateway')}</TableHeader>
              <TableHeader>{t('table.services')}</TableHeader>
            </TableRow>
          </TableHead>
          <TableBody>
            {group.networks.map(network => (
              <Fragment key={network.id}>
                <TableRow>
                  <TableCell>
                    <CodeSpan copyable className={styles.nameSpan}>
                      {network.name}
                    </CodeSpan>
                  </TableCell>
                  <TableCell variant="default">
                    {network.environmentName && network.environmentId && (
                      <Button
                        variant="text"
                        size="sm"
                        align="left"
                        onClick={() => navigate(`/environments/${network.environmentId}`)}
                      >
                        {network.environmentName}
                        <ExternalLink size={12} />
                      </Button>
                    )}
                  </TableCell>
                  <TableCell variant="mono">{network.subnet ?? '—'}</TableCell>
                  <TableCell variant="mono">{network.gateway ?? '—'}</TableCell>
                  <TableCell>
                    <Row>
                      <Label variant="secondary" size="md" weight="medium">
                        {network.services.length}
                      </Label>
                      <Spacer expand direction="horizontal" />
                      <Button
                        variant="ghost"
                        size="sm"
                        icon={
                          expanded.has(network.id) ? (
                            <ChevronDown size={14} />
                          ) : (
                            <ChevronRight size={14} />
                          )
                        }
                        onClick={() => onToggle(network.id)}
                      >
                        {t('table.services')} ({network.services.length})
                      </Button>
                    </Row>
                  </TableCell>
                </TableRow>
                {expanded.has(network.id) && (
                  <TableRow>
                    <TableCell colSpan={6} className={styles.nestedRow}>
                      <ServicesSubTable services={network.services} showProject={false} />
                    </TableCell>
                  </TableRow>
                )}
              </Fragment>
            ))}
          </TableBody>
        </Table>
      </CardContent>
    </Card>
  );
}

function NetworkMetaCard({
  network,
  isExpanded,
  onToggle,
  showProjectColumn,
}: {
  network: NetworkDto;
  isExpanded: boolean;
  onToggle: () => void;
  showProjectColumn: boolean;
}) {
  const { t } = useTranslation('networks');
  const hasServices = network.services.length > 0;

  return (
    <Card className={styles.networkCard} padding={0}>
      <CardHeader className={styles.networkCardHeader}>
        <CodeSpan copyable className={styles.nameSpan}>
          {network.name}
        </CodeSpan>
      </CardHeader>
      <CardContent>
        <Row gap="6" wrap className={styles.metaStrip}>
          <div className={styles.metaItem}>
            <span className={styles.metaLabel}>{t('meta.serviceCount')}</span>
            <span className={styles.metaValue}>{network.serviceCount}</span>
          </div>
          <div className={styles.metaItem}>
            <span className={styles.metaLabel}>{t('meta.subnet')}</span>
            <span className={styles.metaValue}>{network.subnet ?? '—'}</span>
          </div>
          <div className={styles.metaItem}>
            <span className={styles.metaLabel}>{t('meta.gateway')}</span>
            <span className={styles.metaValue}>{network.gateway ?? '—'}</span>
          </div>
          <div className={styles.metaItem}>
            <span className={styles.metaLabel}>{t('meta.createdAt')}</span>
            <span className={styles.metaValue}>{formatDate(network.createdAt)}</span>
          </div>
        </Row>

        {hasServices ? (
          <Stack gap="2" className={styles.serviceListSection}>
            <Button
              variant="ghost"
              size="sm"
              icon={isExpanded ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
              onClick={onToggle}
            >
              {t('table.services')} ({network.services.length})
            </Button>
            {isExpanded && (
              <ServicesSubTable services={network.services} showProject={showProjectColumn} />
            )}
          </Stack>
        ) : (
          <p className={styles.emptyServicesText}>{t('emptyServices')}</p>
        )}
      </CardContent>
    </Card>
  );
}

export function NetworksPage() {
  const { t } = useTranslation('networks');
  const [typeFilter, setTypeFilter] = useState<NetworkType | ''>('');
  const [expanded, setExpanded] = useState<Set<string>>(new Set());

  useSetBreadcrumbs([{ label: t('title') }]);

  const { data, isLoading, isError } = useNetworks({
    type: typeFilter || undefined,
  });

  const handleTypeFilterChange = (value: string) => {
    setTypeFilter(value as NetworkType | '');
  };

  const typeFilterOptions = NETWORK_TYPE_ORDER.map(type => ({
    value: type,
    label: t(`filterType.${type}` as const),
  }));

  const items = useMemo(() => data ?? [], [data]);
  const envGroups = useMemo(() => groupProjectEnvironmentNetworks(items), [items]);
  const sharedNetworks = useMemo(() => items.filter(n => n.type === 'Shared'), [items]);
  const externalNetworks = useMemo(() => items.filter(n => n.type === 'External'), [items]);

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

  return (
    <Stack gap="5" className={styles.container}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>{t('title')}</h1>
          <p className={styles.subtitle}>{t('subtitle')}</p>
        </div>
        <Row gap="3" align="center">
          <SelectInput
            options={typeFilterOptions}
            value={typeFilter}
            onChange={handleTypeFilterChange}
            placeholder={t('filterType.all')}
          />
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
        </Row>
      </div>

      {isError && <ErrorAlert message={t('error')} variant="block" />}

      {!isError && isLoading && (
        <div className={styles.spinner}>
          <Spinner />
          <p>{t('loading')}</p>
        </div>
      )}

      {!isError && !isLoading && !items.length && (
        <p className={styles.emptyState}>{typeFilter ? t('emptyFiltered') : t('empty')}</p>
      )}

      {!isError && !isLoading && items.length > 0 && (
        <>
          {envGroups.length > 0 && (
            <Section
              title={t('groups.ProjectEnvironment')}
              count={envGroups.reduce((sum, g) => sum + g.networks.length, 0)}
              accent="ProjectEnvironment"
            >
              {envGroups.map(group => (
                <ProjectNetworksCard
                  key={group.projectId}
                  group={group}
                  expanded={expanded}
                  onToggle={toggleExpanded}
                />
              ))}
            </Section>
          )}

          {sharedNetworks.length > 0 && (
            <Section title={t('groups.Shared')} count={sharedNetworks.length} accent="Shared">
              {sharedNetworks.map(network => (
                <NetworkMetaCard
                  key={network.id}
                  network={network}
                  isExpanded={expanded.has(network.id)}
                  onToggle={() => toggleExpanded(network.id)}
                  showProjectColumn
                />
              ))}
            </Section>
          )}

          {externalNetworks.length > 0 && (
            <Section title={t('groups.External')} count={externalNetworks.length} accent="External">
              {externalNetworks.map(network => (
                <NetworkMetaCard
                  key={network.id}
                  network={network}
                  isExpanded={expanded.has(network.id)}
                  onToggle={() => toggleExpanded(network.id)}
                  showProjectColumn
                />
              ))}
            </Section>
          )}
        </>
      )}
    </Stack>
  );
}
