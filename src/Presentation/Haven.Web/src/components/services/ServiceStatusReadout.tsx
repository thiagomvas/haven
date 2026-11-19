import { useTranslation } from 'react-i18next';

import { DeploymentDto, ServiceDashboardDto } from '@/api/types';
import { shortIdentifier } from '@/components/services/DeploymentIdentifier';
import { Banner } from '@/components/ui/Banner';
import { StatGrid, StatItem } from '@/components/ui/StatGrid';
import { StatusMark, toMarkState } from '@/components/ui/StatusMark';
import { formatRelative } from '@/lib/utils';
import styles from '@/styles/components/services/ServiceStatusReadout.module.css';

interface ServiceStatusReadoutProps {
  service: ServiceDashboardDto;
  /** Newest first. */
  deployments: DeploymentDto[];
  /** Called when the fault banner is activated, to jump to the deployment logs. */
  onViewDeployments: () => void;
}

/**
 * The first-glance answers for a service: is it up, what is running, did the last deploy work,
 * since when, and where can I reach it.
 */
export function ServiceStatusReadout({
  service,
  deployments,
  onViewDeployments,
}: ServiceStatusReadoutProps) {
  const { t } = useTranslation(['services', 'common']);
  const { t: tCommon } = useTranslation('common');

  const statusKey = service.status.toLowerCase();
  const statusLabel = tCommon(`health.${statusKey}`, { defaultValue: service.status });
  const faulted = service.status === 'Crashed';
  const degraded = service.status === 'Degraded';

  const registry = service.registry;
  const startedAt =
    service.status === 'Running' && registry?.startedAt ? registry.startedAt : undefined;

  const latest = deployments[0];
  const deployed = deployments.find(d => d.status === 'Succeeded' && d.identifier);
  const lastFailed = latest?.status === 'Failed';
  const lastActive = ['Queued', 'Building', 'Starting'].includes(latest?.status ?? '');
  const lastMark = lastFailed ? 'fault' : lastActive ? 'deploying' : 'running';

  const domains = registry?.domains ?? [];
  const ports = registry?.ports ?? [];
  const firstDomain = domains[0];
  const reachable = firstDomain ? (
    <>
      <a
        className={styles.link}
        href={`${firstDomain.tlsMode === 'None' ? 'http' : 'https'}://${firstDomain.hostname}`}
        target="_blank"
        rel="noreferrer"
      >
        {firstDomain.hostname}
      </a>
      {domains.length > 1 && <span className={styles.more}>+{domains.length - 1}</span>}
    </>
  ) : ports.length ? (
    <span className={styles.mono}>
      {ports
        .map(p => (p.hostPort != null ? `${p.hostPort}:${p.containerPort}` : p.containerPort))
        .join(', ')}
    </span>
  ) : (
    '—'
  );

  const items: StatItem[] = [
    {
      label: tCommon('labels.status'),
      value: (
        <span className={styles.state}>
          <StatusMark state={toMarkState(statusKey)} size={14} />
          {statusLabel}
        </span>
      ),
      color: faulted ? 'var(--color-error)' : degraded ? 'var(--color-warning)' : undefined,
    },
    {
      label: t('services:readout.lastDeploy'),
      value: latest ? (
        <span className={styles.state}>
          <StatusMark state={lastMark} size={14} />
          {formatRelative(latest.startedAt, tCommon)}
        </span>
      ) : (
        t('services:readout.noDeploys')
      ),
      color: lastFailed ? 'var(--color-error)' : undefined,
    },
    {
      label: t('services:readout.version'),
      value: deployed ? <span className={styles.mono}>{shortIdentifier(deployed)}</span> : '—',
    },
    {
      label: t('services:startedAt'),
      value: startedAt ? formatRelative(startedAt, tCommon) : '—',
    },
    { label: t('services:readout.reachableAt'), value: reachable },
  ];

  return (
    <div className={styles.readout}>
      {(faulted || degraded) && (
        <Banner
          variant={faulted ? 'error' : 'warning'}
          title={statusLabel}
          description={t(faulted ? 'services:readout.faulted' : 'services:readout.degraded')}
          onClick={onViewDeployments}
        />
      )}
      <StatGrid className={styles.grid} items={items} />
    </div>
  );
}
