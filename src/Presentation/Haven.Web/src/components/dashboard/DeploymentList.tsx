import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import type { DeploymentStatus, RecentDeploymentDto } from '@/api/types';
import { type MarkState, StatusMark } from '@/components/ui/StatusMark';
import { formatDuration, formatRelative } from '@/lib/utils';
import styles from '@/styles/components/dashboard/DeploymentList.module.css';

const MARKS: Record<DeploymentStatus, MarkState> = {
  Succeeded: 'running',
  Failed: 'fault',
  Cancelled: 'stopped',
  Queued: 'pending',
  Building: 'deploying',
  Starting: 'deploying',
};

interface DeploymentListProps {
  deployments: RecentDeploymentDto[];
}

function durationSeconds(d: RecentDeploymentDto): number | undefined {
  if (!d.finishedAt) return undefined;
  return Math.max(0, (new Date(d.finishedAt).getTime() - new Date(d.startedAt).getTime()) / 1000);
}

export function DeploymentList({ deployments }: DeploymentListProps) {
  const { t } = useTranslation('dashboard');
  const { t: tCommon } = useTranslation('common');

  if (deployments.length === 0) {
    return <p className={styles.empty}>{t('deployments.empty')}</p>;
  }

  return (
    <ul className={styles.list}>
      {deployments.map(d => {
        const seconds = durationSeconds(d);
        const outcome =
          seconds === undefined
            ? t(`deployments.status.${d.status}`)
            : d.status === 'Failed'
              ? t('deployments.failedAfter', { duration: formatDuration(seconds) })
              : formatDuration(seconds);
        return (
          <li key={d.id} className={styles.item}>
            <StatusMark
              state={MARKS[d.status]}
              size={14}
              title={t(`deployments.status.${d.status}`)}
            />
            <div className={styles.body}>
              <div className={styles.title}>
                <Link
                  className={styles.service}
                  to={`/projects/${d.projectId}/environments/${d.environmentId}/services/${d.serviceId}`}
                >
                  {d.serviceName}
                </Link>
                <span className={styles.where}>
                  {d.projectName} / {d.environmentName}
                </span>
              </div>
              <p className={styles.detail}>
                {d.identifier && <code>{d.identifier.replace(/^sha256:/, '').slice(0, 7)}</code>}
                {d.commitMessage ?? t('deployments.noMessage')}
              </p>
            </div>
            <div className={styles.meta}>
              <span>{formatRelative(d.startedAt, tCommon)}</span>
              <span>{outcome}</span>
            </div>
          </li>
        );
      })}
    </ul>
  );
}
