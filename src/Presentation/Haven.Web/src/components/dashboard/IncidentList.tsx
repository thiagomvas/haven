import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

import type { AttentionReason, AttentionServiceDto } from '@/api/types';
import { type MarkState, StatusMark } from '@/components/ui/StatusMark';
import { formatRelative } from '@/lib/utils';
import styles from '@/styles/components/dashboard/IncidentList.module.css';

const MARKS: Record<AttentionReason, MarkState> = {
  Unhealthy: 'degraded',
  Crashed: 'fault',
  DeploymentFailed: 'fault',
};

interface IncidentListProps {
  services: AttentionServiceDto[];
}

/** Services that need a human: unhealthy, crashed, or whose latest deployment failed. */
export function IncidentList({ services }: IncidentListProps) {
  const { t } = useTranslation('dashboard');
  const { t: tCommon } = useTranslation('common');

  if (services.length === 0) {
    return (
      <p className={styles.clear}>
        <StatusMark state="running" />
        {t('incidents.clear')}
      </p>
    );
  }

  return (
    <ul className={styles.list}>
      {services.map(service => (
        <li key={service.serviceId} className={styles.item}>
          <StatusMark
            state={MARKS[service.reason]}
            size={14}
            title={t(`incidents.reason.${service.reason}`)}
          />
          <div className={styles.body}>
            <div className={styles.title}>
              <Link
                className={styles.service}
                to={`/projects/${service.projectId}/environments/${service.environmentId}/services/${service.serviceId}`}
              >
                {service.serviceName}
              </Link>
              <Link
                className={styles.where}
                to={`/projects/${service.projectId}/environments/${service.environmentId}`}
              >
                {service.projectName} / {service.environmentName}
              </Link>
            </div>
            <p className={styles.reason}>{t(`incidents.reason.${service.reason}`)}</p>
          </div>
          {service.since && (
            <div className={styles.meta}>
              <span>{t('incidents.since', { time: formatRelative(service.since, tCommon) })}</span>
            </div>
          )}
        </li>
      ))}
    </ul>
  );
}
