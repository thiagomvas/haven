import { useTranslation } from 'react-i18next';

import type { ServiceStatisticsDto } from '@/api/types';
import styles from '@/styles/components/ui/BuoyStrip.module.css';

import { type MarkState, StatusMark } from './StatusMark';

const MAX_MARKS = 20;

interface BuoyStripProps {
  stats: ServiceStatisticsDto;
  /** Edge length of each mark in px. */
  size?: number;
}

/**
 * One mark per service, worst state first so attention always sits at the left edge.
 * Built from aggregate counts; it shows how many services are in each state, not which ones.
 */
export function BuoyStrip({ stats, size = 12 }: BuoyStripProps) {
  const { t } = useTranslation('common');

  const groups: { state: MarkState; count: number; label: string }[] = [
    { state: 'degraded', count: stats.degraded, label: t('health.degraded') },
    { state: 'stopped', count: stats.stopped, label: t('health.stopped') },
    { state: 'deploying', count: stats.deploying, label: t('health.deploying') },
    { state: 'pending', count: stats.deploymentPending, label: t('health.deploymentPending') },
    { state: 'unknown', count: stats.unknown, label: t('health.unknown') },
    { state: 'running', count: stats.running, label: t('health.running') },
  ];

  const summary = groups
    .filter(g => g.count > 0)
    .map(g => `${g.count} ${g.label}`)
    .join(', ');

  const marks: MarkState[] = groups.flatMap(g => Array<MarkState>(g.count).fill(g.state));
  const visible = marks.slice(0, MAX_MARKS);
  const overflow = marks.length - visible.length;

  if (marks.length === 0) {
    return (
      <span className={styles.none}>{t('labels.noServices', { defaultValue: 'No services' })}</span>
    );
  }

  return (
    <span className={styles.strip} role="img" aria-label={summary} title={summary}>
      {visible.map((state, i) => (
        <StatusMark key={i} state={state} size={size} />
      ))}
      {overflow > 0 && <span className={styles.overflow}>+{overflow}</span>}
    </span>
  );
}
